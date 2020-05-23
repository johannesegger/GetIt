namespace GetIt

open FSharp.Control
open FSharp.Control.Reactive
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Hosting.Server.Features
open Microsoft.Extensions.Logging
open Reaction.AspNetCore.Middleware
open System
open System.ComponentModel
open System.Diagnostics
open System.IO
open System.Reactive.Linq
open System.Reactive.Disposables
open System.Reactive.Subjects
open System.Runtime.InteropServices
open System.Threading
open Thoth.Json.Net

module internal UICommunication =
    type CommunicationState =
        {
            Disposable: IDisposable
            CommandSubject: Subject<Guid * ControllerMsg>
            ResponseSubject: Subject<ChannelMsg>
            UIWindowProcess: Process
            MutableModel: MutableModel
        }
        interface IDisposable with member x.Dispose () = x.Disposable.Dispose()

    let private socketPath = "/msgs"
    let private startWebServer controllerMsgs (uiMsgs: IObserver<_>) = async {
        let stream (connectionId: ConnectionId) (msgs: IAsyncObservable<ChannelMsg * ConnectionId>) : IAsyncObservable<ChannelMsg * ConnectionId> =
            let controllerMsgs =
                AsyncRx.create (fun obs -> async {
                    return
                        controllerMsgs
                        |> Observable.subscribeWithCallbacks
                            (obs.OnNextAsync >> Async.StartImmediate)
                            (obs.OnErrorAsync >> Async.StartImmediate)
                            (obs.OnCompletedAsync >> Async.StartImmediate)
                        |> fun d -> AsyncDisposable.Create (fun () -> async { d.Dispose() })
                })
                |> AsyncRx.map (fun msg -> ControllerMsg msg, "")

            msgs
            |> AsyncRx.tapOnNext (fst >> uiMsgs.OnNext)
            |> AsyncRx.flatMap(fun (msg, connId) ->
                match msg with
                | ControllerMsg _ -> AsyncRx.empty () // confirmations
                | ChannelMsg.UIMsg (SetSceneBounds _)
                | ChannelMsg.UIMsg (AnswerStringQuestion _)
                | ChannelMsg.UIMsg (AnswerBoolQuestion _) ->
                    AsyncRx.single (msg, connId)
            )
            |> AsyncRx.merge controllerMsgs

        let configureApp (app: IApplicationBuilder) =
            app
                .UseWebSockets()
                .UseStream(fun options ->
                    { options with
                        Stream = stream
                        Encode = Encode.channelMsg >> Encode.toString 0
                        Decode =
                            fun value ->
                                Decode.fromString Decode.channelMsg value
                                |> (function
                                    | Ok p -> Some p
                                    | Error p ->
                                        eprintfn "Deserializing message failed: %s, Message: %s" p value
                                        None
                                )
                        RequestPath = socketPath
                    }
                )
                |> ignore

        let webHost =
            WebHostBuilder()
                .UseKestrel()
                .Configure(Action<IApplicationBuilder> configureApp)
                .ConfigureLogging(fun hostingContext logging ->
                    logging
                        .AddFilter(fun l -> hostingContext.HostingEnvironment.IsDevelopment() || l.Equals LogLevel.Error)
                        .AddConsole()
                        .AddDebug()
                    |> ignore
                )
                .UseUrls("http://[::1]:0")
                .Build()

        do! webHost.StartAsync() |> Async.AwaitTask
        let url = webHost.ServerFeatures.Get<IServerAddressesFeature>().Addresses |> Seq.head

        let serverDisposable =
            Disposable.create (fun () ->
                webHost.StopAsync()
                |> Async.AwaitTask
                |> Async.RunSynchronously
            )
        return (url, serverDisposable)
    }

    let private startUI windowSize socketUrl =
        let environmentVariables =
            [
                match windowSize with
                | SpecificSize windowSize ->
                    "GET_IT_WINDOW_SIZE", sprintf "%dx%d" (int windowSize.Width) (int windowSize.Height)
                | Maximized ->
                    "GET_IT_START_MAXIMIZED", "1"
                "GET_IT_SOCKET_URL", socketUrl
#if DEBUG
                // Ensure that `yarn webpack-dev-server` is running before starting this
                "GET_IT_INDEX_URL", "http://localhost:8080"
#else
                "GET_IT_INDEX_URL", Path.Combine("..", "GetIt.UI", "index.html")
#endif
            ]

        let startInfo =
            let toOptionIfFileExists v = if File.Exists v then Some v else None
            let uiContainerPath =
                let fileName = "GetIt.UI.Container.exe"
                Environment.GetEnvironmentVariable "GET_IT_UI_CONTAINER_DIRECTORY"
                |> Option.ofObj
                |> Option.map (fun d -> Path.Combine(d, fileName))
