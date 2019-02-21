namespace GetIt

open System
open System.Threading

module private Raw =
    let private rand = Random()

    let private touchesTopOrBottomEdge (player: GetIt.Player) =
        player.Bounds.Top > Model.current.SceneBounds.Top || player.Bounds.Bottom < Model.current.SceneBounds.Bottom

    let private touchesLeftOrRightEdge (player: GetIt.Player) =
        player.Bounds.Right > Model.current.SceneBounds.Right || player.Bounds.Left < Model.current.SceneBounds.Left

    let moveTo (player: GetIt.Player) (position: GetIt.Position) =
        InterProcessCommunication.sendCommands [ SetPosition (player.PlayerId, position) ]

    let moveToXY (player: GetIt.Player) (x: System.Double) (y: System.Double) =
        moveTo player { X = x; Y = y; }

    let moveToCenter (player: GetIt.Player) =
        moveTo player Position.zero

    let moveBy (player: GetIt.Player) (deltaX: System.Double) (deltaY: System.Double) =
        moveToXY player (player.Position.X + deltaX) (player.Position.Y + deltaY)

    let moveRight (player: GetIt.Player) (steps: System.Double) =
        moveBy player steps 0.

    let moveLeft (player: GetIt.Player) (steps: System.Double) =
        moveBy player -steps 0.

    let moveUp (player: GetIt.Player) (steps: System.Double) =
        moveBy player 0. steps

    let moveDown (player: GetIt.Player) (steps: System.Double) =
        moveBy player 0. -steps

    let moveInDirection (player: GetIt.Player) (steps: System.Double) =
        let directionRadians = Degrees.toRadians player.Direction
        moveBy
            player
            (Math.Cos(directionRadians) * steps)
            (Math.Sin(directionRadians) * steps)

    let moveToRandomPosition (player: GetIt.Player) =
        let x = rand.Next(int Model.current.SceneBounds.Left, int Model.current.SceneBounds.Right + 1)
        let y = rand.Next(int Model.current.SceneBounds.Bottom, int Model.current.SceneBounds.Top + 1)
        moveToXY player (float x) (float y)

    let setDirection (player: GetIt.Player) (angle: GetIt.Degrees) =
        InterProcessCommunication.sendCommands [ SetDirection (player.PlayerId, angle) ]

    let rotateClockwise (player: GetIt.Player) (angle: GetIt.Degrees) =
        setDirection player (player.Direction - angle)

    let rotateCounterClockwise (player: GetIt.Player) (angle: GetIt.Degrees) =
        setDirection player (player.Direction + angle)

    let turnUp (player: GetIt.Player) =
        setDirection player (Degrees.op_Implicit 90.)

    let turnRight (player: GetIt.Player) =
        setDirection player (Degrees.op_Implicit 0.)

    let turnDown (player: GetIt.Player) =
        setDirection player (Degrees.op_Implicit 270.)

    let turnLeft (player: GetIt.Player) =
        setDirection player (Degrees.op_Implicit 180.)

    let touchesEdge (player: GetIt.Player) =
        touchesLeftOrRightEdge player || touchesTopOrBottomEdge player

    let touchesPlayer (player: GetIt.Player) (other: GetIt.Player) =
        let maxLeftX = Math.Max(player.Bounds.Left, other.Bounds.Left)
        let minRightX = Math.Min(player.Bounds.Right, other.Bounds.Right)
        let maxBottomY = Math.Max(player.Bounds.Bottom, other.Bounds.Bottom)
        let minTopY = Math.Min(player.Bounds.Top, other.Bounds.Top)
        maxLeftX < minRightX && maxBottomY < minTopY

    let bounceOffWall (player: GetIt.Player) =
        if touchesTopOrBottomEdge player then setDirection player (Degrees.zero - player.Direction)
        elif touchesLeftOrRightEdge player then setDirection player (Degrees.op_Implicit 180. - player.Direction)

    let sleep (player: GetIt.Player) (durationInMilliseconds: System.Double) =
        Thread.Sleep(TimeSpan.FromMilliseconds(durationInMilliseconds))

    let say (player: GetIt.Player) (text: System.String) =
        InterProcessCommunication.sendCommands [ SetSpeechBubble (player.PlayerId, Some (Say text)) ]

    let shutUp (player: GetIt.Player) =
        InterProcessCommunication.sendCommands [ SetSpeechBubble (player.PlayerId, None) ]

    let sayWithDuration (player: GetIt.Player) (text: System.String) (durationInSeconds: System.Double) =
        say player text
        sleep player (TimeSpan.FromSeconds(durationInSeconds).TotalMilliseconds)
        shutUp player

    let ask (player: GetIt.Player) (question: System.String) =
        InterProcessCommunication.sendCommands [ SetSpeechBubble (player.PlayerId, Some (Ask { Question = question; Answer = "" })) ]

    let setPen (player: GetIt.Player) (pen: GetIt.Pen) =
        InterProcessCommunication.sendCommands [ SetPen (player.PlayerId, pen) ]

    let turnOnPen (player: GetIt.Player) =
        setPen player { player.Pen with IsOn = true }

    let turnOffPen (player: GetIt.Player) =
        setPen player { player.Pen with IsOn = false }

    let togglePenOnOff (player: GetIt.Player) =
        setPen player { player.Pen with IsOn = not player.Pen.IsOn }

    let setPenColor (player: GetIt.Player) (color: GetIt.RGBA) =
        setPen player { player.Pen with Color = color }

    let shiftPenColor (player: GetIt.Player) (angle: GetIt.Degrees) =
        setPen player { player.Pen with Color = Color.hueShift angle player.Pen.Color }

    let setPenWeight (player: GetIt.Player) (weight: System.Double) =
        setPen player { player.Pen with Weight = weight }

    let changePenWeight (player: GetIt.Player) (weight: System.Double) =
        setPenWeight player (player.Pen.Weight + weight)

    let setSizeFactor (player: GetIt.Player) (sizeFactor: System.Double) =
        InterProcessCommunication.sendCommands [ SetSizeFactor (player.PlayerId, sizeFactor) ]

    let changeSizeFactor (player: GetIt.Player) (change: System.Double) =
        setSizeFactor player (player.SizeFactor + change)

    let nextCostume (player: GetIt.Player) =
        InterProcessCommunication.sendCommands [ SetNextCostume (player.PlayerId) ]

