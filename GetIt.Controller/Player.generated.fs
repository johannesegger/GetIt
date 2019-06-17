namespace GetIt

open System
open System.Threading

module private Raw =
    let private rand = Random()

    let private touchesTopOrBottomEdge (player: GetIt.Player) =
        player.Bounds.Top > Model.getCurrent().SceneBounds.Top || player.Bounds.Bottom < Model.getCurrent().SceneBounds.Bottom

    let private touchesLeftOrRightEdge (player: GetIt.Player) =
        player.Bounds.Right > Model.getCurrent().SceneBounds.Right || player.Bounds.Left < Model.getCurrent().SceneBounds.Left

    let moveTo (player: GetIt.Player) (position: GetIt.Position) =
        Connection.run UICommunication.setPosition player.PlayerId position

    let moveToXY (player: GetIt.Player) (x: System.Double) (y: System.Double) =
        moveTo player { X = x; Y = y }

    let moveToCenter (player: GetIt.Player) =
        moveTo player Position.zero

    let moveBy (player: GetIt.Player) (deltaX: System.Double) (deltaY: System.Double) =
        Connection.run UICommunication.changePosition player.PlayerId { X = deltaX; Y = deltaY }

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
        let x = rand.Next(int (Model.getCurrent().SceneBounds.Left), int (Model.getCurrent().SceneBounds.Right) + 1)
        let y = rand.Next(int (Model.getCurrent().SceneBounds.Bottom), int (Model.getCurrent().SceneBounds.Top) + 1)
        moveToXY player (float x) (float y)

    let setDirection (player: GetIt.Player) (angle: GetIt.Degrees) =
        Connection.run UICommunication.setDirection player.PlayerId angle

    let turnUp (player: GetIt.Player) =
        setDirection player (Degrees.op_Implicit 90.)

    let turnRight (player: GetIt.Player) =
        setDirection player (Degrees.op_Implicit 0.)

    let turnDown (player: GetIt.Player) =
        setDirection player (Degrees.op_Implicit 270.)

    let turnLeft (player: GetIt.Player) =
        setDirection player (Degrees.op_Implicit 180.)

    let rotateClockwise (player: GetIt.Player) (angle: GetIt.Degrees) =
        Connection.run UICommunication.changeDirection player.PlayerId -angle

    let rotateCounterClockwise (player: GetIt.Player) (angle: GetIt.Degrees) =
        rotateClockwise player -angle

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

    let sleep (player: GetIt.Player) (duration: System.TimeSpan) =
        Thread.Sleep duration

    let sleepMilliseconds (player: GetIt.Player) (durationInMilliseconds: System.Double) =
        sleep player (TimeSpan.FromMilliseconds durationInMilliseconds)

    let say (player: GetIt.Player) (text: System.String) =
        Connection.run UICommunication.say player.PlayerId text

    let shutUp (player: GetIt.Player) =
        Connection.run UICommunication.shutUp player.PlayerId

    let sayWithDuration (player: GetIt.Player) (text: System.String) (duration: System.TimeSpan) =
        say player text
        sleep player duration
        shutUp player

    let sayWithDurationInSeconds (player: GetIt.Player) (text: System.String) (durationInSeconds: System.Double) =
        sayWithDuration player text (TimeSpan.FromSeconds durationInSeconds)

    let ask (player: GetIt.Player) (question: System.String) =
        Connection.run UICommunication.ask player.PlayerId question

    let askBool (player: GetIt.Player) (question: System.String) =
        Connection.run UICommunication.askBool player.PlayerId question

    let turnOnPen (player: GetIt.Player) =
        Connection.run UICommunication.setPenState player.PlayerId true

    let turnOffPen (player: GetIt.Player) =
        Connection.run UICommunication.setPenState player.PlayerId false

    let togglePenState (player: GetIt.Player) =
        Connection.run UICommunication.togglePenState player.PlayerId

    let setPenColor (player: GetIt.Player) (color: GetIt.RGBAColor) =
        Connection.run UICommunication.setPenColor player.PlayerId color

    let shiftPenColor (player: GetIt.Player) (angle: GetIt.Degrees) =
        Connection.run UICommunication.shiftPenColor player.PlayerId angle

    let setPenWeight (player: GetIt.Player) (weight: System.Double) =
        Connection.run UICommunication.setPenWeight player.PlayerId weight

    let changePenWeight (player: GetIt.Player) (weight: System.Double) =
        Connection.run UICommunication.changePenWeight player.PlayerId weight

    let setSizeFactor (player: GetIt.Player) (sizeFactor: System.Double) =
        Connection.run UICommunication.setSizeFactor player.PlayerId sizeFactor

    let changeSizeFactor (player: GetIt.Player) (change: System.Double) =
        Connection.run UICommunication.changeSizeFactor player.PlayerId change

    let nextCostume (player: GetIt.Player) =
        Connection.run UICommunication.setNextCostume player.PlayerId

    let sendToBack (player: GetIt.Player) =
        Connection.run UICommunication.sendToBack player.PlayerId

    let bringToFront (player: GetIt.Player) =
        Connection.run UICommunication.bringToFront player.PlayerId

    let getDirectionToMouse (player: GetIt.Player) =
        player.Position |> Position.angleTo (Model.getCurrent().MouseState.Position)

    let getDistanceToMouse (player: GetIt.Player) =
        player.Position |> Position.distanceTo (Model.getCurrent().MouseState.Position)

    let getDirectionTo (player1: GetIt.Player) (player2: GetIt.Player) =
        player1.Position |> Position.angleTo player2.Position

    let getDistanceTo (player1: GetIt.Player) (player2: GetIt.Player) =
        player1.Position |> Position.distanceTo player2.Position

    let show (player: GetIt.Player) =
        Connection.run UICommunication.setVisibility player.PlayerId true

    let hide (player: GetIt.Player) =
        Connection.run UICommunication.setVisibility player.PlayerId false

    let onKeyDown (player: GetIt.Player) (key: GetIt.KeyboardKey) (action: System.Action<GetIt.Player>) =
        Model.onKeyDown key (fun () -> action.Invoke player)

    let onAnyKeyDown (player: GetIt.Player) (action: System.Action<GetIt.Player, GetIt.KeyboardKey>) =
        Model.onAnyKeyDown (fun key -> action.Invoke(player, key))

    let whileKeyDown (player: GetIt.Player) (key: GetIt.KeyboardKey) (interval: System.TimeSpan) (action: System.Action<GetIt.Player, System.Int32>) =
        Model.whileKeyDown key interval (fun i -> action.Invoke(player, i))

    let whileAnyKeyDown (player: GetIt.Player) (interval: System.TimeSpan) (action: System.Action<GetIt.Player, GetIt.KeyboardKey, System.Int32>) =
        Model.whileAnyKeyDown interval (fun key i -> action.Invoke(player, key, i))

    let onMouseEnter (player: GetIt.Player) (action: System.Action<GetIt.Player>) =
        Model.onEnterPlayer player.PlayerId (fun () -> action.Invoke(player))

    let onClick (player: GetIt.Player) (action: System.Action<GetIt.Player, GetIt.MouseClick>) =
        Model.onClickPlayer player.PlayerId (fun mouseClick -> action.Invoke(player, mouseClick))