#if DEBUG
                // Ensure that UI container is built using `dotnet build .\GetIt.UI.Container\`
                |> Option.orElse (Path.Combine(".", "GetIt.UI.Container", "bin", "Debug", "netcoreapp3.1", fileName) |> toOptionIfFileExists)
#else
                |> Option.orElseWith (fun () ->
                    let thisAssemblyDir = Path.GetDirectoryName(Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath)
                    Path.Combine(thisAssemblyDir, "..", "..", "tools", "GetIt.UI.Container", fileName) |> toOptionIfFileExists
                )
#endif
                |> Option.defaultValue fileName
                |> Path.GetFullPath
            let result = ProcessStartInfo(uiContainerPath, WorkingDirectory = Path.GetDirectoryName uiContainerPath)
            environmentVariables
            |> List.iter result.EnvironmentVariables.Add
            result
        Process.Start startInfo

    let inputEvents =
        Observable.Create (fun (obs: IObserver<InputEvent>) ->
            let observable = Windows.DeviceEvents.observable

            let d1 =
                observable
                |> Observable.choose (function | MouseMove _ as x -> Some x | _ -> None)
                |> Observable.sample (TimeSpan.FromMilliseconds 50.)
                |> Observable.subscribeObserver obs

            let d2 =
                observable
                |> Observable.choose (function | MouseMove _ -> None | x -> Some x )
                |> Observable.subscribeObserver obs

            d1
            |> Disposable.compose d2
        )

    let showScene windowSize =
        if not <| RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            raise (GetItException (sprintf "Operating system \"%s\" is not supported." RuntimeInformation.OSDescription))

        let controllerMsgs = new Subject<_>()
        let uiMsgs = new Subject<_>()

        let (serviceUrl, webServerDisposable) = startWebServer controllerMsgs uiMsgs |> Async.RunSynchronously
        let socketUrl =
            let uriBuilder = UriBuilder(serviceUrl)
            uriBuilder.Scheme <- "ws"
            uriBuilder.Path <- socketPath
            uriBuilder.ToString()
        let uiProcess = startUI windowSize socketUrl
        uiProcess.EnableRaisingEvents <- true
        let uiProcessExitSubscription = uiProcess.Exited.Subscribe (fun _ ->
#if DEBUG
            printfn "UI process exited -> Exiting controller process."
#endif
            exit 0
        )

        let mutableModel = MutableModel.create ()

        let uiMsgsSubscription =
            uiMsgs
            |> Observable.subscribe (function
                | ControllerMsg _ -> () // confirmations
                | ChannelMsg.UIMsg (SetSceneBounds sceneBounds as uiMsg) ->
                    MutableModel.updateCurrent (fun model -> UIMsg uiMsg, { model with SceneBounds = sceneBounds }) mutableModel
                | ChannelMsg.UIMsg (AnswerStringQuestion _ as uiMsg)
                | ChannelMsg.UIMsg (AnswerBoolQuestion _ as uiMsg) ->
                    MutableModel.updateCurrent (fun model -> UIMsg uiMsg, model) mutableModel
            )

        mutableModel.Subject
        |> Observable.choose (fst >> function | UIMsg (SetSceneBounds _) -> Some () | _ -> None)
        |> Observable.first
        |> Observable.wait

        if uiProcess.MainWindowHandle = IntPtr.Zero
        then raise (GetItException "UI doesn't have a window")

        let inputEventsSubscription =
            inputEvents
            |> Observable.subscribe (function
                | MouseMove position as msg ->
                    try
                        let clientPosition = Windows.DeviceEvents.screenToClient uiProcess.MainWindowHandle position
                        MutableModel.updateCurrent (fun model ->
                            let scenePosition = { X = model.SceneBounds.Left + clientPosition.X; Y = model.SceneBounds.Top - clientPosition.Y }
                            Other, { model with MouseState = { model.MouseState with Position = scenePosition } }) mutableModel
                    with
                        | :? Win32Exception when uiProcess.HasExited -> ()
                        | :? Win32Exception -> reraise ()
                | MouseClick mouseClick as msg ->
                    try
                        let clientPosition = Windows.DeviceEvents.screenToClient uiProcess.MainWindowHandle mouseClick.VirtualScreenPosition
                        MutableModel.updateCurrent (fun model ->
                            let scenePosition = { X = model.SceneBounds.Left + clientPosition.X; Y = model.SceneBounds.Top - clientPosition.Y }
                            let mouseClick = { Button = mouseClick.Button; Position = scenePosition }
                            ApplyMouseClick mouseClick, model) mutableModel
                    with
                        | :? Win32Exception when uiProcess.HasExited -> ()
                        | :? Win32Exception -> reraise ()
                | KeyDown key as msg ->
                    MutableModel.updateCurrent (fun model -> Other, { model with KeyboardState = { model.KeyboardState with KeysPressed = Set.add key model.KeyboardState.KeysPressed } }) mutableModel
                | KeyUp key as msg ->
                    MutableModel.updateCurrent (fun model -> Other, { model with KeyboardState = { model.KeyboardState with KeysPressed = Set.remove key model.KeyboardState.KeysPressed } }) mutableModel
            )

        {
            Disposable =
                uiMsgsSubscription
                |> Disposable.compose inputEventsSubscription
                |> Disposable.compose uiProcessExitSubscription
                |> Disposable.compose uiProcess
                |> Disposable.compose webServerDisposable
            CommandSubject = controllerMsgs
            ResponseSubject = uiMsgs
            UIWindowProcess = uiProcess
            MutableModel = mutableModel
        }

    let private sendMessage state message =
        use waitHandle = new ManualResetEventSlim()
        let messageId = Guid.NewGuid()
        use d =
            state.ResponseSubject
            |> Observable.firstIf (fun r ->
                match r with
                | ControllerMsg (msgId, msg) when msgId = messageId -> true
                | _ -> false
            )
            |> Observable.subscribe (fun p ->
                waitHandle.Set()
            )
        state.CommandSubject.OnNext (messageId, message)
        waitHandle.Wait()

    let private sendMessageAndWaitForResponse state msg responseFilter =
        let mutable response = None
        use waitHandle = new ManualResetEventSlim()
        use d =
            state.MutableModel.Subject
            |> Observable.choose responseFilter
            |> Observable.first
            |> Observable.subscribe (fun p ->
                response <- Some p
                waitHandle.Set()
            )

        sendMessage state msg

        waitHandle.Wait()
        Option.get response

    let addPlayer playerData state =
        let playerId = PlayerId.create ()
        sendMessage state <| AddPlayer (playerId, playerData)
        MutableModel.updateCurrent (fun m -> Other, { m with Players = Map.add playerId playerData m.Players |> Player.sendToBack playerId }) state.MutableModel
        playerId

    let removePlayer playerId state =
        sendMessage state <| RemovePlayer playerId
        MutableModel.updateCurrent (fun m -> Other, { m with Players = Map.remove playerId m.Players }) state.MutableModel

    let setWindowTitle title state =
        sendMessage state <| SetWindowTitle title

    let setBackground background state =
        sendMessage state <| SetBackground background

    let clearScene state =
        sendMessage state ClearScene

    type ScreenshotCaptureRegion = FullWindow | WindowContent

    let makeScreenshot region state =
        if RuntimeInformation.IsOSPlatform OSPlatform.Windows then
            let region' =
                match region with
                | FullWindow -> Windows.CaptureRegion.FullWindow
                | WindowContent -> Windows.CaptureRegion.WindowContent
            Thread.Sleep 500 // calm down
            Windows.ScreenCapture.captureWindow state.UIWindowProcess.MainWindowHandle region'
        else
            raise (GetItException (sprintf "Operating system \"%s\" is not supported." RuntimeInformation.OSDescription))

    let startBatch state =
        sendMessage state StartBatch

    let applyBatch state =
        sendMessage state ApplyBatch

    let setPosition playerId position state =
        SetPosition (playerId, position)
        |> sendMessage state
        MutableModel.updatePlayer playerId (fun p -> Other, { p with Position = position }) state.MutableModel

    let changePosition playerId relativePosition state =
        ChangePosition (playerId, relativePosition)
        |> sendMessage state
        MutableModel.updatePlayer playerId (fun p -> Other, { p with Position = p.Position + relativePosition }) state.MutableModel

    let setDirection playerId direction state =
        SetDirection (playerId, direction)
        |> sendMessage state
        MutableModel.updatePlayer playerId (fun p -> Other, { p with Direction = direction }) state.MutableModel

    let changeDirection playerId relativeDirection state =
        ChangeDirection (playerId, relativeDirection)
        |> sendMessage state
        MutableModel.updatePlayer playerId (fun p -> Other, { p with Direction = p.Direction + relativeDirection }) state.MutableModel

    let say playerId text state =
        SetSpeechBubble (playerId, Some (Say text))
        |> sendMessage state
        MutableModel.updatePlayer playerId (fun p -> Other, { p with SpeechBubble = Some (Say text) }) state.MutableModel

    let private setTemporarySpeechBubble playerId speechBubble state =
        MutableModel.updatePlayer playerId (fun p -> Other, { p with SpeechBubble = Some speechBubble }) state.MutableModel
        Disposable.create (fun () ->
            MutableModel.updatePlayer playerId (fun p -> Other, { p with SpeechBubble = None }) state.MutableModel
        )

    let askString playerId text state =
        use d = setTemporarySpeechBubble playerId (AskString text) state
        sendMessageAndWaitForResponse
            state
            (SetSpeechBubble (playerId, Some (AskString text)))
            (fst >> function | UIMsg (AnswerStringQuestion (pId, answer)) when pId = playerId -> Some answer | _ -> None)

    let askBool playerId text state =
        use d = setTemporarySpeechBubble playerId (AskBool text) state
        sendMessageAndWaitForResponse
            state
            (SetSpeechBubble (playerId, Some (AskBool text)))
            (fst >> function | UIMsg (AnswerBoolQuestion (pId, answer)) when pId = playerId -> Some answer | _ -> None)

    let shutUp playerId state =
        SetSpeechBubble (playerId, None)
        |> sendMessage state
        MutableModel.updatePlayer playerId (fun p -> Other, { p with SpeechBubble = None }) state.MutableModel

    let setPenState playerId isOn state =
        SetPenState (playerId, isOn)
        |> sendMessage state
        MutableModel.updatePlayer playerId (fun p -> Other, { p with Pen = { p.Pen with IsOn = isOn } }) state.MutableModel

    let togglePenState playerId state =
        TogglePenState playerId
        |> sendMessage state
        MutableModel.updatePlayer playerId (fun p -> Other, { p with Pen = { p.Pen with IsOn = not p.Pen.IsOn } }) state.MutableModel

    let setPenColor playerId color state =
        SetPenColor (playerId, color)
        |> sendMessage state
        MutableModel.updatePlayer playerId (fun p -> Other, { p with Pen = { p.Pen with Color = color } }) state.MutableModel

    let shiftPenColor playerId angle state =
        ShiftPenColor (playerId, angle)
        |> sendMessage state
        MutableModel.updatePlayer playerId (fun p -> Other, { p with Pen = { p.Pen with Color = Color.hueShift angle p.Pen.Color } }) state.MutableModel

    let setPenWeight playerId weight state =
        SetPenWeight (playerId, weight)
        |> sendMessage state
        MutableModel.updatePlayer playerId (fun p -> Other, { p with Pen = { p.Pen with Weight = weight } }) state.MutableModel

    let changePenWeight playerId weight state =
        ChangePenWeight (playerId, weight)
        |> sendMessage state
        MutableModel.updatePlayer playerId (fun p -> Other, { p with Pen = { p.Pen with Weight = p.Pen.Weight + weight } }) state.MutableModel

    let setSizeFactor playerId sizeFactor state =
        SetSizeFactor (playerId, sizeFactor)
        |> sendMessage state
        MutableModel.updatePlayer playerId (fun p -> Other, { p with SizeFactor = sizeFactor }) state.MutableModel

    let changeSizeFactor playerId sizeFactor state =
        ChangeSizeFactor (playerId, sizeFactor)
        |> sendMessage state
        MutableModel.updatePlayer playerId (fun p -> Other, { p with SizeFactor = p.SizeFactor + sizeFactor }) state.MutableModel

    let setNextCostume playerId state =
        SetNextCostume playerId
        |> sendMessage state
        MutableModel.updatePlayer playerId (fun p -> Other, Player.nextCostume p) state.MutableModel

    let sendToBack playerId state =
        SendToBack playerId
        |> sendMessage state
        MutableModel.updateCurrent (fun m -> Other, { m with Players = Player.sendToBack playerId m.Players }) state.MutableModel

    let bringToFront playerId state =
        BringToFront playerId
        |> sendMessage state
        MutableModel.updateCurrent (fun m -> Other, { m with Players = Player.bringToFront playerId m.Players }) state.MutableModel

    let setVisibility playerId isVisible state =
        SetVisibility (playerId, isVisible)
        |> sendMessage state
        MutableModel.updatePlayer playerId (fun p -> Other, { p with IsVisible = isVisible }) state.MutableModel

    let toggleVisibility playerId state =
        ToggleVisibility playerId
        |> sendMessage state
        MutableModel.updatePlayer playerId (fun p -> Other, { p with IsVisible = not p.IsVisible }) state.MutableModel
