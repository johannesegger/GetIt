namespace GetIt

open System
open System.Diagnostics
open System.IO
open System.IO.Pipes
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Runtime.InteropServices
open System.Threading
open FSharp.Control.Reactive
open GetIt.Windows

exception GetItException of string

type internal EventHandler =
    | OnAnyKeyDown of handler: (KeyboardKey -> unit)
    | OnKeyDown of key: KeyboardKey * handler: (unit -> unit)
    | OnClickScene of handler: (Position -> MouseButton -> unit)
    | OnClickPlayer of playerId: PlayerId * handler: (MouseButton -> unit)
    | OnMouseEnterPlayer of playerId: PlayerId * handler: (unit -> unit)

type internal Model =
    { SceneBounds: Rectangle
      Players: Map<PlayerId, PlayerData>
      MouseState: MouseState
      KeyboardState: KeyboardState
      EventHandlers: (Guid * EventHandler) list }

module internal Model =
    let private gate = Object()

    let mutable private subject =
        let initial =
            { SceneBounds = { Position = Position.zero; Size = { Width = 0.; Height = 0. } }
              Players = Map.empty
              MouseState = MouseState.empty
              KeyboardState = KeyboardState.empty
              EventHandlers = [] }
        new BehaviorSubject<_>(initial)

    let observable = subject.AsObservable()

    let getCurrent () = subject.Value

    let updateCurrent fn =
        lock gate (fun () -> subject.OnNext(fn subject.Value))

    let addEventHandler eventHandler =
        let eventHandlerId = Guid.NewGuid()
        updateCurrent (fun model -> { model with EventHandlers = (eventHandlerId, eventHandler) :: model.EventHandlers })
        Disposable.create (fun () ->
            updateCurrent (fun model ->
                { model with EventHandlers = model.EventHandlers |> List.filter (fst >> (<>) eventHandlerId) }
            )
        )

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
                    ProcessStartInfo("GetIt.WPF.exe")
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
            | Error MessageProcessing.NoResponse ->
                // Close the application if the UI has been closed (throwing an exception might be confusing)
                // TODO dispose subscriptions etc. ?
                Environment.Exit 1
        | None ->
            raise (GetItException "Connection to UI not set up.")

/// <summary>
/// A player that is added to the scene.
/// </summary>
type Player(playerId) =
    let mutable isDisposed = 0

    member internal x.PlayerId with get () = playerId
    member private x.Player with get () = Map.find playerId (Model.getCurrent().Players)
    /// <summary>
    /// The actual size of the player.
    /// </summary>
    member x.Size with get () = x.Player.Size

    /// <summary>
    /// The factor that is used to resize the player.
    /// </summary>
    member x.SizeFactor with get () = x.Player.SizeFactor

    /// <summary>
    /// The position of the player.
    /// </summary>
    member x.Position with get () = x.Player.Position

    /// <summary>
    /// The actual bounds of the player.
    /// </summary>
    member x.Bounds with get () = x.Player.Bounds

    /// <summary>
    /// The direction of the player.
    /// </summary>
    member x.Direction with get () = x.Player.Direction

    /// <summary>
    /// The pen of the player.
    /// </summary>
    member x.Pen with get () = x.Player.Pen

    /// <summary>
    /// Removes the player from the scene.
    /// </summary>
    abstract member Dispose: unit -> unit
    default x.Dispose () =
        if Interlocked.Exchange (&isDisposed, 1) = 0 then
            UICommunication.sendCommand (RemovePlayer playerId)

    interface IDisposable with
        member x.Dispose () = x.Dispose ()

module internal Game =
    let mutable defaultTurtle = None

