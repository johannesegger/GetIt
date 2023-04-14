open System
open System.IO
open System.Text.RegularExpressions

module List =
    let intersperse sep ls =
        (ls, [])
        ||> List.foldBack (fun x -> function
            | [] -> [x]
            | xs -> x::sep::xs)

type Parameter =
    {
        Name: string
        Type: Type
        Description: string
        Attributes: string option
    }

type Result =
    {
        Type: Type
        Description: string
    }

type Command =
    {
        Name: string
        CompiledName: string
        Summary: string
        Parameters: Parameter list
        Result: Result
        Body: string list
    }

let commands =
    [
        {
            Name = "moveTo"
            CompiledName = "MoveTo"
            Summary = "Moves the player to a position."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should be moved."
                        Attributes = None
                    }
                    {
                        Name = "position"
                        Type = typeof<GetIt.Position>
                        Description = "The absolute destination position."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.setPosition player.PlayerId position" ]
        }

        {
            Name = "moveToXY"
            CompiledName = "MoveTo"
            Summary = "Moves the player to a position."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should be moved."
                        Attributes = None
                    }
                    {
                        Name = "x"
                        Type = typeof<float>
                        Description = "The absolute x coordinate of the destination position."
                        Attributes = None
                    }
                    {
                        Name = "y"
                        Type = typeof<float>
                        Description = "The absolute y coordinate of the destination position."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "moveTo player { X = x; Y = y }" ]
        }

        {
            Name = "moveToCenter"
            CompiledName = "MoveToCenter"
            Summary = "Moves the player to the center of the scene."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should be moved."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "moveTo player Position.zero" ]
        }

        {
            Name = "moveBy"
            CompiledName = "MoveBy"
            Summary = "Moves the player relatively."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should be moved."
                        Attributes = None
                    }
                    {
                        Name = "deltaX"
                        Type = typeof<float>
                        Description = "The change of the x coordinate."
                        Attributes = None
                    }
                    {
                        Name = "deltaY"
                        Type = typeof<float>
                        Description = "The change of the y coordinate."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.changePosition player.PlayerId { X = deltaX; Y = deltaY }" ]
        }

        {
            Name = "moveRight"
            CompiledName = "MoveRight"
            Summary = "Moves the player horizontally."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should be moved."
                        Attributes = None
                    }
                    {
                        Name = "steps"
                        Type = typeof<float>
                        Description = "The number of steps."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "moveBy player steps 0." ]
        }

        {
            Name = "moveLeft"
            CompiledName = "MoveLeft"
            Summary = "Moves the player horizontally."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should be moved."
                        Attributes = None
                    }
                    {
                        Name = "steps"
                        Type = typeof<float>
                        Description = "The number of steps."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "moveBy player -steps 0." ]
        }

        {
            Name = "moveUp"
            CompiledName = "MoveUp"
            Summary = "Moves the player vertically."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should be moved."
                        Attributes = None
                    }
                    {
                        Name = "steps"
                        Type = typeof<float>
                        Description = "The number of steps."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "moveBy player 0. steps" ]
        }

        {
            Name = "moveDown"
            CompiledName = "MoveDown"
            Summary = "Moves the player vertically."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should be moved."
                        Attributes = None
                    }
                    {
                        Name = "steps"
                        Type = typeof<float>
                        Description = "The number of steps."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "moveBy player 0. -steps" ]
        }

        {
            Name = "moveInDirection"
            CompiledName = "MoveInDirection"
            Summary = "Moves the player forward."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should be moved."
                        Attributes = None
                    }
                    {
                        Name = "steps"
                        Type = typeof<float>
                        Description = "The number of steps."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body =
                [
                    "let directionRadians = Degrees.toRadians player.Direction"
                    "moveBy"
                    "    player"
                    "    (Math.Cos(directionRadians) * steps)"
                    "    (Math.Sin(directionRadians) * steps)"
                ]
            }

        {
            Name = "moveToRandomPosition"
            CompiledName = "MoveToRandomPosition"
            Summary = "Moves the player to a random position on the scene."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should be moved."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body =
                [
                    "let x = RandomNumberGenerator.GetInt32(int (Game.getCurrentModel().SceneBounds.Left), int (Game.getCurrentModel().SceneBounds.Right) + 1)"
                    "let y = RandomNumberGenerator.GetInt32(int (Game.getCurrentModel().SceneBounds.Bottom), int (Game.getCurrentModel().SceneBounds.Top) + 1)"
                    "moveToXY player (float x) (float y)"
                ]
            }

        {
            Name = "setDirection"
            CompiledName = "SetDirection"
            Summary = "Sets the rotation of the player to a specific angle."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should be rotated."
                        Attributes = None
                    }
                    {
                        Name = "angle"
                        Type = typeof<GetIt.Degrees>
                        Description = "The absolute angle."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.setDirection player.PlayerId angle" ]
        }

        {
            Name = "turnUp"
            CompiledName = "TurnUp"
            Summary = "Rotates the player so that it looks up."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should be rotated."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "setDirection player (Degrees.op_Implicit 90.)" ]
        }

        {
            Name = "turnRight"
            CompiledName = "TurnRight"
            Summary = "Rotates the player so that it looks to the right."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should be rotated."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "setDirection player (Degrees.op_Implicit 0.)" ]
        }

        {
            Name = "turnDown"
            CompiledName = "TurnDown"
            Summary = "Rotates the player so that it looks down."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should be rotated."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "setDirection player (Degrees.op_Implicit 270.)" ]
        }

        {
            Name = "turnLeft"
            CompiledName = "TurnLeft"
            Summary = "Rotates the player so that it looks to the left."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should be rotated."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "setDirection player (Degrees.op_Implicit 180.)" ]
        }

        {
            Name = "rotateClockwise"
            CompiledName = "RotateClockwise"
            Summary = "Rotates the player clockwise by a specific angle."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should be rotated."
                        Attributes = None
                    }
                    {
                        Name = "angle"
                        Type = typeof<GetIt.Degrees>
                        Description = "The relative angle."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.changeDirection player.PlayerId -angle" ]
        }

        {
            Name = "rotateCounterClockwise"
            CompiledName = "RotateCounterClockwise"
            Summary = "Rotates the player counter-clockwise by a specific angle."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should be rotated."
                        Attributes = None
                    }
                    {
                        Name = "angle"
                        Type = typeof<GetIt.Degrees>
                        Description = "The relative angle."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "rotateClockwise player -angle" ]
        }

        {
            Name = "touchesEdge"
            CompiledName = "TouchesEdge"
            Summary = "Checks whether a given player touches an edge of the scene."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that might touch an edge of the scene."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<bool>; Description = "True, if the player touches an edge, otherwise false." }
            Body = [
                "Game.getCurrentModel().SceneBounds"
                "|> Rectangle.containsRectangle player.Bounds"
            ]
        }

        {
            Name = "touchesPlayer"
            CompiledName = "TouchesPlayer"
            Summary = "Checks whether a given player touches another player."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The first player that might be touched."
                        Attributes = None
                    }
                    {
                        Name = "other"
                        Type = typeof<GetIt.Player>
                        Description = "The second player that might be touched."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<bool>; Description = "True, if the two players touch each other, otherwise false." }
            Body =
                [
                    "let maxLeftX = Math.Max(player.Bounds.Left, other.Bounds.Left)"
                    "let minRightX = Math.Min(player.Bounds.Right, other.Bounds.Right)"
                    "let maxBottomY = Math.Max(player.Bounds.Bottom, other.Bounds.Bottom)"
                    "let minTopY = Math.Min(player.Bounds.Top, other.Bounds.Top)"
                    "maxLeftX < minRightX && maxBottomY < minTopY"
                ]
            }

        {
            Name = "bounceOffWall"
            CompiledName = "BounceOffWall"
            Summary = "Bounces the player off the wall if it currently touches it."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should bounce off the wall."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body =
                [
                    "Movement.bounceOffWall (Game.getCurrentModel().SceneBounds) (player.Bounds, player.Direction)"
                    "|> Option.iter (setDirection player)"
                ]
            }

        {
            Name = "sleep"
            CompiledName = "Sleep"
            Summary = "Pauses execution of the player for a given time."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that pauses execution."
                        Attributes = None
                    }
                    {
                        Name = "duration"
                        Type = typeof<TimeSpan>
                        Description = "The length of the pause."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Thread.Sleep duration" ]
        }

        {
            Name = "sleepMilliseconds"
            CompiledName = "Sleep"
            Summary = "Pauses execution of the player for a given time."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that pauses execution."
                        Attributes = None
                    }
                    {
                        Name = "durationInMilliseconds"
                        Type = typeof<float>
                        Description = "The length of the pause in milliseconds."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "sleep player (TimeSpan.FromMilliseconds durationInMilliseconds)" ]
        }

        {
            Name = "say"
            CompiledName = "Say"
            Summary = "Shows a speech bubble next to the player. You can remove the speech bubble with <see cref=\"ShutUp\"/>."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that the speech bubble belongs to."
                        Attributes = None
                    }
                    {
                        Name = "text"
                        Type = typeof<string[]>
                        Description = "The content of the speech bubble."
                        Attributes = Some "[<ParamArray>]"
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.say player.PlayerId (String.concat Environment.NewLine text)" ]
        }

        {
            Name = "shutUp"
            CompiledName = "ShutUp"
            Summary = "Removes the speech bubble of the player."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that the speech bubble belongs to."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.shutUp player.PlayerId" ]
        }

        {
            Name = "sayWithDuration"
            CompiledName = "Say"
            Summary = "Shows a speech bubble next to the player for a specific time."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that the speech bubble belongs to."
                        Attributes = None
                    }
                    {
                        Name = "duration"
                        Type = typeof<TimeSpan>
                        Description = "The time span how long the speech bubble should be visible."
                        Attributes = None
                    }
                    {
                        Name = "text"
                        Type = typeof<string[]>
                        Description = "The content of the speech bubble."
                        Attributes = Some "[<ParamArray>]"
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body =
                [
                    "say player text"
                    "sleep player duration"
                    "shutUp player"
                ]
            }

        {
            Name = "sayWithDurationInSeconds"
            CompiledName = "Say"
            Summary = "Shows a speech bubble next to the player for a specific time."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that the speech bubble belongs to."
                        Attributes = None
                    }
                    {
                        Name = "durationInSeconds"
                        Type = typeof<float>
                        Description = "The number of seconds how long the speech bubble should be visible."
                        Attributes = None
                    }
                    {
                        Name = "text"
                        Type = typeof<string[]>
                        Description = "The content of the speech bubble."
                        Attributes = Some "[<ParamArray>]"
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "sayWithDuration player (TimeSpan.FromSeconds durationInSeconds) text" ]
        }

        {
            Name = "ask"
            CompiledName = "Ask"
            Summary = "Shows a speech bubble with a text box next to the player and waits for the user to fill in the text box."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that the speech bubble belongs to."
                        Attributes = None
                    }
                    {
                        Name = "question"
                        Type = typeof<string>
                        Description = "The content of the speech bubble."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<string>; Description = "The text the user typed in." }
            Body = [ "Game.askString player.PlayerId question" ]
        }

        {
            Name = "askBool"
            CompiledName = "AskBool"
            Summary = "Shows a speech bubble with two buttons \"confirm\" and \"decline\" next to the player and waits for the user to press one of the buttons."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that the speech bubble belongs to."
                        Attributes = None
                    }
                    {
                        Name = "question"
                        Type = typeof<string>
                        Description = "The content of the speech bubble."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<bool>; Description = "True, if the user pressed the \"confirm\" button, false otherwise." }
            Body = [ "Game.askBool player.PlayerId question" ]
        }

        {
            Name = "turnOnPen"
            CompiledName = "TurnOnPen"
            Summary = "Turns on the pen of the player."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should get its pen turned on."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.setPenState player.PlayerId true" ]
        }

        {
            Name = "turnOffPen"
            CompiledName = "TurnOffPen"
            Summary = "Turns off the pen of the player."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should get its pen turned off."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.setPenState player.PlayerId false" ]
        }

        {
            Name = "togglePenState"
            CompiledName = "TogglePenState"
            Summary = "Turns on the pen of the player if it is turned off. Turns off the pen of the player if it is turned on."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should get its pen toggled."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.togglePenState player.PlayerId" ]
        }

        {
            Name = "setPenColor"
            CompiledName = "SetPenColor"
            Summary = "Sets the pen color of the player."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should get its pen color set."
                        Attributes = None
                    }
                    {
                        Name = "color"
                        Type = typeof<GetIt.RGBAColor>
                        Description = "The new color of the pen."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.setPenColor player.PlayerId color" ]
        }

        {
            Name = "shiftPenColor"
            CompiledName = "ShiftPenColor"
            Summary = "Shifts the HUE value of the pen color."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should get its pen color shifted."
                        Attributes = None
                    }
                    {
                        Name = "angle"
                        Type = typeof<GetIt.Degrees>
                        Description = "The angle that the HUE value should be shifted by."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.shiftPenColor player.PlayerId angle" ]
        }

        {
            Name = "setPenWeight"
            CompiledName = "SetPenWeight"
            Summary = "Sets the weight of the pen."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that gets its pen weight set."
                        Attributes = None
                    }
                    {
                        Name = "weight"
                        Type = typeof<float>
                        Description = "The new weight of the pen."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.setPenWeight player.PlayerId weight" ]
        }

        {
            Name = "changePenWeight"
            CompiledName = "ChangePenWeight"
            Summary = "Changes the weight of the pen."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that gets its pen weight changed."
                        Attributes = None
                    }
                    {
                        Name = "weight"
                        Type = typeof<float>
                        Description = "The change of the pen weight."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.changePenWeight player.PlayerId weight" ]
        }

        {
            Name = "setSizeFactor"
            CompiledName = "SetSizeFactor"
            Summary = "Sets the size of the player by multiplying the original size with a factor."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that gets its size changed."
                        Attributes = None
                    }
                    {
                        Name = "sizeFactor"
                        Type = typeof<float>
                        Description = "The factor the original size should be multiplied by."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.setSizeFactor player.PlayerId sizeFactor" ]
        }

        {
            Name = "changeSizeFactor"
            CompiledName = "ChangeSizeFactor"
            Summary = "Changes the size factor of the player that the original size is multiplied by."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that gets its size changed."
                        Attributes = None
                    }
                    {
                        Name = "change"
                        Type = typeof<float>
                        Description = "The change of the size factor."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.changeSizeFactor player.PlayerId change" ]
        }

        {
            Name = "nextCostume"
            CompiledName = "NextCostume"
            Summary = "Changes the costume of the player."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that gets its costume changed."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.setNextCostume player.PlayerId" ]
        }

        {
            Name = "sendToBack"
            CompiledName = "SendToBack"
            Summary = "Sends the player to the back of the scene so that other players will overlap the current player."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that is sent to the back."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.sendToBack player.PlayerId" ]
        }

        {
            Name = "bringToFront"
            CompiledName = "BringToFront"
            Summary = "Sends the player to the front of the scene so that the current player will overlap other players."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that is sent to the front."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.bringToFront player.PlayerId" ]
        }

        {
            Name = "getDirectionToMouse"
            CompiledName = "GetDirectionToMouse"
            Summary = "Calculates the direction from the player to the mouse pointer."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<GetIt.Degrees>; Description = "The direction from the player to the mouse pointer." }
            Body = [ "player.Position |> Position.angleTo (Game.getCurrentModel().MouseState.Position)" ]
        }

        {
            Name = "getDistanceToMouse"
            CompiledName = "GetDistanceToMouse"
            Summary = "Calculates the distance from the player to the mouse pointer."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<float>; Description = "The distance from the player to the mouse pointer." }
            Body = [ "player.Position |> Position.distanceTo (Game.getCurrentModel().MouseState.Position)" ]
        }

        {
            Name = "getDirectionTo"
            CompiledName = "GetDirectionTo"
            Summary = "Calculates the direction from the player to another player."
            Parameters =
                [
                    {
                        Name = "player1"
                        Type = typeof<GetIt.Player>
                        Description = "The player."
                        Attributes = None
                    }

                    {
                        Name = "player2"
                        Type = typeof<GetIt.Player>
                        Description = "The other player."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<GetIt.Degrees>; Description = "The direction from the player to another player." }
            Body = [ "player1.Position |> Position.angleTo player2.Position" ]
        }

        {
            Name = "getDistanceTo"
            CompiledName = "GetDistanceTo"
            Summary = "Calculates the distance from the player to another player."
            Parameters =
                [
                    {
                        Name = "player1"
                        Type = typeof<GetIt.Player>
                        Description = "The player."
                        Attributes = None
                    }

                    {
                        Name = "player2"
                        Type = typeof<GetIt.Player>
                        Description = "The other player."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<float>; Description = "The distance from the player to another player." }
            Body = [ "player1.Position |> Position.distanceTo player2.Position" ]
        }

        {
            Name = "show"
            CompiledName = "Show"
            Summary = "Shows the player that has been hidden using <see cref=\"Hide\"/>."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player to show."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.setVisibility player.PlayerId true" ]
        }

        {
            Name = "hide"
            CompiledName = "Hide"
            Summary = "Hides the player. Use <see cref=\"Show\"/> to unhide the player."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player to hide."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.setVisibility player.PlayerId false" ]
        }

        {
            Name = "toggleVisibility"
            CompiledName = "ToggleVisibility"
            Summary = "Shows the player if it is hidden. Hides the player if it is visible."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that should get its visibility toggled."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<unit>; Description = "" }
            Body = [ "Game.toggleVisibility player.PlayerId" ]
        }

        {
            Name = "onKeyDown"
            CompiledName = "OnKeyDown"
            Summary = "Registers an event handler that is called once when a specific keyboard key is pressed."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that gets passed to the event handler."
                        Attributes = None
                    }
                    {
                        Name = "key"
                        Type = typeof<GetIt.KeyboardKey>
                        Description = "The keyboard key that should be listened to."
                        Attributes = None
                    }
                    {
                        Name = "action"
                        Type = typeof<Action<GetIt.Player>>
                        Description = "The event handler that should be called."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<IDisposable>; Description = "The disposable subscription." }
            Body = [ "Game.onKeyDown key (fun () -> action.Invoke player)" ]
        }

        {
            Name = "onAnyKeyDown"
            CompiledName = "OnAnyKeyDown"
            Summary = "Registers an event handler that is called once when any keyboard key is pressed."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that gets passed to the event handler."
                        Attributes = None
                    }
                    {
                        Name = "action"
                        Type = typeof<Action<GetIt.Player, GetIt.KeyboardKey>>
                        Description = "The event handler that should be called."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<IDisposable>; Description = "The disposable subscription." }
            Body = [ "Game.onAnyKeyDown (fun key -> action.Invoke(player, key))" ]
        }

        {
            Name = "whileKeyDown"
            CompiledName = "OnKeyDown"
            Summary = "Registers an event handler that is called contiuously when a specific keyboard key is pressed."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that gets passed to the event handler."
                        Attributes = None
                    }
                    {
                        Name = "key"
                        Type = typeof<GetIt.KeyboardKey>
                        Description = "The keyboard key that should be listened to."
                        Attributes = None
                    }
                    {
                        Name = "interval"
                        Type = typeof<TimeSpan>
                        Description = "How often the event handler should be called."
                        Attributes = None
                    }
                    {
                        Name = "action"
                        Type = typeof<Action<GetIt.Player, int>>
                        Description = "The event handler that should be called."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<IDisposable>; Description = "The disposable subscription." }
            Body = [ "Game.whileKeyDown key interval (fun i -> action.Invoke(player, i))" ]
        }

        {
            Name = "whileAnyKeyDown"
            CompiledName = "OnAnyKeyDown"
            Summary = "Registers an event handler that is called contiuously when any keyboard key is pressed."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player that gets passed to the event handler."
                        Attributes = None
                    }
                    {
                        Name = "interval"
                        Type = typeof<TimeSpan>
                        Description = "How often the event handler should be called."
                        Attributes = None
                    }
                    {
                        Name = "action"
                        Type = typeof<Action<GetIt.Player, GetIt.KeyboardKey, int>>
                        Description = "The event handler that should be called."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<IDisposable>; Description = "The disposable subscription." }
            Body = [ "Game.whileAnyKeyDown interval (fun key i -> action.Invoke(player, key, i))" ]
        }

        {
            Name = "onMouseEnter"
            CompiledName = "OnMouseEnter"
            Summary = "Registers an event handler that is called when the mouse enters the player area."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player."
                        Attributes = None
                    }
                    {
                        Name = "action"
                        Type = typeof<Action<GetIt.Player>>
                        Description = "The event handler that should be called."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<IDisposable>; Description = "The disposable subscription." }
            Body = [ "Game.onEnterPlayer player.PlayerId (fun () -> action.Invoke(player))" ]
        }

        {
            Name = "onClick"
            CompiledName = "OnClick"
            Summary = "Registers an event handler that is called when the mouse is clicked on the player."
            Parameters =
                [
                    {
                        Name = "player"
                        Type = typeof<GetIt.Player>
                        Description = "The player."
                        Attributes = None
                    }
                    {
                        Name = "action"
                        Type = typeof<Action<GetIt.Player, GetIt.MouseClick>>
                        Description = "The event handler that should be called."
                        Attributes = None
                    }
                ]
            Result = { Type = typeof<IDisposable>; Description = "The disposable subscription." }
            Body = [ "Game.onClickPlayer player.PlayerId (fun mouseClick -> action.Invoke(player, mouseClick))" ]
        }
    ]

