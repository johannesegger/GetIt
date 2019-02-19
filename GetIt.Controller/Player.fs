namespace GetIt

open System

module private Raw =
    let private rand = Random()

    [<CompiledName("MoveTo")>]
    let moveTo (player: GetIt.Player) (position: GetIt.Position) =
        InterProcessCommunication.sendCommands [ UpdatePosition (player.PlayerId, position) ]

    [<CompiledName("MoveTo")>]
    let moveToXY (player: GetIt.Player) (x: System.Double) (y: System.Double) =
        moveTo player { X = x; Y = y; }

    [<CompiledName("MoveToCenter")>]
    let moveToCenter (player: GetIt.Player) =
        moveTo player Position.zero

    [<CompiledName("MoveBy")>]
    let moveBy (player: GetIt.Player) (deltaX: System.Double) (deltaY: System.Double) =
        moveToXY player (player.Position.X + deltaX) (player.Position.Y + deltaY)

    [<CompiledName("MoveRight")>]
    let moveRight (player: GetIt.Player) (steps: System.Double) =
        moveBy player steps 0.

    [<CompiledName("MoveLeft")>]
    let moveLeft (player: GetIt.Player) (steps: System.Double) =
        moveBy player -steps 0.

    [<CompiledName("MoveUp")>]
    let moveUp (player: GetIt.Player) (steps: System.Double) =
        moveBy player 0. steps

    [<CompiledName("MoveDown")>]
    let moveDown (player: GetIt.Player) (steps: System.Double) =
        moveBy player 0. -steps

    [<CompiledName("MoveInDirection")>]
    let moveInDirection (player: GetIt.Player) (steps: System.Double) =
        let directionRadians = Degrees.toRadians player.Direction
        moveBy
            player
            (Math.Cos(directionRadians) * steps)
            (Math.Sin(directionRadians) * steps)

    [<CompiledName("MoveToRandomPosition")>]
    let moveToRandomPosition (player: GetIt.Player) =
        let x = rand.Next(int Model.current.SceneBounds.Left, int Model.current.SceneBounds.Right + 1)
        let y = rand.Next(int Model.current.SceneBounds.Bottom, int Model.current.SceneBounds.Top + 1)
        moveToXY player (float x) (float y)

module Turtle =
    let private getTurtleIdOrFail () =
        match Model.defaultTurtleId with
        | Some v -> v
        | None -> failwith "Default player hasn't been added to the scene. Consider calling `Game.ShowSceneAndAddTurtle()` at the beginning."
    
    let private getTurtleOrFail () =
        let turtleId = getTurtleIdOrFail()
        new Player(turtleId)

    /// <summary>Moves the player to a position.</summary>
    /// <param name="position">The absolute destination position.</param>
    [<CompiledName("MoveTo")>]
    let moveTo (position: GetIt.Position) =
        Raw.moveTo (getTurtleOrFail ()) position

    /// <summary>Moves the player to a position.</summary>
    /// <param name="x">The absolute x coordinate of the destination position.</param>
    /// <param name="y">The absolute y coordinate of the destination position.</param>
    [<CompiledName("MoveTo")>]
    let moveToXY (x: System.Double) (y: System.Double) =
        Raw.moveToXY (getTurtleOrFail ()) x y

    /// <summary>Moves the player to the center of the scene.</summary>
    [<CompiledName("MoveToCenter")>]
    let moveToCenter () =
        Raw.moveToCenter (getTurtleOrFail ())

    /// <summary>Moves the player to the center of the scene.</summary>
    /// <param name="deltaX">The change of the x coordinate.</param>
    /// <param name="deltaY">The change of the y coordinate.</param>
    [<CompiledName("MoveBy")>]
    let moveBy (deltaX: System.Double) (deltaY: System.Double) =
        Raw.moveBy (getTurtleOrFail ()) deltaX deltaY

    /// <summary>Moves the player horizontally.</summary>
    /// <param name="steps">The number of steps.</param>
    [<CompiledName("MoveRight")>]
    let moveRight (steps: System.Double) =
        Raw.moveRight (getTurtleOrFail ()) steps

    /// <summary>Moves the player horizontally.</summary>
    /// <param name="steps">The number of steps.</param>
    [<CompiledName("MoveLeft")>]
    let moveLeft (steps: System.Double) =
        Raw.moveLeft (getTurtleOrFail ()) steps

    /// <summary>Moves the player vertically.</summary>
    /// <param name="steps">The number of steps.</param>
    [<CompiledName("MoveUp")>]
    let moveUp (steps: System.Double) =
        Raw.moveUp (getTurtleOrFail ()) steps

    /// <summary>Moves the player vertically.</summary>
    /// <param name="steps">The number of steps.</param>
    [<CompiledName("MoveDown")>]
    let moveDown (steps: System.Double) =
        Raw.moveDown (getTurtleOrFail ()) steps

    /// <summary>Moves the player forward.</summary>
    /// <param name="steps">The number of steps.</param>
    [<CompiledName("MoveInDirection")>]
    let moveInDirection (steps: System.Double) =
        Raw.moveInDirection (getTurtleOrFail ()) steps

    /// <summary>Moves the player to a random position on the scene.</summary>
    [<CompiledName("MoveToRandomPosition")>]
    let moveToRandomPosition () =
        Raw.moveToRandomPosition (getTurtleOrFail ())

