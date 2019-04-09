namespace GetIt

open System
open System.Diagnostics
open System.IO
open System.IO.Pipes
open System.Runtime.InteropServices

module internal UICommunication =
    let private invokeEventHandlers model fn =
        model.EventHandlers
        |> List.map snd
        |> List.choose fn
        |> Async.Parallel
        |> Async.Ignore
        |> Async.Start
        model

    let private updatePlayer model playerId fn =
        let player = Map.find playerId model.Players |> fn
        { model with Players = Map.add playerId player model.Players }

    let private applyUIToControllerMessage message model =
        let invokeEventHandlers = invokeEventHandlers model
        let updatePlayer = updatePlayer model

        match message with
        | ControllerMsgProcessed -> model
        | UIEvent (SetMousePosition position) ->
            let hasBeenEntered (player: PlayerData) =
                not (Rectangle.contains model.MouseState.Position player.Bounds) &&
                Rectangle.contains position player.Bounds

            let enteredPlayerIds =
                model.Players
                |> Map.toSeq
                |> Seq.filter (snd >> hasBeenEntered)
                |> Seq.map fst
                |> Seq.toList
            invokeEventHandlers (function
                | OnMouseEnterPlayer (playerId, handler) when List.contains playerId enteredPlayerIds ->
                    Some (async { return handler () })
                | _ -> None
            )
            |> ignore

            { model with MouseState = { model.MouseState with Position = position } }
        | UIEvent (ApplyMouseClick (mouseButton, position)) ->
            let clickedPlayerIds =
                model.Players
                |> Map.toSeq
                |> Seq.filter (snd >> fun player -> Rectangle.contains position player.Bounds)
                |> Seq.map fst
                |> Seq.toList
            invokeEventHandlers (function
                | OnClickPlayer (playerId, handler) when List.contains playerId clickedPlayerIds ->
                    Some (async { return handler mouseButton })
                | _ -> None
            )
            |> ignore

            if List.isEmpty clickedPlayerIds && Rectangle.contains position model.SceneBounds
            then
                invokeEventHandlers (function
                    | OnClickScene handler ->
                        Some (async { return handler position mouseButton })
                    | _ -> None
                )
                |> ignore

            model
        | UIEvent (SetSceneBounds sceneBounds) ->
            { model with SceneBounds = sceneBounds }
        | UIEvent (AnswerQuestion (playerId, answer)) ->
            updatePlayer playerId (fun p ->
                match p.SpeechBubble with
                | Some (Ask askData) -> { p with SpeechBubble = Some (Ask { askData with Answer = Some answer }) }
                | Some (Say _)
                | None -> p
            )

    let private applyControllerToUIMessage message model =
        let updatePlayer = updatePlayer model
        let invokeEventHandlers = invokeEventHandlers model

        match message with
        | UIMsgProcessed -> model
        | ShowScene windowSize ->
            // Scene bounds will come from UI
            model
        | ClearScene ->
            model
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
            let hasActiveTextInput =
                model.Players
                |> Map.exists (fun playerId player ->
                    match player.SpeechBubble with
                    | Some (Ask askData) -> true
                    | Some (Say _)
                    | None -> false
                )
            if not hasActiveTextInput then
                invokeEventHandlers (function
                    | OnAnyKeyDown handler ->
                        Some (async { return handler key })
                    | OnKeyDown (handlerKey, handler) when handlerKey = key ->
                        Some (async { return handler () })
                    | _ -> None
                )
                |> ignore
            { model with KeyboardState = { model.KeyboardState with KeysPressed = Set.add key model.KeyboardState.KeysPressed } }
        | ControllerEvent (KeyUp key) ->
            { model with KeyboardState = { model.KeyboardState with KeysPressed = Set.remove key model.KeyboardState.KeysPressed } }
        | ControllerEvent (MouseMove position) ->
            // Position on scene will come from UI
            model
        | ControllerEvent (MouseClick (mouseButton, position)) ->
            // Position on scene will come from UI
            model

    let mutable private connection = None

    let setupLocalConnectionToUIProcess() =
        let localConnection =
            if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
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
                Model.updateCurrent (fun model -> applyUIToControllerMessage msg model)
                localConnection.OnNext(IdentifiableMsg(msgId, UIMsgProcessed))
            )

        connection <- Some localConnection

    let sendCommand command =
        match connection with
        | Some connection ->
            match MessageProcessing.sendCommand connection command with
            | Ok msg ->
                Model.updateCurrent (applyControllerToUIMessage command >> applyUIToControllerMessage msg)
            | Error (MessageProcessing.ResponseError e) ->
                raise (GetItException (sprintf "Error while waiting for response: %O" e))
            | Error MessageProcessing.NoResponse
            | Error (MessageProcessing.ConnectionClosed _) ->
                // Close the application if the UI has been closed (throwing an exception might be confusing)
                // TODO dispose subscriptions etc. ?
                Environment.Exit 1
        | None ->
            raise (GetItException "Connection to UI not set up.")