[<AbstractClass; Sealed>]
type Turtle() =
    static member private Player
        with get () =
            match Game.defaultTurtle with
            | Some player -> player
            | None -> raise (GetItException "Default player hasn't been added to the scene. Consider calling `Game.ShowSceneAndAddTurtle()` at the beginning.")
    
    /// The actual size of the player.
    static member Size with get () = Turtle.Player.Size
    
    /// The factor that is used to change the size of the player.
    static member SizeFactor with get () = Turtle.Player.SizeFactor
    
    /// The position of the player's center point.
    static member Position with get () = Turtle.Player.Position
    
    /// The rectangular bounds of the player.
    /// Note that this doesn't take into account the current rotation of the player.
    static member Bounds with get () = Turtle.Player.Bounds
    
    /// The rotation of the player.
    static member Direction with get () = Turtle.Player.Direction
    
    /// The pen that belongs to the player.
    static member Pen with get () = Turtle.Player.Pen

    /// <summary>Moves the player to a position.</summary>
    /// <param name="position">The absolute destination position.</param>
    /// <returns></returns>
    static member MoveTo (position: GetIt.Position) =
        if obj.ReferenceEquals(position, null) then raise (ArgumentNullException "position")
        Raw.moveTo Turtle.Player position

    /// <summary>Moves the player to a position.</summary>
    /// <param name="x">The absolute x coordinate of the destination position.</param>
    /// <param name="y">The absolute y coordinate of the destination position.</param>
    /// <returns></returns>
    static member MoveTo (x: System.Double, y: System.Double) =
        Raw.moveToXY Turtle.Player x y

    /// <summary>Moves the player to the center of the scene.</summary>
    /// <returns></returns>
    static member MoveToCenter () =
        Raw.moveToCenter Turtle.Player

    /// <summary>Moves the player relatively.</summary>
    /// <param name="deltaX">The change of the x coordinate.</param>
    /// <param name="deltaY">The change of the y coordinate.</param>
    /// <returns></returns>
    static member MoveBy (deltaX: System.Double, deltaY: System.Double) =
        Raw.moveBy Turtle.Player deltaX deltaY

    /// <summary>Moves the player horizontally.</summary>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    static member MoveRight (steps: System.Double) =
        Raw.moveRight Turtle.Player steps

    /// <summary>Moves the player horizontally.</summary>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    static member MoveLeft (steps: System.Double) =
        Raw.moveLeft Turtle.Player steps

    /// <summary>Moves the player vertically.</summary>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    static member MoveUp (steps: System.Double) =
        Raw.moveUp Turtle.Player steps

    /// <summary>Moves the player vertically.</summary>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    static member MoveDown (steps: System.Double) =
        Raw.moveDown Turtle.Player steps

    /// <summary>Moves the player forward.</summary>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    static member MoveInDirection (steps: System.Double) =
        Raw.moveInDirection Turtle.Player steps

    /// <summary>Moves the player to a random position on the scene.</summary>
    /// <returns></returns>
    static member MoveToRandomPosition () =
        Raw.moveToRandomPosition Turtle.Player

    /// <summary>Sets the rotation of the player to a specific angle.</summary>
    /// <param name="angle">The absolute angle.</param>
    /// <returns></returns>
    static member SetDirection (angle: GetIt.Degrees) =
        if obj.ReferenceEquals(angle, null) then raise (ArgumentNullException "angle")
        Raw.setDirection Turtle.Player angle

    /// <summary>Rotates the player so that it looks up.</summary>
    /// <returns></returns>
    static member TurnUp () =
        Raw.turnUp Turtle.Player

    /// <summary>Rotates the player so that it looks to the right.</summary>
    /// <returns></returns>
    static member TurnRight () =
        Raw.turnRight Turtle.Player

    /// <summary>Rotates the player so that it looks down.</summary>
    /// <returns></returns>
    static member TurnDown () =
        Raw.turnDown Turtle.Player

    /// <summary>Rotates the player so that it looks to the left.</summary>
    /// <returns></returns>
    static member TurnLeft () =
        Raw.turnLeft Turtle.Player

    /// <summary>Rotates the player clockwise by a specific angle.</summary>
    /// <param name="angle">The relative angle.</param>
    /// <returns></returns>
    static member RotateClockwise (angle: GetIt.Degrees) =
        if obj.ReferenceEquals(angle, null) then raise (ArgumentNullException "angle")
        Raw.rotateClockwise Turtle.Player angle

    /// <summary>Rotates the player counter-clockwise by a specific angle.</summary>
    /// <param name="angle">The relative angle.</param>
    /// <returns></returns>
    static member RotateCounterClockwise (angle: GetIt.Degrees) =
        if obj.ReferenceEquals(angle, null) then raise (ArgumentNullException "angle")
        Raw.rotateCounterClockwise Turtle.Player angle

    /// <summary>Checks whether a given player touches an edge of the scene.</summary>
    /// <returns>True, if the player touches an edge, otherwise false.</returns>
    static member TouchesEdge () =
        Raw.touchesEdge Turtle.Player

    /// <summary>Checks whether a given player touches another player.</summary>
    /// <param name="other">The second player that might be touched.</param>
    /// <returns>True, if the two players touch each other, otherwise false.</returns>
    static member TouchesPlayer (other: GetIt.Player) =
        if obj.ReferenceEquals(other, null) then raise (ArgumentNullException "other")
        Raw.touchesPlayer Turtle.Player other

    /// <summary>Bounces the player off the wall if it currently touches it.</summary>
    /// <returns></returns>
    static member BounceOffWall () =
        Raw.bounceOffWall Turtle.Player

    /// <summary>Pauses execution of the player for a given time.</summary>
    /// <param name="duration">The length of the pause.</param>
    /// <returns></returns>
    static member Sleep (duration: System.TimeSpan) =
        Raw.sleep Turtle.Player duration

    /// <summary>Pauses execution of the player for a given time.</summary>
    /// <param name="durationInMilliseconds">The length of the pause in milliseconds.</param>
    /// <returns></returns>
    static member Sleep (durationInMilliseconds: System.Double) =
        Raw.sleepMilliseconds Turtle.Player durationInMilliseconds

    /// <summary>Shows a speech bubble next to the player. You can remove the speech bubble with <see cref="ShutUp"/>.</summary>
    /// <param name="text">The content of the speech bubble.</param>
    /// <returns></returns>
    static member Say (text: System.String) =
        if obj.ReferenceEquals(text, null) then raise (ArgumentNullException "text")
        Raw.say Turtle.Player text

    /// <summary>Removes the speech bubble of the player.</summary>
    /// <returns></returns>
    static member ShutUp () =
        Raw.shutUp Turtle.Player

    /// <summary>Shows a speech bubble next to the player for a specific time.</summary>
    /// <param name="text">The content of the speech bubble.</param>
    /// <param name="duration">The time span how long the speech bubble should be visible.</param>
    /// <returns></returns>
    static member Say (text: System.String, duration: System.TimeSpan) =
        if obj.ReferenceEquals(text, null) then raise (ArgumentNullException "text")
        Raw.sayWithDuration Turtle.Player text duration

    /// <summary>Shows a speech bubble next to the player for a specific time.</summary>
    /// <param name="text">The content of the speech bubble.</param>
    /// <param name="durationInSeconds">The number of seconds how long the speech bubble should be visible.</param>
    /// <returns></returns>
    static member Say (text: System.String, durationInSeconds: System.Double) =
        if obj.ReferenceEquals(text, null) then raise (ArgumentNullException "text")
        Raw.sayWithDurationInSeconds Turtle.Player text durationInSeconds

    /// <summary>Shows a speech bubble with a text box next to the player and waits for the user to fill in the text box.</summary>
    /// <param name="question">The content of the speech bubble.</param>
    /// <returns>The text the user typed in.</returns>
    static member Ask (question: System.String) =
        if obj.ReferenceEquals(question, null) then raise (ArgumentNullException "question")
        Raw.ask Turtle.Player question

    /// <summary>Shows a speech bubble with two buttons "confirm" and "decline" next to the player and waits for the user to press one of the buttons.</summary>
    /// <param name="question">The content of the speech bubble.</param>
    /// <returns>True, if the user pressed the "confirm" button, false otherwise.</returns>
    static member AskBool (question: System.String) =
        if obj.ReferenceEquals(question, null) then raise (ArgumentNullException "question")
        Raw.askBool Turtle.Player question

    /// <summary>Turns on the pen of the player.</summary>
    /// <returns></returns>
    static member TurnOnPen () =
        Raw.turnOnPen Turtle.Player

    /// <summary>Turns off the pen of the player.</summary>
    /// <returns></returns>
    static member TurnOffPen () =
        Raw.turnOffPen Turtle.Player

    /// <summary>Turns on the pen of the player if it is turned off. Turns off the pen of the player if it is turned on.</summary>
    /// <returns></returns>
    static member TogglePenState () =
        Raw.togglePenState Turtle.Player

    /// <summary>Sets the pen color of the player.</summary>
    /// <param name="color">The new color of the pen.</param>
    /// <returns></returns>
    static member SetPenColor (color: GetIt.RGBAColor) =
        if obj.ReferenceEquals(color, null) then raise (ArgumentNullException "color")
        Raw.setPenColor Turtle.Player color

    /// <summary>Shifts the HUE value of the pen color.</summary>
    /// <param name="angle">The angle that the HUE value should be shifted by.</param>
    /// <returns></returns>
    static member ShiftPenColor (angle: GetIt.Degrees) =
        if obj.ReferenceEquals(angle, null) then raise (ArgumentNullException "angle")
        Raw.shiftPenColor Turtle.Player angle

    /// <summary>Sets the weight of the pen.</summary>
    /// <param name="weight">The new weight of the pen.</param>
    /// <returns></returns>
    static member SetPenWeight (weight: System.Double) =
        Raw.setPenWeight Turtle.Player weight

    /// <summary>Changes the weight of the pen.</summary>
    /// <param name="weight">The change of the pen weight.</param>
    /// <returns></returns>
    static member ChangePenWeight (weight: System.Double) =
        Raw.changePenWeight Turtle.Player weight

    /// <summary>Sets the size of the player by multiplying the original size with a factor.</summary>
    /// <param name="sizeFactor">The factor the original size should be multiplied by.</param>
    /// <returns></returns>
    static member SetSizeFactor (sizeFactor: System.Double) =
        Raw.setSizeFactor Turtle.Player sizeFactor

    /// <summary>Changes the size factor of the player that the original size is multiplied by.</summary>
    /// <param name="change">The change of the size factor.</param>
    /// <returns></returns>
    static member ChangeSizeFactor (change: System.Double) =
        Raw.changeSizeFactor Turtle.Player change

    /// <summary>Changes the costume of the player.</summary>
    /// <returns></returns>
    static member NextCostume () =
        Raw.nextCostume Turtle.Player

    /// <summary>Sends the player to the back of the scene so that other players will overlap the current player.</summary>
    /// <returns></returns>
    static member SendToBack () =
        Raw.sendToBack Turtle.Player

    /// <summary>Sends the player to the front of the scene so that the current player will overlap other players.</summary>
    /// <returns></returns>
    static member BringToFront () =
        Raw.bringToFront Turtle.Player

    /// <summary>Calculates the direction from the player to the mouse pointer.</summary>
    /// <returns>The direction from the player to the mouse pointer.</returns>
    static member GetDirectionToMouse () =
        Raw.getDirectionToMouse Turtle.Player

    /// <summary>Calculates the distance from the player to the mouse pointer.</summary>
    /// <returns>The distance from the player to the mouse pointer.</returns>
    static member GetDistanceToMouse () =
        Raw.getDistanceToMouse Turtle.Player

    /// <summary>Calculates the direction from the player to another player.</summary>
    /// <param name="player2">The other player.</param>
    /// <returns>The direction from the player to another player.</returns>
    static member GetDirectionTo (player2: GetIt.Player) =
        if obj.ReferenceEquals(player2, null) then raise (ArgumentNullException "player2")
        Raw.getDirectionTo Turtle.Player player2

    /// <summary>Calculates the distance from the player to another player.</summary>
    /// <param name="player2">The other player.</param>
    /// <returns>The distance from the player to another player.</returns>
    static member GetDistanceTo (player2: GetIt.Player) =
        if obj.ReferenceEquals(player2, null) then raise (ArgumentNullException "player2")
        Raw.getDistanceTo Turtle.Player player2

    /// <summary>Shows the player that has been hidden using <see cref="Hide"/>.</summary>
    /// <returns></returns>
    static member Show () =
        Raw.show Turtle.Player

    /// <summary>Hides the player. Use <see cref="Show"/> to unhide the player.</summary>
    /// <returns></returns>
    static member Hide () =
        Raw.hide Turtle.Player

    /// <summary>Registers an event handler that is called once when a specific keyboard key is pressed.</summary>
    /// <param name="key">The keyboard key that should be listened to.</param>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnKeyDown (key: GetIt.KeyboardKey, action: System.Action<GetIt.Player>) =
        if obj.ReferenceEquals(key, null) then raise (ArgumentNullException "key")
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")
        Raw.onKeyDown Turtle.Player key action

    /// <summary>Registers an event handler that is called once when any keyboard key is pressed.</summary>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnAnyKeyDown (action: System.Action<GetIt.Player, GetIt.KeyboardKey>) =
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")
        Raw.onAnyKeyDown Turtle.Player action

    /// <summary>Registers an event handler that is called contiuously when a specific keyboard key is pressed.</summary>
    /// <param name="key">The keyboard key that should be listened to.</param>
    /// <param name="interval">How often the event handler should be called.</param>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnKeyDown (key: GetIt.KeyboardKey, interval: System.TimeSpan, action: System.Action<GetIt.Player, System.Int32>) =
        if obj.ReferenceEquals(key, null) then raise (ArgumentNullException "key")
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")
        Raw.whileKeyDown Turtle.Player key interval action

    /// <summary>Registers an event handler that is called contiuously when any keyboard key is pressed.</summary>
    /// <param name="interval">How often the event handler should be called.</param>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnAnyKeyDown (interval: System.TimeSpan, action: System.Action<GetIt.Player, GetIt.KeyboardKey, System.Int32>) =
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")
        Raw.whileAnyKeyDown Turtle.Player interval action

    /// <summary>Registers an event handler that is called when the mouse enters the player area.</summary>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnMouseEnter (action: System.Action<GetIt.Player>) =
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")
        Raw.onMouseEnter Turtle.Player action

    /// <summary>Registers an event handler that is called when the mouse is clicked on the player.</summary>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnClick (action: System.Action<GetIt.Player, GetIt.MouseClick>) =
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")
        Raw.onClick Turtle.Player action

