namespace GetIt

open System
open System.Runtime.InteropServices
open System.Threading
open FSharp.Control.Reactive

module internal Game =
    let mutable defaultTurtle = None

    let showScene windowSize =
        UICommunication.setupLocalConnectionToUIProcess()

        do
            use enumerator =
                Model.observable
                |> Observable.skip 1 // Skip initial value
                |> Observable.filter (fun (modelChangeEvent, model) ->
                    match modelChangeEvent with
                    | UIToControllerMsg (UIEvent (SetSceneBounds sceneBounds)) -> true
                    | _ -> false
                )
                |> Observable.take 1
                |> Observable.getEnumerator

            UICommunication.sendCommand (ShowScene windowSize)

            if not <| enumerator.MoveNext() then
                raise (GetItException "UI didn't initialize properly: Didn't receive scene size).")

        do
            use enumerator =
                Model.observable
                |> Observable.skip 1 // Skip initial value
                |> Observable.filter (fun (modelChangeEvent, model) ->
                    match modelChangeEvent with
                    | UIToControllerMsg (UIEvent (SetMousePosition position)) -> true
                    | _ -> false
                )
                |> Observable.take 1
                |> Observable.getEnumerator

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

            if not <| enumerator.MoveNext() then
                raise (GetItException "UI didn't initialize properly: Didn't receive mouse position).")

        ()

    let addTurtle () =
        let turtleId = PlayerId.create ()
        UICommunication.sendCommand (AddPlayer (turtleId, PlayerData.Turtle))
        defaultTurtle <- Some (new Player (turtleId))

/// <summary>
/// Defines methods to setup a game, add players, register global events and more.
/// </summary>
[<AbstractClass; Sealed>]
type Game() =
    /// <summary>
    /// Initializes and shows an empty scene with the default size and no players on it.
    /// </summary>
    static member ShowScene () =
        Game.showScene (SpecificSize { Width = 800.; Height = 600. })

    /// <summary>
    /// Initializes and shows an empty scene with a specific size and no players on it.
    /// </summary>
    static member ShowScene (windowWidth, windowHeight) =
        Game.showScene (SpecificSize { Width = windowWidth; Height = windowHeight })

    /// <summary>
    /// Initializes and shows an empty scene with maximized size and no players on it.
    /// </summary>
    static member ShowMaximizedScene () =
        Game.showScene Maximized

    /// <summary>
    /// Adds a player to the scene.
    /// </summary>
    /// <param name="player">The definition of the player that should be added.</param>
    /// <returns>The added player.</returns>
    static member AddPlayer (playerData: PlayerData) =
        if obj.ReferenceEquals(playerData, null) then raise (ArgumentNullException "playerData")

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
        if obj.ReferenceEquals(playerData, null) then raise (ArgumentNullException "playerData")
        if obj.ReferenceEquals(run, null) then raise (ArgumentNullException "run")

        let player = Game.AddPlayer playerData
        async { run.Invoke player } |> Async.Start
        player

    /// <summary>
    /// Initializes and shows an empty scene and adds the default player to it.
    /// </summary>
    static member ShowSceneAndAddTurtle () =
        Game.ShowScene ()
        Game.addTurtle ()

    /// <summary>
    /// Initializes and shows an empty scene with a specific size and adds the default player to it.
    /// </summary>
    static member ShowSceneAndAddTurtle (windowWidth, windowHeight) =
        Game.showScene (SpecificSize { Width = windowWidth; Height = windowHeight })
        Game.addTurtle ()

    /// <summary>
    /// Initializes and shows an empty scene with maximized size and adds the default player to it.
    /// </summary>
    static member ShowMaximizedSceneAndAddTurtle () =
        Game.showScene Maximized
        Game.addTurtle ()

    /// <summary>
    /// Sets the scene background.
    /// </summary>
    static member SetBackground (background) =
        if obj.ReferenceEquals(background, null) then raise (ArgumentNullException "background")

        UICommunication.sendCommand (SetBackground background)

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
        if obj.ReferenceEquals(key, null) then raise (ArgumentNullException "key")

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
        if obj.ReferenceEquals(key, null) then raise (ArgumentNullException "key")

        Model.getCurrent().KeyboardState.KeysPressed
        |> Set.contains key

    /// <summary>
    /// Checks whether any keyboard key is pressed.
    /// </summary>
    /// <returns>True, if any keyboard key is pressed, otherwise false.</returns>
    static member IsAnyKeyDown
        with get () =
            Model.getCurrent().KeyboardState.KeysPressed
            |> Set.isEmpty
            |> not

    /// <summary>
    /// Registers an event handler that is called when any keyboard key is pressed.
    /// </summary>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnAnyKeyDown (action: Action<_>) =
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")

        Model.addEventHandler (OnAnyKeyDown action.Invoke)

    /// <summary>
    /// Registers an event handler that is called when a specific keyboard key is pressed.
    /// </summary>
    /// <param name="key">The keyboard key that should be listened to.</param>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnKeyDown (key, action: Action) =
        if obj.ReferenceEquals(key, null) then raise (ArgumentNullException "key")
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")

        Model.addEventHandler (OnKeyDown (key, action.Invoke))

    /// <summary>
    /// Registers an event handler that is called when the mouse is clicked anywhere on the scene.
    /// </summary>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnClickScene (action: Action<_, _>) =
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")

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
