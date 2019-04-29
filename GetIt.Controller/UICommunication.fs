namespace GetIt

open System
open System.Diagnostics
open System.IO
open System.IO.Pipes
open System.Runtime.InteropServices

module internal UICommunication =
    let private updatePlayer model playerId fn =
        let player = Map.find playerId model.Players |> fn
        { model with Players = Map.add playerId player model.Players }

    let private applyUIToControllerMessage message model =
        let updatePlayer = updatePlayer model

        match message with
        | ControllerMsgProcessed -> model
        | UIEvent (SetMousePosition position) ->
            { model with MouseState = { model.MouseState with Position = position } }
        | UIEvent (ApplyMouseClick (mouseButton, position)) -> model
        | UIEvent (SetSceneBounds sceneBounds) ->
            { model with SceneBounds = sceneBounds }
        | UIEvent (AnswerQuestion (playerId, answer)) ->
            updatePlayer playerId (fun p ->
                match p.SpeechBubble with
                | Some (Ask askData) -> { p with SpeechBubble = Some (Ask { askData with Answer = Some answer }) }
                | Some (Say _)
                | None -> p
            )
        | UIEvent (Screenshot (PngImage data)) ->
            model

    let private applyControllerToUIMessage message model =
        let updatePlayer = updatePlayer model

        match message with
        | UIMsgProcessed -> model
        | ShowScene windowSize ->
            // Scene bounds will come from UI
            model
        | SetWindowTitle text -> model
        | SetBackground background -> model
        | ClearScene -> model
        | MakeScreenshot -> model
        | AddPlayer (playerId, player) ->
            { model with Players = Map.add playerId player model.Players }
        | RemovePlayer playerId ->
            { model with Players = Map.remove playerId model.Players }
        | SetPosition (playerId, position) ->
            updatePlayer playerId (fun p -> { p with Position = position })
        | SetDirection (playerId, angle) ->
            updatePlayer playerId (fun p -> { p with Direction = angle })
        | SetSpeechBubble (playerId, speechBubble) ->
            updatePlayer playerId (fun p -> { p with SpeechBubble = speechBubble })
        | SetPen (playerId, pen) ->
            updatePlayer playerId (fun p -> { p with Pen = pen })
        | SetSizeFactor (playerId, sizeFactor) ->
            updatePlayer playerId (fun p -> { p with SizeFactor = sizeFactor })
        | SetNextCostume playerId ->
            updatePlayer playerId Player.nextCostume
        | ControllerEvent (KeyDown key) ->
            { model with KeyboardState = { model.KeyboardState with KeysPressed = Set.add key model.KeyboardState.KeysPressed } }
        | ControllerEvent (KeyUp key) ->
            { model with KeyboardState = { model.KeyboardState with KeysPressed = Set.remove key model.KeyboardState.KeysPressed } }
        | ControllerEvent (MouseMove position) ->
            // Position on scene will come from UI
            model
        | ControllerEvent (MouseClick (mouseButton, position)) ->
            // Position on scene will come from UI
            model
        | StartBatch
        | ApplyBatch -> model

    let mutable private connection = None

    let setupLocalConnectionToUIProcess() =
        if Option.isSome connection then raise (GetItException "Connection to UI already set up. Do you call `Game.ShowSceneAndAddTurtle()` multiple times?")
        let localConnection =
            if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
                Process.GetProcessesByName("GetIt.WPF")
                |> Seq.iter (fun p ->
                    if not <| p.CloseMainWindow() then p.Kill()
                    p.WaitForExit()
                )

                let startInfo =
#if DEBUG
                    let path =
                        let rec parentPaths path acc =
                            if isNull path then List.rev acc
                            else parentPaths (Path.GetDirectoryName path) (path :: acc)
                        parentPaths (Path.GetFullPath ".") []
                        |> Seq.choose (fun p ->
                            let projectDir = Path.Combine(p, "GetIt.WPF")
                            if Directory.Exists projectDir
                            then Some projectDir
                            else None
                        )
                        |> Seq.head
                    ProcessStartInfo("dotnet", sprintf "run --project %s" path)
#else
                    let baseDir =
                        System.Reflection.Assembly.GetExecutingAssembly().Location
                        |> Path.GetDirectoryName
                        |> Path.GetDirectoryName
                        |> Path.GetDirectoryName
                    let path = Path.Combine(baseDir, "runtimes", "win-x64", "native", "GetIt.UI", "GetIt.WPF.exe")
                    ProcessStartInfo(path)
#endif

                let proc = Process.Start(startInfo)

                let pipeClient = new NamedPipeClientStream(".", "GetIt", PipeDirection.InOut, PipeOptions.Asynchronous)
                pipeClient.Connect()

                MessageProcessing.forStream pipeClient ControllerToUIMsg.encode UIToControllerMsg.decode
            else
                raise (GetItException (sprintf "Operating system \"%s\" is not supported" RuntimeInformation.OSDescription))

        let subscription =
            localConnection
            |> Observable.subscribe(fun (IdentifiableMsg (msgId, msg)) ->
                Model.updateCurrent (fun model -> UIToControllerMsg msg, applyUIToControllerMessage msg model)
                localConnection.OnNext(IdentifiableMsg(msgId, UIMsgProcessed))
            )

        connection <- Some localConnection

    let sendCommand command =
        match connection with
        | Some connection ->
            match MessageProcessing.sendCommand connection command with
            | Ok msg ->
                Model.updateCurrent (applyControllerToUIMessage command >> applyUIToControllerMessage msg >> (fun model -> UIToControllerMsg msg, model))
            | Error (MessageProcessing.ResponseError e) ->
                raise (GetItException ("Error while waiting for response.", e))
            | Error MessageProcessing.NoResponse
            | Error (MessageProcessing.ConnectionClosed _) ->
                // Close the application if the UI has been closed (throwing an exception might be confusing)
                // TODO dispose subscriptions etc. ?
                Environment.Exit 1
        | None ->
            raise (GetItException "Connection to UI not set up. Consider calling `Game.ShowSceneAndAddTurtle()` at the beginning.")