/// <summary>
/// Defines methods to setup a game, add players, register global events and more.
/// </summary>
[<AbstractClass; Sealed>]
type Game() =
    /// <summary>
    /// Initializes and shows an empty scene with no players on it.
    /// </summary>
    static member ShowScene () =
        UICommunication.setupLocalConnectionToUIProcess()

        let windowSize = { Width = 800.; Height = 600. }
        UICommunication.sendCommand (ShowScene windowSize)

        let subject = new System.Reactive.Subjects.Subject<_>()
        let (mouseMoveObservable, otherEventsObservable) =
            subject
            |> Observable.split (function
                | MouseMove _ as x -> Choice1Of2 x
                | x -> Choice2Of2 x
            )
        let d1 =
            mouseMoveObservable
            |> Observable.sample (TimeSpan.FromMilliseconds 50.)
            |> Observable.subscribe (ControllerEvent >> UICommunication.sendCommand)

        let d2 =
            otherEventsObservable
            |> Observable.subscribe (ControllerEvent >> UICommunication.sendCommand)

        let d3 =
            if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
                GetIt.Windows.DeviceEvents.register subject
            else
                raise (GetItException (sprintf "Operating system \"%s\" is not supported" RuntimeInformation.OSDescription))

        ()

    /// <summary>
    /// Adds a player to the scene.
    /// </summary>
    /// <param name="player">The definition of the player that should be added.</param>
    /// <returns>The added player.</returns>
    static member AddPlayer (playerData: PlayerData) =
        let playerId = PlayerId.create ()
        UICommunication.sendCommand (AddPlayer (playerId, playerData))
        new Player(playerId)

    /// <summary>
    /// Adds a player to the scene and calls a method to control the player.
    /// The method runs on a task pool thread so that multiple players can be controlled in parallel.
    /// </summary>
    /// <param name="player">The definition of the player that should be added.</param>
    /// <param name="run">The method that is used to control the player.</param>
    /// <returns>The added player.</returns>
    static member AddPlayer (playerData, run: Action<_>) =
        let player = Game.AddPlayer playerData
        async { run.Invoke player } |> Async.Start
        player

    /// <summary>
    /// Initializes and shows an empty scene and adds the default player to it.
    /// </summary>
    static member ShowSceneAndAddTurtle () =
        Game.ShowScene ()
        let turtleId = PlayerId.create ()
        UICommunication.sendCommand (AddPlayer (turtleId, PlayerData.Turtle))
        Game.defaultTurtle <- Some (new Player (turtleId))

    /// <summary>
    /// Clears all drawings from the scene.
    /// </summary>
    static member ClearScene () =
        UICommunication.sendCommand ClearScene

    /// <summary>
    /// Pauses execution of the current thread for a given time.
    /// </summary>
    /// <param name="durationInMilliseconds">The length of the pause in milliseconds.</param>
    static member Sleep (durationInMilliseconds) =
        Thread.Sleep (TimeSpan.FromMilliseconds (durationInMilliseconds))

    /// <summary>
    /// Pauses execution until the mouse clicks at the scene.
    /// </summary>
    /// <returns>The position of the mouse click.</returns>
    static member WaitForMouseClick () =
        use signal = new ManualResetEventSlim()
        let mutable mouseClickEvent = Unchecked.defaultof<_>
        let fn position mouseButton =
            mouseClickEvent <- {
                Position = position
                MouseButton = mouseButton
            }
            signal.Set()
        use d = Model.addEventHandler (OnClickScene fn)
        signal.Wait()
        mouseClickEvent

    /// <summary>
    /// Pauses execution until a specific keyboard key is pressed.
    /// </summary>
    /// <param name="key">The keyboard key to wait for.</param>
    static member WaitForKeyDown key =
        use signal = new ManualResetEventSlim()
        let fn () =
            signal.Set()
        use d = Model.addEventHandler (OnKeyDown (key, fn))
        signal.Wait()

    /// <summary>
    /// Pauses execution until any keyboard key is pressed.
    /// </summary>
    /// <returns>The keyboard key that is pressed.</returns>
    static member WaitForAnyKeyDown () =
        use signal = new ManualResetEventSlim()
        let mutable keyboardKey = Unchecked.defaultof<_>
        let fn key =
            keyboardKey <- key
            signal.Set()
        use d = Model.addEventHandler (OnAnyKeyDown fn)
        signal.Wait()
        keyboardKey

    /// <summary>
    /// Checks whether a given keyboard key is pressed.
    /// </summary>
    /// <param name="key">The keyboard key.</param>
    /// <returns>True, if the keyboard key is pressed, otherwise false.</returns>
    static member IsKeyDown key =
        Model.getCurrent().KeyboardState.KeysPressed
        |> Set.contains key

    /// <summary>
    /// Checks whether any keyboard key is pressed.
    /// </summary>
    /// <returns>True, if any keyboard key is pressed, otherwise false.</returns>
    static member IsAnyKeyDown key =
        Model.getCurrent().KeyboardState.KeysPressed
        |> Set.isEmpty
        |> not

    /// <summary>
    /// Registers an event handler that is called when any keyboard key is pressed.
    /// </summary>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnAnyKeyDown (action: Action<_>) =
        Model.addEventHandler (OnAnyKeyDown action.Invoke)

    /// <summary>
    /// Registers an event handler that is called when a specific keyboard key is pressed.
    /// </summary>
    /// <param name="key">The keyboard key that should be listened to.</param>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnKeyDown (key, action: Action) =
        Model.addEventHandler (OnKeyDown (key, action.Invoke))

    /// <summary>
    /// Registers an event handler that is called when the mouse is clicked anywhere on the scene.
    /// </summary>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnClickScene (action: Action<_, _>) =
        Model.addEventHandler (OnClickScene (curry action.Invoke))

    /// <summary>
    /// The bounds of the scene.
    /// </summary>
    static member SceneBounds
        with get () = Model.getCurrent().SceneBounds

    /// <summary>
    /// The current position of the mouse.
    /// </summary>
    static member MousePosition
        with get () = Model.getCurrent().MouseState.Position
