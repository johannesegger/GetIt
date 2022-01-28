namespace GetIt

open FSharp.Control
open FSharp.Control.Reactive
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Hosting.Server.Features
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Hosting
open System
open System.ComponentModel
open System.Diagnostics
open System.IO
open System.Net.WebSockets
open System.Reactive.Linq
open System.Reactive.Disposables
open System.Reactive.Subjects
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
open Thoth.Json.Net

module internal UICommunication =
    type CommunicationState =
        {
            Disposable: IDisposable
            CommandSubject: Subject<Guid * ControllerMsg>
            ResponseSubject: Subject<UIToControllerMsg>
            UIWindowProcess: Process
            MutableModel: MutableModel
            CancellationToken: CancellationToken
        }
        interface IDisposable with member x.Dispose () = x.Disposable.Dispose()

    let private socketPath = "/msgs"
    let private startWebServer controllerMsgs (uiMsgs: IObserver<_>) = async {
        let configureApp (app: IApplicationBuilder) =
            let (encode, decoder) = Encode.Auto.generateEncoder(), Decode.Auto.generateDecoder()
            let appLifetime = app.ApplicationServices.GetService<IHostApplicationLifetime> ()
            app
                .UseWebSockets()
                .Use(fun (context: HttpContext) (next: Func<Task>) ->
                    async {
                        if context.Request.Path = PathString socketPath then
                            if context.WebSockets.IsWebSocketRequest then
                                use! webSocket = context.WebSockets.AcceptWebSocketAsync() |> Async.AwaitTask
                                let (wsConnection, wsSubject) = ReactiveWebSocket.setup webSocket
                                use __ = wsConnection
                                use __ =
                                    controllerMsgs
                                    |> Observable.map (ControllerMsg >> encode >> Encode.toString 0)
                                    |> Observable.subscribeObserver wsSubject
                                use __ =
                                    wsSubject
                                    |> Observable.choose (Decode.fromString decoder >> function | Ok msg -> Some msg | Error e -> None)
                                    |> Observable.subscribeObserver uiMsgs
                                do! Async.Sleep Int32.MaxValue
                            else
                                context.Response.StatusCode <- 400
                        else
                            do! next.Invoke() |> Async.AwaitTask
                    }
                    |> fun wf -> Async.HandleCancellation(wf, (fun e cont econt ccont -> cont ()), appLifetime.ApplicationStopping)
                    |> fun wf -> Async.StartAsTask(wf, cancellationToken = appLifetime.ApplicationStopping) :> Task
                )
                |> ignore

        let webHost =
            WebHostBuilder()
                .UseKestrel()
                .Configure(Action<IApplicationBuilder> configureApp)
                .ConfigureLogging(fun hostingContext logging ->
                    logging
                        .AddFilter(fun l -> hostingContext.HostingEnvironment.IsDevelopment() || l >= LogLevel.Error)
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

    let private startUI sceneSize socketUrl =
        let environmentVariables =
            [
                match sceneSize with
                | SpecificSize sceneSize ->
                    "GET_IT_SCENE_SIZE", sprintf "%dx%d" (int sceneSize.Width) (int sceneSize.Height)
                | Maximized ->
                    "GET_IT_START_MAXIMIZED", "1"
                "GET_IT_SOCKET_URL", socketUrl
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
                |> Option.orElseWith (fun () ->
                    let thisAssemblyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                    Path.Combine(thisAssemblyDir, "..", "..", "..", "..", "GetIt.UI.Container", "bin", "Debug", "netcoreapp3.1", fileName) |> toOptionIfFileExists
                )
#else
                |> Option.orElseWith (fun () ->
                    let thisAssemblyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                    Path.Combine(thisAssemblyDir, "tools", "GetIt.UI.Container", fileName) |> toOptionIfFileExists
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

    let showScene sceneSize =
        if not <| RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            raise (GetItException (sprintf "Operating system \"%s\" is not supported." RuntimeInformation.OSDescription))

        let d = new SingleAssignmentDisposable()
        let ds = new CompositeDisposable()
        let cd = new CancellationDisposable()
        ds.Add cd

        let controllerMsgs = new Subject<_>()
        let uiMsgs = new Subject<_>()

        let (serviceUrl, webServerDisposable) = startWebServer controllerMsgs uiMsgs |> Async.RunSynchronously
        ds.Add webServerDisposable
        let socketUrl =
            let uriBuilder = UriBuilder(serviceUrl)
            uriBuilder.Scheme <- "ws"
            uriBuilder.Path <- socketPath
            uriBuilder.ToString()
        let uiProcess = startUI sceneSize socketUrl
        ds.Add uiProcess
        uiProcess.EnableRaisingEvents <- true
        uiProcess.Exited.Subscribe (fun _ ->
            d.Dispose()
        )
        |> ds.Add

        let mutableModel = MutableModel.create ()

        uiMsgs
        |> Observable.subscribe (function
            | ControllerMsgConfirmation _ -> ()
            | UIToControllerMsg.UIMsg (SetSceneBounds sceneBounds as uiMsg) ->
                MutableModel.updateCurrent (fun model -> UIMsg uiMsg, { model with SceneBounds = sceneBounds }) mutableModel
            | UIToControllerMsg.UIMsg (AnswerStringQuestion _ as uiMsg)
            | UIToControllerMsg.UIMsg (AnswerBoolQuestion _ as uiMsg) ->
                MutableModel.updateCurrent (fun model -> UIMsg uiMsg, model) mutableModel
        )
        |> ds.Add

        mutableModel.Subject
        |> Observable.choose (fst >> function | UIMsg (SetSceneBounds _) -> Some () | _ -> None)
        |> Observable.first
        |> Observable.wait

        if uiProcess.MainWindowHandle = IntPtr.Zero
        then raise (GetItException "UI doesn't have a window")

        inputEvents
        |> Observable.subscribe (function
            | MouseMove position as msg ->
                try
                    let clientPosition = Windows.DeviceEvents.screenToClient uiProcess.MainWindowHandle position
                    MutableModel.updateCurrent (fun model ->
                        let scenePosition = { X = model.SceneBounds.Left + clientPosition.X; Y = model.SceneBounds.Top - clientPosition.Y }
                        Other, { model with MouseState = { model.MouseState with Position = scenePosition } }) mutableModel
                with
                    | :? Win32Exception when uiProcess.MainWindowHandle = IntPtr.Zero -> ()
                    | :? Win32Exception -> reraise ()
            | MouseClick mouseClick as msg ->
                try
                    let clientPosition = Windows.DeviceEvents.screenToClient uiProcess.MainWindowHandle mouseClick.VirtualScreenPosition
                    MutableModel.updateCurrent (fun model ->
                        let scenePosition = { X = model.SceneBounds.Left + clientPosition.X; Y = model.SceneBounds.Top - clientPosition.Y }
                        let mouseClick = { Button = mouseClick.Button; Position = scenePosition }
                        ApplyMouseClick mouseClick, model) mutableModel
                with
                    | :? Win32Exception when uiProcess.MainWindowHandle = IntPtr.Zero -> ()
                    | :? Win32Exception -> reraise ()
            | KeyDown key as msg ->
                MutableModel.updateCurrent (fun model -> Other, { model with KeyboardState = { model.KeyboardState with KeysPressed = Set.add key model.KeyboardState.KeysPressed } }) mutableModel
            | KeyUp key as msg ->
                MutableModel.updateCurrent (fun model -> Other, { model with KeyboardState = { model.KeyboardState with KeysPressed = Set.remove key model.KeyboardState.KeysPressed } }) mutableModel
        )
        |> ds.Add

        d.Disposable <- new CompositeDisposable(ds |> Seq.rev)
        {
            Disposable = d
            CommandSubject = controllerMsgs
            ResponseSubject = uiMsgs
            UIWindowProcess = uiProcess
            MutableModel = mutableModel
            CancellationToken = cd.Token
        }

    let private sendMessage state message =
        use waitHandle = new ManualResetEventSlim()
        let messageId = Guid.NewGuid()
        use __ =
            state.ResponseSubject
            |> Observable.firstIf (fun r ->
                match r with
                | ControllerMsgConfirmation msgId when msgId = messageId -> true
                | _ -> false
            )
            |> Observable.subscribe (fun p ->
                waitHandle.Set()
            )
        state.CommandSubject.OnNext (messageId, message)
        waitHandle.Wait(state.CancellationToken)

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

        waitHandle.Wait(state.CancellationToken)
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

    let shutUp playerId state =
        SetSpeechBubble (playerId, None)
        |> sendMessage state
        MutableModel.updatePlayer playerId (fun p -> Other, { p with SpeechBubble = None }) state.MutableModel

    let askString playerId text state =
        use __ = setTemporarySpeechBubble playerId (AskString text) state
        use __ = Disposable.create (fun () -> shutUp playerId state)
        sendMessageAndWaitForResponse
            state
            (SetSpeechBubble (playerId, Some (AskString text)))
            (fst >> function | UIMsg (AnswerStringQuestion (pId, answer)) when pId = playerId -> Some answer | _ -> None)

    let askBool playerId text state =
        use __ = setTemporarySpeechBubble playerId (AskBool text) state
        use __ = Disposable.create (fun () -> shutUp playerId state)
        sendMessageAndWaitForResponse
            state
            (SetSpeechBubble (playerId, Some (AskBool text)))
            (fst >> function | UIMsg (AnswerBoolQuestion (pId, answer)) when pId = playerId -> Some answer | _ -> None)

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
