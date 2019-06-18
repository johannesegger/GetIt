namespace GetIt

open System

/// Defines methods to setup a game, add players, register global events and more.
[<AbstractClass; Sealed>]
type Game() =
    static let mutable defaultTurtle = None

    /// Initializes and shows an empty scene with the default size and no players on it.
    static member ShowScene () =
        UICommunication.showScene (SpecificSize { Width = 800.; Height = 600. })

    /// Initializes and shows an empty scene with a specific size and no players on it.
    static member ShowScene (windowWidth, windowHeight) =
        UICommunication.showScene (SpecificSize { Width = windowWidth; Height = windowHeight })

    /// Initializes and shows an empty scene with maximized size and no players on it.
    static member ShowMaximizedScene () =
        UICommunication.showScene Maximized

    /// <summary>
    /// Adds a player to the scene.
    /// </summary>
    /// <param name="player">The definition of the player that should be added.</param>
    /// <returns>The added player.</returns>
    static member AddPlayer (playerData: PlayerData) =
        if obj.ReferenceEquals(playerData, null) then raise (ArgumentNullException "playerData")

        let playerId = UICommunication.addPlayer playerData
        new Player(playerId)

    /// <summary>
    /// Adds a player to the scene and calls a method to control the player.
    /// The method runs on a thread pool thread so that multiple players can be controlled in parallel.
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

    static member private AddTurtle() =
        defaultTurtle <- Some <| Game.AddPlayer PlayerData.Turtle

    /// Initializes and shows an empty scene and adds the default player to it.
    static member ShowSceneAndAddTurtle () =
        Game.ShowScene ()
        Game.AddTurtle ()

    /// Initializes and shows an empty scene with a specific size and adds the default player to it.
    static member ShowSceneAndAddTurtle (windowWidth, windowHeight) =
        Game.ShowScene (windowWidth, windowHeight)
        Game.AddTurtle ()

    /// Initializes and shows an empty scene with maximized size and adds the default player to it.
    static member ShowMaximizedSceneAndAddTurtle () =
        Game.ShowMaximizedScene ()
        Game.AddTurtle ()