open System.Runtime.CompilerServices

[<Extension>]
type PlayerExtensions() =
    /// <summary>Moves the player to a position.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="position">The absolute destination position.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveTo(player: GetIt.Player, position: GetIt.Position) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        if obj.ReferenceEquals(position, null) then raise (ArgumentNullException "position")
        Raw.moveTo player position

    /// <summary>Moves the player to a position.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="x">The absolute x coordinate of the destination position.</param>
    /// <param name="y">The absolute y coordinate of the destination position.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveTo(player: GetIt.Player, x: System.Double, y: System.Double) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.moveToXY player x y

    /// <summary>Moves the player to the center of the scene.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveToCenter(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.moveToCenter player

    /// <summary>Moves the player relatively.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="deltaX">The change of the x coordinate.</param>
    /// <param name="deltaY">The change of the y coordinate.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveBy(player: GetIt.Player, deltaX: System.Double, deltaY: System.Double) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.moveBy player deltaX deltaY

    /// <summary>Moves the player horizontally.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveRight(player: GetIt.Player, steps: System.Double) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.moveRight player steps

    /// <summary>Moves the player horizontally.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveLeft(player: GetIt.Player, steps: System.Double) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.moveLeft player steps

    /// <summary>Moves the player vertically.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveUp(player: GetIt.Player, steps: System.Double) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.moveUp player steps

    /// <summary>Moves the player vertically.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveDown(player: GetIt.Player, steps: System.Double) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.moveDown player steps

    /// <summary>Moves the player forward.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <param name="steps">The number of steps.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveInDirection(player: GetIt.Player, steps: System.Double) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.moveInDirection player steps

    /// <summary>Moves the player to a random position on the scene.</summary>
    /// <param name="player">The player that should be moved.</param>
    /// <returns></returns>
    [<Extension>]
    static member MoveToRandomPosition(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.moveToRandomPosition player

    /// <summary>Sets the rotation of the player to a specific angle.</summary>
    /// <param name="player">The player that should be rotated.</param>
    /// <param name="angle">The absolute angle.</param>
    /// <returns></returns>
    [<Extension>]
    static member SetDirection(player: GetIt.Player, angle: GetIt.Degrees) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        if obj.ReferenceEquals(angle, null) then raise (ArgumentNullException "angle")
        Raw.setDirection player angle

    /// <summary>Rotates the player so that it looks up.</summary>
    /// <param name="player">The player that should be rotated.</param>
    /// <returns></returns>
    [<Extension>]
    static member TurnUp(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.turnUp player

    /// <summary>Rotates the player so that it looks to the right.</summary>
    /// <param name="player">The player that should be rotated.</param>
    /// <returns></returns>
    [<Extension>]
    static member TurnRight(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.turnRight player

    /// <summary>Rotates the player so that it looks down.</summary>
    /// <param name="player">The player that should be rotated.</param>
    /// <returns></returns>
    [<Extension>]
    static member TurnDown(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.turnDown player

    /// <summary>Rotates the player so that it looks to the left.</summary>
    /// <param name="player">The player that should be rotated.</param>
    /// <returns></returns>
    [<Extension>]
    static member TurnLeft(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.turnLeft player

    /// <summary>Rotates the player clockwise by a specific angle.</summary>
    /// <param name="player">The player that should be rotated.</param>
    /// <param name="angle">The relative angle.</param>
    /// <returns></returns>
    [<Extension>]
    static member RotateClockwise(player: GetIt.Player, angle: GetIt.Degrees) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        if obj.ReferenceEquals(angle, null) then raise (ArgumentNullException "angle")
        Raw.rotateClockwise player angle

    /// <summary>Rotates the player counter-clockwise by a specific angle.</summary>
    /// <param name="player">The player that should be rotated.</param>
    /// <param name="angle">The relative angle.</param>
    /// <returns></returns>
    [<Extension>]
    static member RotateCounterClockwise(player: GetIt.Player, angle: GetIt.Degrees) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        if obj.ReferenceEquals(angle, null) then raise (ArgumentNullException "angle")
        Raw.rotateCounterClockwise player angle

    /// <summary>Checks whether a given player touches an edge of the scene.</summary>
    /// <param name="player">The player that might touch an edge of the scene.</param>
    /// <returns>True, if the player touches an edge, otherwise false.</returns>
    [<Extension>]
    static member TouchesEdge(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.touchesEdge player

    /// <summary>Checks whether a given player touches another player.</summary>
    /// <param name="player">The first player that might be touched.</param>
    /// <param name="other">The second player that might be touched.</param>
    /// <returns>True, if the two players touch each other, otherwise false.</returns>
    [<Extension>]
    static member TouchesPlayer(player: GetIt.Player, other: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        if obj.ReferenceEquals(other, null) then raise (ArgumentNullException "other")
        Raw.touchesPlayer player other

    /// <summary>Bounces the player off the wall if it currently touches it.</summary>
    /// <param name="player">The player that should bounce off the wall.</param>
    /// <returns></returns>
    [<Extension>]
    static member BounceOffWall(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.bounceOffWall player

    /// <summary>Pauses execution of the player for a given time.</summary>
    /// <param name="player">The player that pauses execution.</param>
    /// <param name="duration">The length of the pause.</param>
    /// <returns></returns>
    [<Extension>]
    static member Sleep(player: GetIt.Player, duration: System.TimeSpan) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.sleep player duration

    /// <summary>Pauses execution of the player for a given time.</summary>
    /// <param name="player">The player that pauses execution.</param>
    /// <param name="durationInMilliseconds">The length of the pause in milliseconds.</param>
    /// <returns></returns>
    [<Extension>]
    static member Sleep(player: GetIt.Player, durationInMilliseconds: System.Double) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.sleepMilliseconds player durationInMilliseconds

    /// <summary>Shows a speech bubble next to the player. You can remove the speech bubble with <see cref="ShutUp"/>.</summary>
    /// <param name="player">The player that the speech bubble belongs to.</param>
    /// <param name="text">The content of the speech bubble.</param>
    /// <returns></returns>
    [<Extension>]
    static member Say(player: GetIt.Player, text: System.String) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        if obj.ReferenceEquals(text, null) then raise (ArgumentNullException "text")
        Raw.say player text

    /// <summary>Removes the speech bubble of the player.</summary>
    /// <param name="player">The player that the speech bubble belongs to.</param>
    /// <returns></returns>
    [<Extension>]
    static member ShutUp(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.shutUp player

    /// <summary>Shows a speech bubble next to the player for a specific time.</summary>
    /// <param name="player">The player that the speech bubble belongs to.</param>
    /// <param name="text">The content of the speech bubble.</param>
    /// <param name="duration">The time span how long the speech bubble should be visible.</param>
    /// <returns></returns>
    [<Extension>]
    static member Say(player: GetIt.Player, text: System.String, duration: System.TimeSpan) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        if obj.ReferenceEquals(text, null) then raise (ArgumentNullException "text")
        Raw.sayWithDuration player text duration

    /// <summary>Shows a speech bubble next to the player for a specific time.</summary>
    /// <param name="player">The player that the speech bubble belongs to.</param>
    /// <param name="text">The content of the speech bubble.</param>
    /// <param name="durationInSeconds">The number of seconds how long the speech bubble should be visible.</param>
    /// <returns></returns>
    [<Extension>]
    static member Say(player: GetIt.Player, text: System.String, durationInSeconds: System.Double) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        if obj.ReferenceEquals(text, null) then raise (ArgumentNullException "text")
        Raw.sayWithDurationInSeconds player text durationInSeconds

    /// <summary>Shows a speech bubble with a text box next to the player and waits for the user to fill in the text box.</summary>
    /// <param name="player">The player that the speech bubble belongs to.</param>
    /// <param name="question">The content of the speech bubble.</param>
    /// <returns>The text the user typed in.</returns>
    [<Extension>]
    static member Ask(player: GetIt.Player, question: System.String) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        if obj.ReferenceEquals(question, null) then raise (ArgumentNullException "question")
        Raw.ask player question

    /// <summary>Shows a speech bubble with two buttons "confirm" and "decline" next to the player and waits for the user to press one of the buttons.</summary>
    /// <param name="player">The player that the speech bubble belongs to.</param>
    /// <param name="question">The content of the speech bubble.</param>
    /// <returns>True, if the user pressed the "confirm" button, false otherwise.</returns>
    [<Extension>]
    static member AskBool(player: GetIt.Player, question: System.String) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        if obj.ReferenceEquals(question, null) then raise (ArgumentNullException "question")
        Raw.askBool player question

    /// <summary>Turns on the pen of the player.</summary>
    /// <param name="player">The player that should get its pen turned on.</param>
    /// <returns></returns>
    [<Extension>]
    static member TurnOnPen(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.turnOnPen player

    /// <summary>Turns off the pen of the player.</summary>
    /// <param name="player">The player that should get its pen turned off.</param>
    /// <returns></returns>
    [<Extension>]
    static member TurnOffPen(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.turnOffPen player

    /// <summary>Turns on the pen of the player if it is turned off. Turns off the pen of the player if it is turned on.</summary>
    /// <param name="player">The player that should get its pen toggled.</param>
    /// <returns></returns>
    [<Extension>]
    static member TogglePenState(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.togglePenState player

    /// <summary>Sets the pen color of the player.</summary>
    /// <param name="player">The player that should get its pen color set.</param>
    /// <param name="color">The new color of the pen.</param>
    /// <returns></returns>
    [<Extension>]
    static member SetPenColor(player: GetIt.Player, color: GetIt.RGBAColor) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        if obj.ReferenceEquals(color, null) then raise (ArgumentNullException "color")
        Raw.setPenColor player color

    /// <summary>Shifts the HUE value of the pen color.</summary>
    /// <param name="player">The player that should get its pen color shifted.</param>
    /// <param name="angle">The angle that the HUE value should be shifted by.</param>
    /// <returns></returns>
    [<Extension>]
    static member ShiftPenColor(player: GetIt.Player, angle: GetIt.Degrees) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        if obj.ReferenceEquals(angle, null) then raise (ArgumentNullException "angle")
        Raw.shiftPenColor player angle

    /// <summary>Sets the weight of the pen.</summary>
    /// <param name="player">The player that gets its pen weight set.</param>
    /// <param name="weight">The new weight of the pen.</param>
    /// <returns></returns>
    [<Extension>]
    static member SetPenWeight(player: GetIt.Player, weight: System.Double) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.setPenWeight player weight

    /// <summary>Changes the weight of the pen.</summary>
    /// <param name="player">The player that gets its pen weight changed.</param>
    /// <param name="weight">The change of the pen weight.</param>
    /// <returns></returns>
    [<Extension>]
    static member ChangePenWeight(player: GetIt.Player, weight: System.Double) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.changePenWeight player weight

    /// <summary>Sets the size of the player by multiplying the original size with a factor.</summary>
    /// <param name="player">The player that gets its size changed.</param>
    /// <param name="sizeFactor">The factor the original size should be multiplied by.</param>
    /// <returns></returns>
    [<Extension>]
    static member SetSizeFactor(player: GetIt.Player, sizeFactor: System.Double) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.setSizeFactor player sizeFactor

    /// <summary>Changes the size factor of the player that the original size is multiplied by.</summary>
    /// <param name="player">The player that gets its size changed.</param>
    /// <param name="change">The change of the size factor.</param>
    /// <returns></returns>
    [<Extension>]
    static member ChangeSizeFactor(player: GetIt.Player, change: System.Double) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.changeSizeFactor player change

    /// <summary>Changes the costume of the player.</summary>
    /// <param name="player">The player that gets its costume changed.</param>
    /// <returns></returns>
    [<Extension>]
    static member NextCostume(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.nextCostume player

    /// <summary>Sends the player to the back of the scene so that other players will overlap the current player.</summary>
    /// <param name="player">The player that is sent to the back.</param>
    /// <returns></returns>
    [<Extension>]
    static member SendToBack(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.sendToBack player

    /// <summary>Sends the player to the front of the scene so that the current player will overlap other players.</summary>
    /// <param name="player">The player that is sent to the front.</param>
    /// <returns></returns>
    [<Extension>]
    static member BringToFront(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.bringToFront player

    /// <summary>Calculates the direction from the player to the mouse pointer.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The direction from the player to the mouse pointer.</returns>
    [<Extension>]
    static member GetDirectionToMouse(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.getDirectionToMouse player

    /// <summary>Calculates the distance from the player to the mouse pointer.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The distance from the player to the mouse pointer.</returns>
    [<Extension>]
    static member GetDistanceToMouse(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.getDistanceToMouse player

    /// <summary>Calculates the direction from the player to another player.</summary>
    /// <param name="player1">The player.</param>
    /// <param name="player2">The other player.</param>
    /// <returns>The direction from the player to another player.</returns>
    [<Extension>]
    static member GetDirectionTo(player1: GetIt.Player, player2: GetIt.Player) =
        if obj.ReferenceEquals(player1, null) then raise (ArgumentNullException "player1")
        if obj.ReferenceEquals(player2, null) then raise (ArgumentNullException "player2")
        Raw.getDirectionTo player1 player2

    /// <summary>Calculates the distance from the player to another player.</summary>
    /// <param name="player1">The player.</param>
    /// <param name="player2">The other player.</param>
    /// <returns>The distance from the player to another player.</returns>
    [<Extension>]
    static member GetDistanceTo(player1: GetIt.Player, player2: GetIt.Player) =
        if obj.ReferenceEquals(player1, null) then raise (ArgumentNullException "player1")
        if obj.ReferenceEquals(player2, null) then raise (ArgumentNullException "player2")
        Raw.getDistanceTo player1 player2

    /// <summary>Shows the player that has been hidden using <see cref="Hide"/>.</summary>
    /// <param name="player">The player to show.</param>
    /// <returns></returns>
    [<Extension>]
    static member Show(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.show player

    /// <summary>Hides the player. Use <see cref="Show"/> to unhide the player.</summary>
    /// <param name="player">The player to hide.</param>
    /// <returns></returns>
    [<Extension>]
    static member Hide(player: GetIt.Player) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        Raw.hide player

    /// <summary>Registers an event handler that is called once when a specific keyboard key is pressed.</summary>
    /// <param name="player">The player that gets passed to the event handler.</param>
    /// <param name="key">The keyboard key that should be listened to.</param>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    [<Extension>]
    static member OnKeyDown(player: GetIt.Player, key: GetIt.KeyboardKey, action: System.Action<GetIt.Player>) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        if obj.ReferenceEquals(key, null) then raise (ArgumentNullException "key")
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")
        Raw.onKeyDown player key action

    /// <summary>Registers an event handler that is called once when any keyboard key is pressed.</summary>
    /// <param name="player">The player that gets passed to the event handler.</param>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    [<Extension>]
    static member OnAnyKeyDown(player: GetIt.Player, action: System.Action<GetIt.Player, GetIt.KeyboardKey>) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")
        Raw.onAnyKeyDown player action

    /// <summary>Registers an event handler that is called contiuously when a specific keyboard key is pressed.</summary>
    /// <param name="player">The player that gets passed to the event handler.</param>
    /// <param name="key">The keyboard key that should be listened to.</param>
    /// <param name="interval">How often the event handler should be called.</param>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    [<Extension>]
    static member OnKeyDown(player: GetIt.Player, key: GetIt.KeyboardKey, interval: System.TimeSpan, action: System.Action<GetIt.Player, System.Int32>) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        if obj.ReferenceEquals(key, null) then raise (ArgumentNullException "key")
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")
        Raw.whileKeyDown player key interval action

    /// <summary>Registers an event handler that is called contiuously when any keyboard key is pressed.</summary>
    /// <param name="player">The player that gets passed to the event handler.</param>
    /// <param name="interval">How often the event handler should be called.</param>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    [<Extension>]
    static member OnAnyKeyDown(player: GetIt.Player, interval: System.TimeSpan, action: System.Action<GetIt.Player, GetIt.KeyboardKey, System.Int32>) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")
        Raw.whileAnyKeyDown player interval action

    /// <summary>Registers an event handler that is called when the mouse enters the player area.</summary>
    /// <param name="player">The player.</param>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    [<Extension>]
    static member OnMouseEnter(player: GetIt.Player, action: System.Action<GetIt.Player>) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")
        Raw.onMouseEnter player action

    /// <summary>Registers an event handler that is called when the mouse is clicked on the player.</summary>
    /// <param name="player">The player.</param>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    [<Extension>]
    static member OnClick(player: GetIt.Player, action: System.Action<GetIt.Player, GetIt.MouseClick>) =
        if obj.ReferenceEquals(player, null) then raise (ArgumentNullException "player")
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")
        Raw.onClick player action
