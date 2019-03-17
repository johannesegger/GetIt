namespace GetIt

open System
open System.Diagnostics
open System.IO
open System.IO.Pipes
open System.Reactive.Linq
open System.Threading
open FSharp.Control.Reactive
open GetIt.Windows

type EventHandler =
    | OnAnyKeyDown of handler: (KeyboardKey -> unit)
    | OnKeyDown of key: KeyboardKey * handler: (unit -> unit)
    | OnClickScene of handler: (Position -> MouseButton -> unit)
    | OnClickPlayer of playerId: PlayerId * handler: (MouseButton -> unit)
    | OnMouseEnterPlayer of playerId: PlayerId * handler: (unit -> unit)

type Model =
    { SceneBounds: Rectangle
      Players: Map<PlayerId, PlayerData>
      MouseState: MouseState
      KeyboardState: KeyboardState
      EventHandlers: (Guid * EventHandler) list }

module Model =
    let private gate = Object()

    let mutable private current =
        { SceneBounds = { Position = Position.zero; Size = { Width = 0.; Height = 0. } }
          Players = Map.empty
          MouseState = MouseState.empty
          KeyboardState = KeyboardState.empty
          EventHandlers = [] }

    let getCurrent () = current

    let updateCurrent fn =
        lock gate (fun () -> current <- fn current)

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

    let private applyUIToControllerMessage message model =
        let invokeEventHandlers = invokeEventHandlers model

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

    let private updatePlayer model playerId fn =
        let player = Map.find playerId model.Players |> fn
        { model with Players = Map.add playerId player model.Players }

    let private applyControllerToUIMessage message model =
        let updatePlayer = updatePlayer model
        let invokeEventHandlers = invokeEventHandlers model

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
        | ControllerEvent (KeyDown key) ->
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
        | ControllerEvent (MouseClick mouseButton) ->
            let clickedPlayerIds =
                model.Players
                |> Map.toSeq
                |> Seq.filter (snd >> fun player -> Rectangle.contains model.MouseState.Position player.Bounds)
                |> Seq.map fst
                |> Seq.toList
            invokeEventHandlers (function
                | OnClickPlayer (playerId, handler) when List.contains playerId clickedPlayerIds ->
                    Some (async { return handler mouseButton })
                | _ -> None
            )
            |> ignore

            if List.isEmpty clickedPlayerIds && Rectangle.contains model.MouseState.Position model.SceneBounds
            then
                invokeEventHandlers (function
                    | OnClickScene handler ->
                        Some (async { return handler model.MouseState.Position mouseButton })
                    | _ -> None
                )
                |> ignore

            model

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
                    Model.updateCurrent (fun model -> applyUIToControllerMessage msg model)
                    subject.OnNext(IdentifiableMsg(msgId, UIMsgProcessed))
                )

            subject
        )

    let sendCommand command =
        let connection = uiProcess.Force()

        match MessageProcessing.sendCommand connection command with
        | Ok msg ->
            Model.updateCurrent (applyControllerToUIMessage command >> applyUIToControllerMessage msg)
        | Error (MessageProcessing.ErrorWhileWaitingForResponse e) ->
            failwithf "Error while waiting for response: %O" e
        | Error MessageProcessing.NoResponse ->
            // Close the application if the UI has been closed (throwing an exception might be confusing)
            Environment.Exit 1