module Turtle =
    let private getTurtleOrFail () =
        match Game.defaultTurtle with
        | Some player -> player
        | None -> failwith "Default player hasn't been added to the scene. Consider calling `Game.ShowSceneAndAddTurtle()` at the beginning."

    /// <summary>Moves the player to a position.</summary>
    /// <param name="position">The absolute destination position.</param>
    /// <returns></returns>
    [<CompiledName("MoveTo")>]
    let moveTo (position: GetIt.Position) =
        Raw.moveTo (getTurtleOrFail ()) position

    /// <summary>Moves the player to a position.</summary>
    /// <param name="x">The absolute x coordinate of the destination position.</param>
    /// <param name="y">The absolute y coordinate of the destination position.</param>
    /// <returns></returns>
    [<CompiledName("MoveTo")>]
    let moveToXY (x: System.Double) (y: System.Double) =
        Raw.moveToXY (getTurtleOrFail ()) x y

    /// <summary>Moves the player to the center of the scene.</summary>
    /// <returns></returns>
    [<CompiledName("MoveToCenter")>]
    let moveToCenter () =
        Raw.moveToCenter (getTurtleOrFail ())

    /// <summary>Moves the player to the center of the scene.</summary>
    /// <param name="deltaX">The change of the x coordinate.</param>
    /// <param name="deltaY">The change of the y coordinate.</param>
    /// <returns></returns>
    [<CompiledName("MoveBy")>]
    let moveBy (deltaX: System.Double) (deltaY: System.Double) =
        Raw.moveBy (getTurtleOrFail ()) deltaX deltaY

    /// <summary>Moves the player horizontally.</summary>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    [<CompiledName("MoveRight")>]
    let moveRight (steps: System.Double) =
        Raw.moveRight (getTurtleOrFail ()) steps

    /// <summary>Moves the player horizontally.</summary>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    [<CompiledName("MoveLeft")>]
    let moveLeft (steps: System.Double) =
        Raw.moveLeft (getTurtleOrFail ()) steps

    /// <summary>Moves the player vertically.</summary>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    [<CompiledName("MoveUp")>]
    let moveUp (steps: System.Double) =
        Raw.moveUp (getTurtleOrFail ()) steps

    /// <summary>Moves the player vertically.</summary>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    [<CompiledName("MoveDown")>]
    let moveDown (steps: System.Double) =
        Raw.moveDown (getTurtleOrFail ()) steps

    /// <summary>Moves the player forward.</summary>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    [<CompiledName("MoveInDirection")>]
    let moveInDirection (steps: System.Double) =
        Raw.moveInDirection (getTurtleOrFail ()) steps

    /// <summary>Moves the player to a random position on the scene.</summary>
    /// <returns></returns>
    [<CompiledName("MoveToRandomPosition")>]
    let moveToRandomPosition () =
        Raw.moveToRandomPosition (getTurtleOrFail ())

    /// <summary>Sets the rotation of the player to a specific angle.</summary>
    /// <param name="angle">The absolute angle.</param>
    /// <returns></returns>
    [<CompiledName("SetDirection")>]
    let setDirection (angle: GetIt.Degrees) =
        Raw.setDirection (getTurtleOrFail ()) angle

    /// <summary>Rotates the player clockwise by a specific angle.</summary>
    /// <param name="angle">The relative angle.</param>
    /// <returns></returns>
    [<CompiledName("RotateClockwise")>]
    let rotateClockwise (angle: GetIt.Degrees) =
        Raw.rotateClockwise (getTurtleOrFail ()) angle

    /// <summary>Rotates the player counter-clockwise by a specific angle.</summary>
    /// <param name="angle">The relative angle.</param>
    /// <returns></returns>
    [<CompiledName("RotateCounterClockwise")>]
    let rotateCounterClockwise (angle: GetIt.Degrees) =
        Raw.rotateCounterClockwise (getTurtleOrFail ()) angle

    /// <summary>Rotates the player so that it looks up.</summary>
    /// <returns></returns>
    [<CompiledName("TurnUp")>]
    let turnUp () =
        Raw.turnUp (getTurtleOrFail ())

    /// <summary>Rotates the player so that it looks to the right.</summary>
    /// <returns></returns>
    [<CompiledName("TurnRight")>]
    let turnRight () =
        Raw.turnRight (getTurtleOrFail ())

    /// <summary>Rotates the player so that it looks down.</summary>
    /// <returns></returns>
    [<CompiledName("TurnDown")>]
    let turnDown () =
        Raw.turnDown (getTurtleOrFail ())

    /// <summary>Rotates the player so that it looks to the left.</summary>
    /// <returns></returns>
    [<CompiledName("TurnLeft")>]
    let turnLeft () =
        Raw.turnLeft (getTurtleOrFail ())

    /// <summary>Checks whether a given player touches an edge of the scene.</summary>
    /// <returns>True, if the player touches an edge, otherwise false.</returns>
    [<CompiledName("TouchesEdge")>]
    let touchesEdge () =
        Raw.touchesEdge (getTurtleOrFail ())

    /// <summary>Checks whether a given player touches another player.</summary>
    /// <param name="other">The second player that might be touched.</param>
    /// <returns>True, if the two players touch each other, otherwise false.</returns>
    [<CompiledName("TouchesPlayer")>]
    let touchesPlayer (other: GetIt.Player) =
        Raw.touchesPlayer (getTurtleOrFail ()) other

    /// <summary>Bounces the player off the wall if it currently touches it.</summary>
    /// <returns></returns>
    [<CompiledName("BounceOffWall")>]
    let bounceOffWall () =
        Raw.bounceOffWall (getTurtleOrFail ())

    /// <summary>Pauses execution of the player for a given time.</summary>
    /// <param name="durationInMilliseconds">The length of the pause in milliseconds.</param>
    /// <returns></returns>
    [<CompiledName("Sleep")>]
    let sleep (durationInMilliseconds: System.Double) =
        Raw.sleep (getTurtleOrFail ()) durationInMilliseconds

    /// <summary>Shows a speech bubble next to the player. You can remove the speech bubble with <see cref="ShutUp"/>.</summary>
    /// <param name="text">The content of the speech bubble.</param>
    /// <returns></returns>
    [<CompiledName("Say")>]
    let say (text: System.String) =
        Raw.say (getTurtleOrFail ()) text

    /// <summary>Removes the speech bubble of the player.</summary>
    /// <returns></returns>
    [<CompiledName("ShutUp")>]
    let shutUp () =
        Raw.shutUp (getTurtleOrFail ())

    /// <summary>Shows a speech bubble next to the player for a specific time.</summary>
    /// <param name="text">The content of the speech bubble.</param>
    /// <param name="durationInSeconds">The number of seconds how long the speech bubble should be visible.</param>
    /// <returns></returns>
    [<CompiledName("Say")>]
    let sayWithDuration (text: System.String) (durationInSeconds: System.Double) =
        Raw.sayWithDuration (getTurtleOrFail ()) text durationInSeconds

    /// <summary>Shows a speech bubble with a text box next to the player and waits for the user to fill in the text box.</summary>
    /// <param name="question">The content of the speech bubble.</param>
    /// <returns>The text the user typed in.</returns>
    [<CompiledName("Ask")>]
    let ask (question: System.String) =
        Raw.ask (getTurtleOrFail ()) question

    /// <summary>Sets the pen of the player.</summary>
    /// <param name="pen">The pen that should be assigned to the player.</param>
    /// <returns></returns>
    [<CompiledName("SetPen")>]
    let setPen (pen: GetIt.Pen) =
        Raw.setPen (getTurtleOrFail ()) pen

    /// <summary>Turns on the pen of the player.</summary>
    /// <returns></returns>
    [<CompiledName("TurnOnPen")>]
    let turnOnPen () =
        Raw.turnOnPen (getTurtleOrFail ())

    /// <summary>Turns off the pen of the player.</summary>
    /// <returns></returns>
    [<CompiledName("TurnOffPen")>]
    let turnOffPen () =
        Raw.turnOffPen (getTurtleOrFail ())

    /// <summary>Turns on the pen of the player if it is turned off. Turns off the pen of the player if it is turned on.</summary>
    /// <returns></returns>
    [<CompiledName("TogglePenOnOff")>]
    let togglePenOnOff () =
        Raw.togglePenOnOff (getTurtleOrFail ())

    /// <summary>Sets the pen color of the player.</summary>
    /// <param name="color">The new color of the pen.</param>
    /// <returns></returns>
    [<CompiledName("SetPenColor")>]
    let setPenColor (color: GetIt.RGBA) =
        Raw.setPenColor (getTurtleOrFail ()) color

    /// <summary>Shifts the HUE value of the pen color.</summary>
    /// <param name="angle">The angle that the HUE value should be shifted by.</param>
    /// <returns></returns>
    [<CompiledName("ShiftPenColor")>]
    let shiftPenColor (angle: GetIt.Degrees) =
        Raw.shiftPenColor (getTurtleOrFail ()) angle

    /// <summary>Sets the weight of the pen.</summary>
    /// <param name="weight">The new weight of the pen.</param>
    /// <returns></returns>
    [<CompiledName("SetPenWeight")>]
    let setPenWeight (weight: System.Double) =
        Raw.setPenWeight (getTurtleOrFail ()) weight

    /// <summary>Changes the weight of the pen.</summary>
    /// <param name="weight">The change of the pen weight.</param>
    /// <returns></returns>
    [<CompiledName("ChangePenWeight")>]
    let changePenWeight (weight: System.Double) =
        Raw.changePenWeight (getTurtleOrFail ()) weight

    /// <summary>Sets the size of the player by multiplying the original size with a factor.</summary>
    /// <param name="sizeFactor">The factor the original size should be multiplied by.</param>
    /// <returns></returns>
    [<CompiledName("SetSizeFactor")>]
    let setSizeFactor (sizeFactor: System.Double) =
        Raw.setSizeFactor (getTurtleOrFail ()) sizeFactor

    /// <summary>Changes the size factor of the player that the original size is multiplied by.</summary>
    /// <param name="change">The change of the size factor.</param>
    /// <returns></returns>
    [<CompiledName("ChangeSizeFactor")>]
    let changeSizeFactor (change: System.Double) =
        Raw.changeSizeFactor (getTurtleOrFail ()) change

    /// <summary>Changes the costume of the player.</summary>
    /// <returns></returns>
    [<CompiledName("NextCostume")>]
    let nextCostume () =
        Raw.nextCostume (getTurtleOrFail ())

