namespace GetIt

open FSharp.Control
open FSharp.Control.Reactive
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System
open System.ComponentModel
open System.Diagnostics
open System.IO
open System.Reactive
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
            ResponseSubject: Subject<Result<UIToControllerMsg, string>>
            UIWindowProcess: Process
            MutableModel: MutableModel
            CancellationToken: CancellationToken
        }
        interface IDisposable with member x.Dispose () = x.Disposable.Dispose()

    let private getLoggerFactory () =
        let serviceCollection = ServiceCollection()
        serviceCollection.AddLogging(fun config ->
            config
                .SetMinimumLevel(LogLevel.Debug)
                .AddSimpleConsole(fun options ->
                    options.SingleLine <- true
                    options.IncludeScopes <- true
                    options.TimestampFormat <- "[HH:mm:ss] "
                )
            |> ignore
        ) |> ignore
        serviceCollection.BuildServiceProvider().GetService<ILoggerFactory>()

    let private startUI (logger: ILogger) onExit sceneSize socketUrl =
        let uiContainerPath = Environment.ProcessPath
        let startInfo =
            ProcessStartInfo(
                uiContainerPath,
                WorkingDirectory = Path.GetDirectoryName uiContainerPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            )
        [
            match sceneSize with
            | SpecificSize sceneSize ->
                "GET_IT_SCENE_SIZE", sprintf "%dx%d" (int sceneSize.Width) (int sceneSize.Height)
            | Maximized ->
                "GET_IT_START_MAXIMIZED", "1"
            "GET_IT_SOCKET_URL", socketUrl
        ]
        |> List.iter startInfo.EnvironmentVariables.Add

        let proc = Process.Start startInfo // TODO wrap exceptions?

        let d = new CompositeDisposable()
        proc.OutputDataReceived.Subscribe(fun v ->
            if not <| isNull v.Data then logger.LogInformation(v.Data)
        )
        |> d.Add
        proc.BeginOutputReadLine()
        proc.ErrorDataReceived.Subscribe(fun v ->
            if not <| isNull v.Data then logger.LogError(v.Data)
        )
        |> d.Add
        proc.BeginErrorReadLine()

        proc.EnableRaisingEvents <- true
        proc.Exited.Subscribe (fun _ ->
            logger.LogInformation("UI process exited with code {exitCode}", proc.ExitCode)
            onExit ()
        )
        |> d.Add

        d.Add proc

        (proc, d :> IDisposable)

    let private inputEvents =
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

    let showScene (sceneSize: SceneSize) =
        if not <| RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            raise (GetItException (sprintf "Operating system \"%s\" is not supported." RuntimeInformation.OSDescription))

        let d = new SingleAssignmentDisposable()
        let ds = new CompositeDisposable()
        let cd = new CancellationDisposable()
        ds.Add cd

        let controllerMsgs = new Subject<_>()
        let uiMsgs = new Subject<_>()
        let (encode, decoder) = Encode.Auto.generateEncoder(), Decode.Auto.generateDecoder()
        let messages = Subject.Create<_, _>(
            Observer.Create(Decode.fromString decoder >> uiMsgs.OnNext),
            controllerMsgs |> Observable.map (ControllerMsg >> encode >> Encode.toString 0)
        )

        let loggerFactory = getLoggerFactory ()
        ds.Add loggerFactory

        let logger = loggerFactory.CreateLogger("UICommunication")

        let (socketUrl, serverDisposable) = WebSocketServer.start messages loggerFactory |> Async.RunSynchronously
        ds.Add serverDisposable

        let uiLogger = loggerFactory.CreateLogger("UI process")
        let (uiProcess, uiProcessDisposables) = startUI uiLogger (fun () -> d.Dispose()) sceneSize socketUrl
        ds.Add uiProcessDisposables

        let mutableModel = MutableModel.create ()

        uiMsgs
        |> Observable.subscribe (function
            | Ok (ControllerMsgConfirmation _) -> ()
            | Ok (UIToControllerMsg.UIMsg (SetSceneBounds sceneBounds as uiMsg)) ->
                MutableModel.updateCurrent (fun model -> UIMsg uiMsg, { model with SceneBounds = sceneBounds }) mutableModel
            | Ok (UIToControllerMsg.UIMsg (AnswerStringQuestion _ as uiMsg))
            | Ok (UIToControllerMsg.UIMsg (AnswerBoolQuestion _ as uiMsg))
            | Ok (UIToControllerMsg.UIMsg (CapturedScene _ as uiMsg)) ->
                MutableModel.updateCurrent (fun model -> UIMsg uiMsg, model) mutableModel
            | Error e ->
                logger.LogError("Can't deserialize UI response: {error}", e)
        )
        |> ds.Add

        logger.LogDebug("Waiting for UI process to submit scene bounds.")

        mutableModel.Subject
        |> Observable.choose (fst >> function | UIMsg (SetSceneBounds _) -> Some () | _ -> None)
        |> Observable.first
        |> Observable.wait

        logger.LogDebug("UI process submitted scene bounds.")

        SpinWait.SpinUntil(fun () -> uiProcess.MainWindowHandle <> IntPtr.Zero)

        logger.LogDebug("Found main window handle of UI process.")

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
        // use waitHandle = new ManualResetEventSlim()
        let messageId = Guid.NewGuid()
        // use __ =
        //     state.ResponseSubject
        //     |> Observable.firstIf (fun r ->
        //         match r with
        //         | Ok (ControllerMsgConfirmation msgId) when msgId = messageId -> true
        //         | _ -> false
        //     )
        //     |> Observable.subscribe (fun p ->
        //         waitHandle.Set()
        //     )
        state.CommandSubject.OnNext (messageId, message)
        // waitHandle.Wait(state.CancellationToken)

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

    let makeScreenshot state =
        sendMessageAndWaitForResponse
            state
            CaptureScene
            (fst >> function | UIMsg (CapturedScene scene) -> Some scene | _ -> None)

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