open System.Runtime.CompilerServices

[<Extension>]
type PlayerExtensions() =
    /// <summary>Moves the player to a position.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="position">The absolute destination position.</param>
    [<Extension>]
    static member MoveTo(player: GetIt.Player, position: GetIt.Position) =
        Raw.moveTo player position

    /// <summary>Moves the player to a position.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="x">The absolute x coordinate of the destination position.</param>
    /// <param name="y">The absolute y coordinate of the destination position.</param>
    [<Extension>]
    static member MoveTo(player: GetIt.Player, x: System.Double, y: System.Double) =
        Raw.moveToXY player x y

    /// <summary>Moves the player to the center of the scene.</summary>
    /// <param name="player">The player that should be moved.</param>
    [<Extension>]
    static member MoveToCenter(player: GetIt.Player) =
        Raw.moveToCenter player

    /// <summary>Moves the player to the center of the scene.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="deltaX">The change of the x coordinate.</param>
    /// <param name="deltaY">The change of the y coordinate.</param>
    [<Extension>]
    static member MoveBy(player: GetIt.Player, deltaX: System.Double, deltaY: System.Double) =
        Raw.moveBy player deltaX deltaY

    /// <summary>Moves the player horizontally.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="steps">The number of steps.</param>
    [<Extension>]
    static member MoveRight(player: GetIt.Player, steps: System.Double) =
        Raw.moveRight player steps

    /// <summary>Moves the player horizontally.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="steps">The number of steps.</param>
    [<Extension>]
    static member MoveLeft(player: GetIt.Player, steps: System.Double) =
        Raw.moveLeft player steps

    /// <summary>Moves the player vertically.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="steps">The number of steps.</param>
    [<Extension>]
    static member MoveUp(player: GetIt.Player, steps: System.Double) =
        Raw.moveUp player steps

    /// <summary>Moves the player vertically.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="steps">The number of steps.</param>
    [<Extension>]
    static member MoveDown(player: GetIt.Player, steps: System.Double) =
        Raw.moveDown player steps

    /// <summary>Moves the player forward.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="steps">The number of steps.</param>
    [<Extension>]
    static member MoveInDirection(player: GetIt.Player, steps: System.Double) =
        Raw.moveInDirection player steps

    /// <summary>Moves the player to a random position on the scene.</summary>
    /// <param name="player">The player that should be moved.</param>
    [<Extension>]
    static member MoveToRandomPosition(player: GetIt.Player) =
        Raw.moveToRandomPosition player