type Player(playerId) =
    let mutable isDisposed = 0

    member internal x.PlayerId with get() = playerId
    member private x.Player with get() = Map.find playerId (Model.getCurrent().Players)
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

        let t = Thread(fun () ->
            let mouseHook = MouseHook()
            let d1 =
                Observable.Create(fun (observer: IObserver<_>) ->
                    let callback = MouseHook.MouseHookCallback(observer.OnNext)
                    mouseHook.add_MouseMove(callback)
                    Disposable.create (fun () ->
                        mouseHook.remove_MouseMove(callback)
                    )
                )
                |> Observable.sample (TimeSpan.FromMilliseconds 50.)
                |> Observable.map (fun evt -> { X = float evt.pt.x; Y = float evt.pt.y })
                |> Observable.subscribe (MouseMove >> ControllerEvent >> UICommunication.sendCommand)

            mouseHook.Install()

            let tryGetKeyboardKey key =
                match key with
                | KeyboardHook.VKeys.SPACE -> Some Space
                | KeyboardHook.VKeys.ESCAPE -> Some Escape
                | KeyboardHook.VKeys.UP -> Some Up
                | KeyboardHook.VKeys.DOWN -> Some Down
                | KeyboardHook.VKeys.LEFT -> Some Left
                | KeyboardHook.VKeys.RIGHT -> Some Right
                | KeyboardHook.VKeys.KEY_A -> Some A
                | KeyboardHook.VKeys.KEY_B -> Some B
                | KeyboardHook.VKeys.KEY_C -> Some C
                | KeyboardHook.VKeys.KEY_D -> Some D
                | KeyboardHook.VKeys.KEY_E -> Some E
                | KeyboardHook.VKeys.KEY_F -> Some F
                | KeyboardHook.VKeys.KEY_G -> Some G
                | KeyboardHook.VKeys.KEY_H -> Some H
                | KeyboardHook.VKeys.KEY_I -> Some I
                | KeyboardHook.VKeys.KEY_J -> Some J
                | KeyboardHook.VKeys.KEY_K -> Some K
                | KeyboardHook.VKeys.KEY_L -> Some L
                | KeyboardHook.VKeys.KEY_M -> Some M
                | KeyboardHook.VKeys.KEY_N -> Some N
                | KeyboardHook.VKeys.KEY_O -> Some O
                | KeyboardHook.VKeys.KEY_P -> Some P
                | KeyboardHook.VKeys.KEY_Q -> Some Q
                | KeyboardHook.VKeys.KEY_R -> Some R
                | KeyboardHook.VKeys.KEY_S -> Some S
                | KeyboardHook.VKeys.KEY_T -> Some T
                | KeyboardHook.VKeys.KEY_U -> Some U
                | KeyboardHook.VKeys.KEY_V -> Some V
                | KeyboardHook.VKeys.KEY_W -> Some W
                | KeyboardHook.VKeys.KEY_X -> Some X
                | KeyboardHook.VKeys.KEY_Y -> Some Y
                | KeyboardHook.VKeys.KEY_Z -> Some Z
                | KeyboardHook.VKeys.KEY_0 -> Some Digit0
                | KeyboardHook.VKeys.KEY_1 -> Some Digit1
                | KeyboardHook.VKeys.KEY_2 -> Some Digit2
                | KeyboardHook.VKeys.KEY_3 -> Some Digit3
                | KeyboardHook.VKeys.KEY_4 -> Some Digit4
                | KeyboardHook.VKeys.KEY_5 -> Some Digit5
                | KeyboardHook.VKeys.KEY_6 -> Some Digit6
                | KeyboardHook.VKeys.KEY_7 -> Some Digit7
                | KeyboardHook.VKeys.KEY_8 -> Some Digit8
                | KeyboardHook.VKeys.KEY_9 -> Some Digit9
                | _ -> None

            let keyboardHook = KeyboardHook()
            let d2 =
                Observable.Create(fun (observer: IObserver<_>) ->
                    let callback = KeyboardHook.KeyboardHookCallback(observer.OnNext)
                    keyboardHook.add_KeyDown(callback)
                    Disposable.create (fun () ->
                        keyboardHook.remove_KeyDown(callback)
                    )
                )
                |> Observable.choose tryGetKeyboardKey
                |> Observable.subscribe (KeyDown >> ControllerEvent >> UICommunication.sendCommand)
            let d3 =
                Observable.Create(fun (observer: IObserver<_>) ->
                    let callback = KeyboardHook.KeyboardHookCallback(observer.OnNext)
                    keyboardHook.add_KeyUp(callback)
                    Disposable.create (fun () ->
                        keyboardHook.remove_KeyUp(callback)
                    )
                )
                |> Observable.choose tryGetKeyboardKey
                |> Observable.subscribe (KeyUp >> ControllerEvent >> UICommunication.sendCommand)
            keyboardHook.Install()

            let mutable msg = Unchecked.defaultof<_>
            while WinNative.GetMessage(&msg, IntPtr.Zero, uint32 MouseHook.MouseMessages.WM_MOUSEFIRST, uint32 MouseHook.MouseMessages.WM_MOUSELAST) > 0 do
                WinNative.TranslateMessage(&msg) |> ignore
                WinNative.DispatchMessage(&msg) |> ignore

            // TODO uninstall when pipe is closed

            ()
        )
        t.IsBackground <- false
        t.Start()

    [<CompiledName("ShowSceneAndAddTurtle")>]
    let showSceneAndAddTurtle() =
        showScene ()
        UICommunication.sendCommand (AddPlayer (PlayerId.create (), Player.turtle))
        defaultTurtle <-
            Map.toSeq (Model.getCurrent().Players)
            |> Seq.tryHead
            |> Option.map (fst >> (fun playerId -> new Player(playerId)))

    [<CompiledName("OnAnyKeyDown")>]
    let onAnyKeyDown (action: Action<_>) =
        Model.addEventHandler (OnAnyKeyDown action.Invoke)

    [<CompiledName("OnKeyDown")>]
    let onKeyDown (key, action: Action) =
        Model.addEventHandler (OnKeyDown (key, action.Invoke))

    [<CompiledName("OnClickScene")>]
    let onClickScene (action: Action<_, _>) =
        Model.addEventHandler (OnClickScene (curry action.Invoke))