let rec getFullName (t: Type) =
    if t.IsGenericTypeParameter then t.Name
    elif t.IsGenericType then
      let rawName = Regex.Replace(t.Name, @"`\d+$", "")
      let genericArgumentNames = Seq.map getFullName t.GenericTypeArguments
      sprintf "%s.%s<%s>" t.Namespace rawName (String.concat ", " genericArgumentNames)
    else
      t.FullName

let nullGuards (parameters: Parameter list) =
    parameters
    |> List.filter (fun p -> p.Type.IsClass)
    |> List.map (fun p -> sprintf "if obj.ReferenceEquals(%s, null) then raise (ArgumentNullException \"%s\")" p.Name p.Name)

[<EntryPoint>]
let main _argv =
    let rawFuncs =
        commands
        |> List.map (fun command ->
            [
                let parameterList =
                    command.Parameters
                    |> List.map (fun p -> sprintf "(%s: %s)" p.Name (getFullName p.Type))
                    |> String.concat " "
                yield sprintf "let %s %s =" command.Name parameterList
                yield!
                    command.Body
                    |> List.map (sprintf "    %s")
            ]
            |> List.map (sprintf "    %s")
        )
        |> List.intersperse [ "" ]
        |> List.collect id
        |> List.map (fun line -> line.TrimEnd())
        |> List.append
            [ "module private Raw =" ]

    let defaultTurtleFuncs =
        commands
        |> List.map (fun command ->
            [
                let parameters =
                    command.Parameters
                    |> List.skip 1 // skip player
                yield sprintf "/// <summary>%s</summary>" command.Summary
                yield!
                    parameters
                    |> List.map (fun p ->
                        sprintf "/// <param name=\"%s\">%s</param>" p.Name p.Description
                    )
                yield sprintf "/// <returns>%s</returns>" command.Result.Description
                let parameterListWithTypes =
                    parameters
                    |> List.map (fun p ->
                        let attributesText = p.Attributes |> Option.map (sprintf "%s ") |> Option.defaultValue ""
                        sprintf "%s%s: %s" attributesText p.Name (getFullName p.Type)
                    )
                    |> String.concat ", "
                    |> sprintf "(%s)"
                let parameterNames =
                    parameters
                    |> List.map (fun p -> p.Name)
                    |> List.append [ "Turtle.Player" ]
                    |> String.concat " "
                yield sprintf "static member %s %s =" command.CompiledName parameterListWithTypes
                yield!
                    nullGuards parameters
                    |> List.map (sprintf "    %s")
                yield sprintf "    Raw.%s %s" command.Name parameterNames
            ]
            |> List.map (sprintf "    %s")
        )
        |> List.intersperse [ "" ]
        |> List.collect id
        |> List.append
            [
                yield "[<AbstractClass; Sealed>]"
                yield "type Turtle() ="
                yield!
                    [
                        "static member private Player"
                        "    with get () ="
                        "        match Game.defaultTurtle with"
                        "        | Some player -> player"
                        "        | None -> raise (GetItException \"Default player hasn't been added to the scene. Consider calling `Game.ShowSceneAndAddTurtle()` at the beginning.\")"
                        ""
                        "/// The actual size of the player."
                        "static member Size with get () = Turtle.Player.Size"
                        ""
                        "/// The factor that is used to change the size of the player."
                        "static member SizeFactor with get () = Turtle.Player.SizeFactor"
                        ""
                        "/// The position of the player's center point."
                        "static member Position with get () = Turtle.Player.Position"
                        ""
                        "/// The rectangular bounds of the player."
                        "/// Note that this doesn't take into account the current rotation of the player."
                        "static member Bounds with get () = Turtle.Player.Bounds"
                        ""
                        "/// The rotation of the player."
                        "static member Direction with get () = Turtle.Player.Direction"
                        ""
                        "/// The pen that belongs to the player."
                        "static member Pen with get () = Turtle.Player.Pen"
                        ""
                        "/// The current costume of the player."
                        "static member Costume with get () = Turtle.Player.Costume"
                        ""
                        "/// True, if the player should be drawn, otherwise false."
                        "static member IsVisible with get () = Turtle.Player.IsVisible"
                    ]
                    |> List.map (sprintf "    %s")
                yield ""
              ]

    let extensionMethods =
        commands
        |> List.map (fun command ->
            [
                yield sprintf "/// <summary>%s</summary>" command.Summary
                yield!
                    command.Parameters
                    |> List.map (fun p ->
                        sprintf "/// <param name=\"%s\">%s</param>" p.Name p.Description
                    )
                yield sprintf "/// <returns>%s</returns>" command.Result.Description
                let parameterListWithTypes =
                    command.Parameters
                    |> List.map (fun p ->
                        let attributesText = p.Attributes |> Option.map (sprintf "%s ") |> Option.defaultValue ""
                        sprintf "%s%s: %s" attributesText p.Name (getFullName p.Type)
                    )
                    |> String.concat ", "
                let parameterNames =
                    command.Parameters
                    |> List.map (fun p -> p.Name)
                    |> String.concat " "
                yield "[<Extension>]"
                yield sprintf "static member %s(%s) =" command.CompiledName parameterListWithTypes
                yield!
                    nullGuards command.Parameters
                    |> List.map (sprintf "    %s")
                yield sprintf "    Raw.%s %s" command.Name parameterNames
            ]
            |> List.map (sprintf "    %s")
        )
        |> List.intersperse [ "" ]
        |> List.collect id
        |> List.append
            [ "open System.Runtime.CompilerServices"
              ""
              "[<Extension>]"
              "type PlayerExtensions() =" ]

    let prelude =
        [
            "namespace GetIt"
            ""
            "open System"
            "open System.Security.Cryptography"
            "open System.Threading"
        ]
    let lines =
        [
            prelude
            rawFuncs
            defaultTurtleFuncs
            extensionMethods
        ]
        |> List.intersperse [ "" ]
        |> List.collect id
    File.WriteAllLines("GetIt.Controller\\Player.generated.fs", lines)
    0
