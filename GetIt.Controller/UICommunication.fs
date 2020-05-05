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
open System.Reflection
open System.Runtime.InteropServices
open System.Threading
open Thoth.Json.Net

module internal UICommunication =
    type CommunicationState = {
        Disposable: IDisposable
        CommandSubject: Reactive.Subjects.Subject<Guid * ControllerMsg>
        ResponseSubject: Reactive.Subjects.Subject<ChannelMsg * ConnectionId>
        UIWindowProcess: Process
    }

    let private socketPath = "/msgs"
    let private startWebServer controllerMsgs (uiMsgs: IObserver<_>) = async {
        let stream (connectionId: ConnectionId) (msgs: IAsyncObservable<ChannelMsg * ConnectionId>) : IAsyncObservable<ChannelMsg * ConnectionId> =
            printfn "Client %s connected" connectionId
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
            |> AsyncRx.tapOnNext uiMsgs.OnNext
            |> AsyncRx.flatMap(fun (msg, connId) ->
                match msg with
                | ControllerMsg (msgId, msg) -> AsyncRx.empty () // confirmations
                | ChannelMsg.UIMsg (SetSceneBounds sceneBounds as uiMsg) ->
                    Model.updateCurrent (fun model -> UIMsg uiMsg, { model with SceneBounds = sceneBounds })
                    AsyncRx.single (msg, connId)
                | ChannelMsg.UIMsg (AnswerStringQuestion _ as uiMsg)
                | ChannelMsg.UIMsg (AnswerBoolQuestion _ as uiMsg) ->
                    Model.updateCurrent (fun model -> UIMsg uiMsg, model)
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

        printfn "Starting server"

        let cancellation = new CancellationDisposable()

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

        do! webHost.StartAsync(cancellation.Token) |> Async.AwaitTask
        let url = webHost.ServerFeatures.Get<IServerAddressesFeature>().Addresses |> Seq.head

        return (url, cancellation :> IDisposable)
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
                "GET_IT_INDEX_URL", "../../resources/UI/index.html"
#endif
            ]

        let startInfo =
            let toOptionIfFileExists v = if File.Exists v then Some v else None
            let uiContainerPath =
                let fileName = "GetIt.UI.Container.exe"
                let thisAssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                Environment.GetEnvironmentVariable "GET_IT_UI_CONTAINER_DIRECTORY"
                |> Option.ofObj
                |> Option.map (fun d -> Path.Combine(d, fileName))
                |> Option.orElse (Path.Combine(thisAssemblyDir, fileName) |> toOptionIfFileExists)
                |> Option.orElse (Path.Combine(thisAssemblyDir, "..", "..", "GetIt.UI", fileName) |> toOptionIfFileExists)
                |> Option.defaultValue fileName
            let result = ProcessStartInfo(uiContainerPath)
            environmentVariables
            |> List.iter result.EnvironmentVariables.Add
            result
        printfn "Starting UI: %s %s" startInfo.FileName startInfo.Arguments
        let proc = Process.Start startInfo
        proc.EnableRaisingEvents <- true
        let exitSubscription = proc.Exited.Subscribe (fun _ -> Environment.Exit 0)

        let killProcessDisposable =
            Disposable.create (fun () ->
                proc.Kill() // TODO catch exceptions?
            )

        let d =
            exitSubscription
            |> Disposable.compose killProcessDisposable
        (proc, d)

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

        let controllerMsgs = new Reactive.Subjects.Subject<_>()
        let uiMsgs = new Reactive.Subjects.Subject<_>()

        let (serviceUrl, webServerDisposable) = startWebServer controllerMsgs uiMsgs |> Async.RunSynchronously
        let socketUrl =
            let uriBuilder = UriBuilder(serviceUrl)
            uriBuilder.Scheme <- "ws"
            uriBuilder.Path <- socketPath
            uriBuilder.ToString()
        let (uiProcess, uiProcessDisposable) = startUI windowSize socketUrl

        printfn "Waiting for scene bounds"

        Model.observable
        |> Observable.choose (fst >> function | UIMsg (SetSceneBounds _) -> Some () | _ -> None)
        |> Observable.first
        |> Observable.wait

        printfn "Setting up input events"

        if uiProcess.MainWindowHandle = nativeint 0
        then raise (GetItException "UI doesn't have a window")

        let inputEventsSubscription =
            inputEvents
            |> Observable.subscribe (function
                | MouseMove position as msg ->
                    try
                        let clientPosition = Windows.DeviceEvents.screenToClient uiProcess.MainWindowHandle position
                        Model.updateCurrent (fun model ->
                            let scenePosition = { X = model.SceneBounds.Left + clientPosition.X; Y = model.SceneBounds.Top - clientPosition.Y }
                            Other, { model with MouseState = { model.MouseState with Position = scenePosition } })
                    with
                        | :? Win32Exception when uiProcess.HasExited -> ()
                        | :? Win32Exception -> reraise ()
                | MouseClick mouseClick as msg ->
                    try
                        let clientPosition = Windows.DeviceEvents.screenToClient uiProcess.MainWindowHandle mouseClick.VirtualScreenPosition
                        Model.updateCurrent (fun model ->
                            let scenePosition = { X = model.SceneBounds.Left + clientPosition.X; Y = model.SceneBounds.Top - clientPosition.Y }
                            let mouseClick = { Button = mouseClick.Button; Position = scenePosition }
                            ApplyMouseClick mouseClick, model)
                    with
                        | :? Win32Exception when uiProcess.HasExited -> ()
                        | :? Win32Exception -> reraise ()
                | KeyDown key as msg ->
                    Model.updateCurrent (fun model -> Other, { model with KeyboardState = { model.KeyboardState with KeysPressed = Set.add key model.KeyboardState.KeysPressed } })
                | KeyUp key as msg ->
                    Model.updateCurrent (fun model -> Other, { model with KeyboardState = { model.KeyboardState with KeysPressed = Set.remove key model.KeyboardState.KeysPressed } })
            )

        printfn "Setup complete"

        {
            Disposable =
                inputEventsSubscription
                |> Disposable.compose uiProcessDisposable
                |> Disposable.compose webServerDisposable
            CommandSubject = controllerMsgs
            ResponseSubject = uiMsgs
            UIWindowProcess = uiProcess
        }

    let private sendMessage state message =
        use waitHandle = new ManualResetEventSlim()
        let messageId = Guid.NewGuid()
        use d =
            state.ResponseSubject
            |> Observable.firstIf (fst >> fun r ->
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
            Model.observable
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
        Model.updateCurrent (fun m -> Other, { m with Players = Map.add playerId playerData m.Players |> Player.sendToBack playerId })
        playerId

    let removePlayer playerId state =
        sendMessage state <| RemovePlayer playerId
        Model.updateCurrent (fun m -> Other, { m with Players = Map.remove playerId m.Players })

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
        Model.updatePlayer playerId (fun p -> Other, { p with Position = position })

    let changePosition playerId relativePosition state =
        ChangePosition (playerId, relativePosition)
        |> sendMessage state
        Model.updatePlayer playerId (fun p -> Other, { p with Position = p.Position + relativePosition })

    let setDirection playerId direction state =
        SetDirection (playerId, direction)
        |> sendMessage state
        Model.updatePlayer playerId (fun p -> Other, { p with Direction = direction })

    let changeDirection playerId relativeDirection state =
        ChangeDirection (playerId, relativeDirection)
        |> sendMessage state
        Model.updatePlayer playerId (fun p -> Other, { p with Direction = p.Direction + relativeDirection })

    let say playerId text state =
        SetSpeechBubble (playerId, Some (Say text))
        |> sendMessage state
        Model.updatePlayer playerId (fun p -> Other, { p with SpeechBubble = Some (Say text) })

    let private setTemporarySpeechBubble playerId speechBubble =
        Model.updatePlayer playerId (fun p -> Other, { p with SpeechBubble = Some speechBubble })
        Disposable.create (fun () ->
            Model.updatePlayer playerId (fun p -> Other, { p with SpeechBubble = None })
        )

    let askString playerId text state =
        use d = setTemporarySpeechBubble playerId (AskString text)
        sendMessageAndWaitForResponse
            state
            (SetSpeechBubble (playerId, Some (AskString text)))
            (fst >> function | UIMsg (AnswerStringQuestion (pId, answer)) when pId = playerId -> Some answer | _ -> None)

    let askBool playerId text state =
        use d = setTemporarySpeechBubble playerId (AskBool text)
        sendMessageAndWaitForResponse
            state
            (SetSpeechBubble (playerId, Some (AskBool text)))
            (fst >> function | UIMsg (AnswerBoolQuestion (pId, answer)) when pId = playerId -> Some answer | _ -> None)

    let shutUp playerId state =
        SetSpeechBubble (playerId, None)
        |> sendMessage state
        Model.updatePlayer playerId (fun p -> Other, { p with SpeechBubble = None })

    let setPenState playerId isOn state =
        SetPenState (playerId, isOn)
        |> sendMessage state
        Model.updatePlayer playerId (fun p -> Other, { p with Pen = { p.Pen with IsOn = isOn } })

    let togglePenState playerId state =
        TogglePenState playerId
        |> sendMessage state
        Model.updatePlayer playerId (fun p -> Other, { p with Pen = { p.Pen with IsOn = not p.Pen.IsOn } })

    let setPenColor playerId color state =
        SetPenColor (playerId, color)
        |> sendMessage state
        Model.updatePlayer playerId (fun p -> Other, { p with Pen = { p.Pen with Color = color } })

    let shiftPenColor playerId angle state =
        ShiftPenColor (playerId, angle)
        |> sendMessage state
        Model.updatePlayer playerId (fun p -> Other, { p with Pen = { p.Pen with Color = Color.hueShift angle p.Pen.Color } })

    let setPenWeight playerId weight state =
        SetPenWeight (playerId, weight)
        |> sendMessage state
        Model.updatePlayer playerId (fun p -> Other, { p with Pen = { p.Pen with Weight = weight } })

    let changePenWeight playerId weight state =
        ChangePenWeight (playerId, weight)
        |> sendMessage state
        Model.updatePlayer playerId (fun p -> Other, { p with Pen = { p.Pen with Weight = p.Pen.Weight + weight } })

    let setSizeFactor playerId sizeFactor state =
        SetSizeFactor (playerId, sizeFactor)
        |> sendMessage state
        Model.updatePlayer playerId (fun p -> Other, { p with SizeFactor = sizeFactor })

    let changeSizeFactor playerId sizeFactor state =
        ChangeSizeFactor (playerId, sizeFactor)
        |> sendMessage state
        Model.updatePlayer playerId (fun p -> Other, { p with SizeFactor = p.SizeFactor + sizeFactor })

    let setNextCostume playerId state =
        SetNextCostume playerId
        |> sendMessage state
        Model.updatePlayer playerId (fun p -> Other, Player.nextCostume p)

    let sendToBack playerId state =
        SendToBack playerId
        |> sendMessage state
        Model.updateCurrent (fun m -> Other, { m with Players = Player.sendToBack playerId m.Players })

    let bringToFront playerId state =
        BringToFront playerId
        |> sendMessage state
        Model.updateCurrent (fun m -> Other, { m with Players = Player.bringToFront playerId m.Players })

    let setVisibility playerId isVisible state =
        SetVisibility (playerId, isVisible)
        |> sendMessage state
        Model.updatePlayer playerId (fun p -> Other, { p with IsVisible = isVisible })

    let toggleVisibility playerId state =
        ToggleVisibility playerId
        |> sendMessage state
        Model.updatePlayer playerId (fun p -> Other, { p with IsVisible = not p.IsVisible })
