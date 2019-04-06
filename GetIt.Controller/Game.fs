namespace GetIt

open System
open System.Runtime.InteropServices
open System.Threading
open FSharp.Control.Reactive

module Game =
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
