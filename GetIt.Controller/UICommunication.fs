namespace GetIt

open FSharp.Control
open FSharp.Control.Reactive
open Microsoft.Extensions.Logging
open System
open System.ComponentModel
open System.Diagnostics
open System.IO
open System.Net
open System.Net.Sockets
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
            ClientMessages: ISubject<ControllerToUIMsg, Result<UIToControllerMsg, string>>
            UIWindowProcess: Process
            MutableModel: MutableModel
            CancellationToken: CancellationToken
        }
        interface IDisposable with member x.Dispose () = x.Disposable.Dispose()

    let private getLoggerFactory () =
        LoggerFactory.Create(fun builder ->
            builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddSimpleConsole(fun options ->
                    options.SingleLine <- true
                    options.IncludeScopes <- true
                    options.TimestampFormat <- "[HH:mm:ss] "
                )
            |> ignore
        )

    let private startUI (logger: ILogger) onExit sceneSize (serverAddress: IPEndPoint) =
        let uiContainerPath = Environment.ProcessPath
        let startInfo =
            ProcessStartInfo(
                uiContainerPath,
                Environment.CommandLine,
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
            "GET_IT_SERVER_ADDRESS", serverAddress.ToString()
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
            logger.LogDebug("UI process exited with code {exitCode}", proc.ExitCode)
            onExit ()
        )
        |> d.Add

        d.Add proc

        (proc, d :> IDisposable)

    let private keepProcessAlive () =
        let waitHandle = new ManualResetEventSlim()

        let messageLoopThread = Thread(fun () -> waitHandle.Wait())
        messageLoopThread.Name <- "KeepProcessAlive"
        messageLoopThread.IsBackground <- false
        messageLoopThread.Start()

        Disposable.create waitHandle.Set

    let showScene (sceneSize: SceneSize) =
        if not <| Seq.exists RuntimeInformation.IsOSPlatform [ OSPlatform.Windows; OSPlatform.OSX ] then
            raise (GetItException $"Operating system \"%s{RuntimeInformation.OSDescription}\" is not supported.")

        let d = new SingleAssignmentDisposable()
        let ds = new CompositeDisposable()
        let cd = new CancellationDisposable()
        ds.Add cd

        let loggerFactory = getLoggerFactory ()
        ds.Add loggerFactory

        let logger = loggerFactory.CreateLogger("UICommunication")

        let tcpServer = TcpListener(IPAddress.Loopback, 0)
        ds.Add (Disposable.create tcpServer.Stop)
        tcpServer.Start()
        let serverAddress = tcpServer.LocalEndpoint :?> IPEndPoint

        let uiLogger = loggerFactory.CreateLogger("UI process")
        let (uiProcess, uiProcessDisposables) = startUI uiLogger (fun () -> d.Dispose()) sceneSize serverAddress
        ds.Add uiProcessDisposables

        logger.LogDebug("Waiting for TCP client")
        let tcpClient = tcpServer.AcceptTcpClient()
        ds.Add tcpClient
        logger.LogDebug("TCP client connected")

        let tcpClientStream = tcpClient.GetStream()
        ds.Add tcpClientStream
        let messageSubject = Subject.fromStream tcpClientStream

        let (encode, decoder) = Encode.Auto.generateEncoder(), Decode.Auto.generateDecoder()
        let clientMessages = Subject.Create<_, _>(
            Observer.Create<ControllerToUIMsg>(encode >> Encode.toString 0 >> messageSubject.OnNext),
            messageSubject |> Observable.map (Decode.fromString decoder)
        )

        let mutableModel = MutableModel.create ()

        clientMessages
        |> Observable.subscribe (function
            | Ok (ControllerMsgConfirmation _) -> ()
            | Ok (UIToControllerMsg.UIMsg (SetSceneBounds sceneBounds as uiMsg)) ->
                logger.LogDebug("Received new scene bounds {SceneBounds}", sceneBounds)
                MutableModel.updateCurrent (fun model -> UIMsg uiMsg, { model with SceneBounds = sceneBounds }) mutableModel
            | Ok (UIToControllerMsg.UIMsg (MouseMove position as uiMsg)) ->
                MutableModel.updateCurrent (fun model ->
                    UIMsg uiMsg, { model with MouseState = { model.MouseState with Position = position } }) mutableModel
            | Ok (UIToControllerMsg.UIMsg (MouseClick _ as uiMsg)) ->
                MutableModel.updateCurrent (fun model -> UIMsg uiMsg, model) mutableModel
            | Ok (UIToControllerMsg.UIMsg (KeyDown key as uiMsg)) ->
                MutableModel.updateCurrent (fun model -> UIMsg uiMsg, { model with KeyboardState = { model.KeyboardState with KeysPressed = Set.add key model.KeyboardState.KeysPressed } }) mutableModel
            | Ok (UIToControllerMsg.UIMsg (KeyUp key as uiMsg)) ->
                MutableModel.updateCurrent (fun model -> UIMsg uiMsg, { model with KeyboardState = { model.KeyboardState with KeysPressed = Set.remove key model.KeyboardState.KeysPressed } }) mutableModel
            | Ok (UIToControllerMsg.UIMsg (AnswerStringQuestion _ as uiMsg))
            | Ok (UIToControllerMsg.UIMsg (AnswerBoolQuestion _ as uiMsg))
            | Ok (UIToControllerMsg.UIMsg (CapturedScene _ as uiMsg)) ->
                MutableModel.updateCurrent (fun model -> UIMsg uiMsg, model) mutableModel
            | Error e ->
                logger.LogError("Can't deserialize UI response: {error}", e)
        )
        |> ds.Add

        logger.LogDebug "Waiting for UI process to submit scene bounds and mouse position."

        Observable.mergeSeq [
            mutableModel.Subject
            |> Observable.choose (fst >> function | UIMsg (SetSceneBounds _) -> Some () | _ -> None)
            |> Observable.first

            mutableModel.Subject
            |> Observable.choose (fst >> function | UIMsg (MouseMove _) -> Some () | _ -> None)
            |> Observable.first
        ]
        |> Observable.wait

        logger.LogDebug "UI process submitted scene bounds and mouse position."

        keepProcessAlive ()
        |> ds.Add

        d.Disposable <- new CompositeDisposable(ds |> Seq.rev)
        {
            Disposable = d
            ClientMessages = clientMessages
            UIWindowProcess = uiProcess
            MutableModel = mutableModel
            CancellationToken = cd.Token
        }

    let private sendMessage state message =
        state.ClientMessages.OnNext (ControllerMsg message)

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