open System.Runtime.CompilerServices

[<Extension>]
type PlayerExtensions() =
    /// <summary>Moves the player to a position.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="position">The absolute destination position.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveTo(player: GetIt.Player, position: GetIt.Position) =
        Raw.moveTo player position

    /// <summary>Moves the player to a position.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="x">The absolute x coordinate of the destination position.</param>
    /// <param name="y">The absolute y coordinate of the destination position.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveTo(player: GetIt.Player, x: System.Double, y: System.Double) =
        Raw.moveToXY player x y

    /// <summary>Moves the player to the center of the scene.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveToCenter(player: GetIt.Player) =
        Raw.moveToCenter player

    /// <summary>Moves the player to the center of the scene.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="deltaX">The change of the x coordinate.</param>
    /// <param name="deltaY">The change of the y coordinate.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveBy(player: GetIt.Player, deltaX: System.Double, deltaY: System.Double) =
        Raw.moveBy player deltaX deltaY

    /// <summary>Moves the player horizontally.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveRight(player: GetIt.Player, steps: System.Double) =
        Raw.moveRight player steps

    /// <summary>Moves the player horizontally.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveLeft(player: GetIt.Player, steps: System.Double) =
        Raw.moveLeft player steps

    /// <summary>Moves the player vertically.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveUp(player: GetIt.Player, steps: System.Double) =
        Raw.moveUp player steps

    /// <summary>Moves the player vertically.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveDown(player: GetIt.Player, steps: System.Double) =
        Raw.moveDown player steps

    /// <summary>Moves the player forward.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveInDirection(player: GetIt.Player, steps: System.Double) =
        Raw.moveInDirection player steps

    /// <summary>Moves the player to a random position on the scene.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveToRandomPosition(player: GetIt.Player) =
        Raw.moveToRandomPosition player

    /// <summary>Sets the rotation of the player to a specific angle.</summary>
    /// <param name="player">The player that should be rotated.</param>
    /// <param name="angle">The absolute angle.</param>
    /// <returns></returns>
    [<Extension>]
    static member SetDirection(player: GetIt.Player, angle: GetIt.Degrees) =
        Raw.setDirection player angle

    /// <summary>Rotates the player clockwise by a specific angle.</summary>
    /// <param name="player">The player that should be rotated.</param>
    /// <param name="angle">The relative angle.</param>
    /// <returns></returns>
    [<Extension>]
    static member RotateClockwise(player: GetIt.Player, angle: GetIt.Degrees) =
        Raw.rotateClockwise player angle

    /// <summary>Rotates the player counter-clockwise by a specific angle.</summary>
    /// <param name="player">The player that should be rotated.</param>
    /// <param name="angle">The relative angle.</param>
    /// <returns></returns>
    [<Extension>]
    static member RotateCounterClockwise(player: GetIt.Player, angle: GetIt.Degrees) =
        Raw.rotateCounterClockwise player angle

    /// <summary>Rotates the player so that it looks up.</summary>
    /// <param name="player">The player that should be rotated.</param>
    /// <returns></returns>
    [<Extension>]
    static member TurnUp(player: GetIt.Player) =
        Raw.turnUp player

    /// <summary>Rotates the player so that it looks to the right.</summary>
    /// <param name="player">The player that should be rotated.</param>
    /// <returns></returns>
    [<Extension>]
    static member TurnRight(player: GetIt.Player) =
        Raw.turnRight player

    /// <summary>Rotates the player so that it looks down.</summary>
    /// <param name="player">The player that should be rotated.</param>
    /// <returns></returns>
    [<Extension>]
    static member TurnDown(player: GetIt.Player) =
        Raw.turnDown player

    /// <summary>Rotates the player so that it looks to the left.</summary>
    /// <param name="player">The player that should be rotated.</param>
    /// <returns></returns>
    [<Extension>]
    static member TurnLeft(player: GetIt.Player) =
        Raw.turnLeft player

    /// <summary>Checks whether a given player touches an edge of the scene.</summary>
    /// <param name="player">The player that might touch an edge of the scene.</param>
    /// <returns>True, if the player touches an edge, otherwise false.</returns>
    [<Extension>]
    static member TouchesEdge(player: GetIt.Player) =
        Raw.touchesEdge player

    /// <summary>Checks whether a given player touches another player.</summary>
    /// <param name="player">The first player that might be touched.</param>
    /// <param name="other">The second player that might be touched.</param>
    /// <returns>True, if the two players touch each other, otherwise false.</returns>
    [<Extension>]
    static member TouchesPlayer(player: GetIt.Player, other: GetIt.Player) =
        Raw.touchesPlayer player other

    /// <summary>Bounces the player off the wall if it currently touches it.</summary>
    /// <param name="player">The player that should bounce off the wall.</param>
    /// <returns></returns>
    [<Extension>]
    static member BounceOffWall(player: GetIt.Player) =
        Raw.bounceOffWall player

    /// <summary>Pauses execution of the player for a given time.</summary>
    /// <param name="player">The player that pauses execution.</param>
    /// <param name="durationInMilliseconds">The length of the pause in milliseconds.</param>
    /// <returns></returns>
    [<Extension>]
    static member Sleep(player: GetIt.Player, durationInMilliseconds: System.Double) =
        Raw.sleep player durationInMilliseconds

    /// <summary>Shows a speech bubble next to the player. You can remove the speech bubble with <see cref="ShutUp"/>.</summary>
    /// <param name="player">The player that the speech bubble belongs to.</param>
    /// <param name="text">The content of the speech bubble.</param>
    /// <returns></returns>
    [<Extension>]
    static member Say(player: GetIt.Player, text: System.String) =
        Raw.say player text

    /// <summary>Removes the speech bubble of the player.</summary>
    /// <param name="player">The player that the speech bubble belongs to.</param>
    /// <returns></returns>
    [<Extension>]
    static member ShutUp(player: GetIt.Player) =
        Raw.shutUp player

    /// <summary>Shows a speech bubble next to the player for a specific time.</summary>
    /// <param name="player">The player that the speech bubble belongs to.</param>
    /// <param name="text">The content of the speech bubble.</param>
    /// <param name="durationInSeconds">The number of seconds how long the speech bubble should be visible.</param>
    /// <returns></returns>
    [<Extension>]
    static member Say(player: GetIt.Player, text: System.String, durationInSeconds: System.Double) =
        Raw.sayWithDuration player text durationInSeconds

    /// <summary>Shows a speech bubble with a text box next to the player and waits for the user to fill in the text box.</summary>
    /// <param name="player">The player that the speech bubble belongs to.</param>
    /// <param name="question">The content of the speech bubble.</param>
    /// <returns>The text the user typed in.</returns>
    [<Extension>]
    static member Ask(player: GetIt.Player, question: System.String) =
        Raw.ask player question

    /// <summary>Sets the pen of the player.</summary>
    /// <param name="player">The player that should get the pen.</param>
    /// <param name="pen">The pen that should be assigned to the player.</param>
    /// <returns></returns>
    [<Extension>]
    static member SetPen(player: GetIt.Player, pen: GetIt.Pen) =
        Raw.setPen player pen

    /// <summary>Turns on the pen of the player.</summary>
    /// <param name="player">The player that should get its pen turned on.</param>
    /// <returns></returns>
    [<Extension>]
    static member TurnOnPen(player: GetIt.Player) =
        Raw.turnOnPen player

    /// <summary>Turns off the pen of the player.</summary>
    /// <param name="player">The player that should get its pen turned off.</param>
    /// <returns></returns>
    [<Extension>]
    static member TurnOffPen(player: GetIt.Player) =
        Raw.turnOffPen player

    /// <summary>Turns on the pen of the player if it is turned off. Turns off the pen of the player if it is turned on.</summary>
    /// <param name="player">The player that should get its pen toggled.</param>
    /// <returns></returns>
    [<Extension>]
    static member TogglePenOnOff(player: GetIt.Player) =
        Raw.togglePenOnOff player

    /// <summary>Sets the pen color of the player.</summary>
    /// <param name="player">The player that should get its pen color set.</param>
    /// <param name="color">The new color of the pen.</param>
    /// <returns></returns>
    [<Extension>]
    static member SetPenColor(player: GetIt.Player, color: GetIt.RGBA) =
        Raw.setPenColor player color

    /// <summary>Shifts the HUE value of the pen color.</summary>
    /// <param name="player">The player that should get its pen color shifted.</param>
    /// <param name="angle">The angle that the HUE value should be shifted by.</param>
    /// <returns></returns>
    [<Extension>]
    static member ShiftPenColor(player: GetIt.Player, angle: GetIt.Degrees) =
        Raw.shiftPenColor player angle

    /// <summary>Sets the weight of the pen.</summary>
    /// <param name="player">The player that gets its pen weight set.</param>
    /// <param name="weight">The new weight of the pen.</param>
    /// <returns></returns>
    [<Extension>]
    static member SetPenWeight(player: GetIt.Player, weight: System.Double) =
        Raw.setPenWeight player weight

    /// <summary>Changes the weight of the pen.</summary>
    /// <param name="player">The player that gets its pen weight changed.</param>
    /// <param name="weight">The change of the pen weight.</param>
    /// <returns></returns>
    [<Extension>]
    static member ChangePenWeight(player: GetIt.Player, weight: System.Double) =
        Raw.changePenWeight player weight

    /// <summary>Sets the size of the player by multiplying the original size with a factor.</summary>
    /// <param name="player">The player that gets its size changed.</param>
    /// <param name="sizeFactor">The factor the original size should be multiplied by.</param>
    /// <returns></returns>
    [<Extension>]
    static member SetSizeFactor(player: GetIt.Player, sizeFactor: System.Double) =
        Raw.setSizeFactor player sizeFactor

    /// <summary>Changes the size factor of the player that the original size is multiplied by.</summary>
    /// <param name="player">The player that gets its size changed.</param>
    /// <param name="change">The change of the size factor.</param>
    /// <returns></returns>
    [<Extension>]
    static member ChangeSizeFactor(player: GetIt.Player, change: System.Double) =
        Raw.changeSizeFactor player change

    /// <summary>Changes the costume of the player.</summary>
    /// <param name="player">The player that gets its costume changed.</param>
    /// <returns></returns>
    [<Extension>]
    static member NextCostume(player: GetIt.Player) =
        Raw.nextCostume player
