namespace GetIt

open System
open System.Diagnostics
open System.IO
open System.IO.Pipes
open System.Reactive
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Threading
open System.Threading.Tasks
open FSharp.Control.Reactive
open Thoth.Json.Net

type EventHandler =
    | OnKeyDown of key: KeyboardKey option * handler: (KeyboardKey -> unit)
    | OnClickScene of handler: (Position -> MouseButton -> unit)
    | OnClickPlayer of playerId: PlayerId * handler: (MouseButton -> unit)
    | OnMouseEnterPlayer of playerId: PlayerId * handler: (unit -> unit)

type Model =
    { SceneBounds: Rectangle
      Players: Map<PlayerId, PlayerData>
      MouseState: MouseState
      KeyboardState: KeyboardState
      EventHandlers: EventHandler list }

module Model =
    let mutable current =
        { SceneBounds = { Position = Position.zero; Size = { Width = 0.; Height = 0. } }
          Players = Map.empty
          MouseState = MouseState.empty
          KeyboardState = KeyboardState.empty
          EventHandlers = [] }

module internal UICommunication =
    let private uiProcess =
        lazy (
            // TODO determine UI technology based on host OS
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
                ProcessStartInfo("GetIt.WPF.exe")
#endif
            
            let proc = Process.Start(startInfo)

            let pipeClient = new NamedPipeClientStream(".", "GetIt", PipeDirection.InOut, PipeOptions.Asynchronous)
            pipeClient.Connect()

            let subject = MessageProcessing.forStream pipeClient ControllerToUIMsg.encode UIToControllerMsg.decode

            let subscription =
                subject
                |> Observable.subscribe(fun (IdentifiableMsg (msgId, msg)) ->
                    // TODO handle events etc.
                    subject.OnNext(IdentifiableMsg(msgId, UIMsgProcessed))
                )

            subject
        )

    let private updatePlayer model playerId fn =
        let player = Map.find playerId model.Players |> fn
        { model with Players = Map.add playerId player model.Players }

    let private applyUIToControllerMessage message model =
        let invokeEventHandlers fn =
            model.EventHandlers
            |> List.choose fn
            |> Async.Parallel
            |> Async.Ignore
            |> Async.Start
            model

        match message with
        | ControllerMsgProcessed -> model
        | UIEvent (ClickScene (position, mouseButton)) ->
            invokeEventHandlers (function
                | OnClickScene handler -> Some (async { return handler position mouseButton })
                | _ -> None
            )
        | UIEvent (ClickPlayer (playerId, mouseButton)) ->
            invokeEventHandlers (function
                | OnClickPlayer (playerId, handler) when playerId = playerId -> Some (async { return handler mouseButton })
                | _ -> None
            )
        | UIEvent (MouseEnterPlayer playerId) ->
            invokeEventHandlers (function
                | OnMouseEnterPlayer (playerId, handler) when playerId = playerId -> Some (async { return handler () })
                | _ -> None
            )

    let private applyControllerToUIMessage message model =
        let updatePlayer = updatePlayer model
        match message with
        | UIMsgProcessed -> model
        | ShowScene sceneBounds ->
            { model with SceneBounds = sceneBounds }
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
        // | KeyDown key ->
        //     { model with KeyboardState = { model.KeyboardState with KeysPressed = Set.add key model.KeyboardState.KeysPressed } }
        // | KeyUp key ->
        //     { model with KeyboardState = { model.KeyboardState with KeysPressed = Set.remove key model.KeyboardState.KeysPressed } }
        | MouseMove position ->
            // "Real" position (relative to UI) will come from UI
            model
        | MouseClick -> model

    let sendCommand command =
        let connection = uiProcess.Force()

        match MessageProcessing.sendCommand connection command with
        | Ok msg ->
            Model.current <-
                Model.current
                |> applyControllerToUIMessage command
                |> applyUIToControllerMessage msg
        | Error (MessageProcessing.ErrorWhileWaitingForResponse e) ->
            failwithf "Error while waiting for response: %O" e
        | Error MessageProcessing.NoResponse ->
            // Close the application if the UI has been closed (throwing an exception might be confusing)
            Environment.Exit 1

type Player(playerId) =
    let mutable isDisposed = 0

    member internal x.PlayerId with get() = playerId
    member private x.Player with get() = Map.find playerId Model.current.Players
    /// <summary>
    /// The actual size of the player.
    /// </summary>
    member x.Size with get() = x.Player.Size

    /// <summary>
    /// The factor that is used to resize the player.
    /// </summary>
    member x.SizeFactor with get() = x.Player.SizeFactor

    /// <summary>
    /// The position of the player.
    /// </summary>
    member x.Position with get() = x.Player.Position

    /// <summary>
    /// The actual bounds of the player.
    /// </summary>
    member x.Bounds with get() = x.Player.Bounds

    /// <summary>
    /// The direction of the player.
    /// </summary>
    member x.Direction with get() = x.Player.Direction

    /// <summary>
    /// The pen of the player.
    /// </summary>
    member x.Pen with get() = x.Player.Pen

    abstract member Dispose: unit -> unit
    default x.Dispose() =
        if Interlocked.Exchange(&isDisposed, 1) = 0
        then
            UICommunication.sendCommand (RemovePlayer playerId)

    interface IDisposable with
        member x.Dispose() = x.Dispose()

module Game =
    let mutable internal defaultTurtle = None

    [<CompiledName("ShowScene")>]
    let showScene () =
        let sceneBounds = { Position = { X = -300.; Y = -200. }; Size = { Width = 600.; Height = 400. } }
        UICommunication.sendCommand (ShowScene sceneBounds)

    [<CompiledName("ShowSceneAndAddTurtle")>]
    let showSceneAndAddTurtle() =
        showScene ()
        UICommunication.sendCommand (AddPlayer (PlayerId.create (), Player.turtle))
        defaultTurtle <-
            Map.toSeq Model.current.Players
            |> Seq.tryHead
            |> Option.map (fst >> (fun playerId -> new Player(playerId)))
