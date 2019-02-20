open System
open System.IO

module List =
    let intersperse sep ls =
        List.foldBack (fun x -> function
            | [] -> [x]
            | xs -> x::sep::xs) ls []


type Parameter =
    { Name: string
      Type: Type
      Description: string }

type Result =
    { Type: Type
      Description: string }

type Command =
    { Name: string
      CompiledName: string
      Summary: string
      Parameters: Parameter list
      Result: Result
      Body: string list }

let commands =
    [
        { Name = "moveTo"
          CompiledName = "MoveTo"
          Summary = "Moves the player to a position."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should be moved." }
              { Name = "position"
                Type = typeof<GetIt.Position>
                Description = "The absolute destination position." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body = [ "InterProcessCommunication.sendCommands [ SetPosition (player.PlayerId, position) ]" ] }

        { Name = "moveToXY"
          CompiledName = "MoveTo"
          Summary = "Moves the player to a position."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should be moved." }
              { Name = "x"
                Type = typeof<float>
                Description = "The absolute x coordinate of the destination position." }
              { Name = "y"
                Type = typeof<float>
                Description = "The absolute y coordinate of the destination position." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body = [ "moveTo player { X = x; Y = y; }" ] }

        { Name = "moveToCenter"
          CompiledName = "MoveToCenter"
          Summary = "Moves the player to the center of the scene."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should be moved." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body = [ "moveTo player Position.zero" ] }

        { Name = "moveBy"
          CompiledName = "MoveBy"
          Summary = "Moves the player to the center of the scene."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should be moved." }
              { Name = "deltaX"
                Type = typeof<float>
                Description = "The change of the x coordinate." }
              { Name = "deltaY"
                Type = typeof<float>
                Description = "The change of the y coordinate." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body = [ "moveToXY player (player.Position.X + deltaX) (player.Position.Y + deltaY)" ] }

        { Name = "moveRight"
          CompiledName = "MoveRight"
          Summary = "Moves the player horizontally."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should be moved." }
              { Name = "steps"
                Type = typeof<float>
                Description = "The number of steps." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body = [ "moveBy player steps 0." ] }

        { Name = "moveLeft"
          CompiledName = "MoveLeft"
          Summary = "Moves the player horizontally."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should be moved." }
              { Name = "steps"
                Type = typeof<float>
                Description = "The number of steps." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body = [ "moveBy player -steps 0." ] }

        { Name = "moveUp"
          CompiledName = "MoveUp"
          Summary = "Moves the player vertically."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should be moved." }
              { Name = "steps"
                Type = typeof<float>
                Description = "The number of steps." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body = [ "moveBy player 0. steps" ] }

        { Name = "moveDown"
          CompiledName = "MoveDown"
          Summary = "Moves the player vertically."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should be moved." }
              { Name = "steps"
                Type = typeof<float>
                Description = "The number of steps." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body = [ "moveBy player 0. -steps" ] }

        { Name = "moveInDirection"
          CompiledName = "MoveInDirection"
          Summary = "Moves the player forward."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should be moved." }
              { Name = "steps"
                Type = typeof<float>
                Description = "The number of steps." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body =
            [ "let directionRadians = Degrees.toRadians player.Direction"
              "moveBy"
              "    player"
              "    (Math.Cos(directionRadians) * steps)"
              "    (Math.Sin(directionRadians) * steps)" ] }

        { Name = "moveToRandomPosition"
          CompiledName = "MoveToRandomPosition"
          Summary = "Moves the player to a random position on the scene."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should be moved." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body =
            [ "let x = rand.Next(int Model.current.SceneBounds.Left, int Model.current.SceneBounds.Right + 1)"
              "let y = rand.Next(int Model.current.SceneBounds.Bottom, int Model.current.SceneBounds.Top + 1)"
              "moveToXY player (float x) (float y)" ] }

        { Name = "setDirection"
          CompiledName = "SetDirection"
          Summary = "Sets the rotation of the player to a specific angle."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should be rotated." }
              { Name = "angle"
                Type = typeof<GetIt.Degrees>
                Description = "The absolute angle." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body = [ "InterProcessCommunication.sendCommands [ SetDirection (player.PlayerId, angle) ]" ] }

        { Name = "rotateClockwise"
          CompiledName = "RotateClockwise"
          Summary = "Rotates the player clockwise by a specific angle."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should be rotated." }
              { Name = "angle"
                Type = typeof<GetIt.Degrees>
                Description = "The relative angle." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body = [ "setDirection player (player.Direction - angle)" ] }

        { Name = "rotateCounterClockwise"
          CompiledName = "RotateCounterClockwise"
          Summary = "Rotates the player counter-clockwise by a specific angle."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should be rotated." }
              { Name = "angle"
                Type = typeof<GetIt.Degrees>
                Description = "The relative angle." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body = [ "setDirection player (player.Direction + angle)" ] }

        { Name = "turnUp"
          CompiledName = "TurnUp"
          Summary = "Rotates the player so that it looks up."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should be rotated." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body = [ "setDirection player (Degrees.op_Implicit 90.)" ] }

        { Name = "turnRight"
          CompiledName = "TurnRight"
          Summary = "Rotates the player so that it looks to the right."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should be rotated." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body = [ "setDirection player (Degrees.op_Implicit 0.)" ] }

        { Name = "turnDown"
          CompiledName = "TurnDown"
          Summary = "Rotates the player so that it looks down."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should be rotated." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body = [ "setDirection player (Degrees.op_Implicit 270.)" ] }

        { Name = "turnLeft"
          CompiledName = "TurnLeft"
          Summary = "Rotates the player so that it looks to the left."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should be rotated." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body = [ "setDirection player (Degrees.op_Implicit 180.)" ] }

        { Name = "touchesEdge"
          CompiledName = "TouchesEdge"
          Summary = "Checks whether a given player touches an edge of the scene."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that might touch an edge of the scene." } ]
          Result = { Type = typeof<bool>; Description = "True, if the player touches an edge, otherwise false." }
          Body = [ "touchesLeftOrRightEdge player || touchesTopOrBottomEdge player" ] }

        { Name = "touchesPlayer"
          CompiledName = "TouchesPlayer"
          Summary = "Checks whether a given player touches another player."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The first player that might be touched." }
              { Name = "other"
                Type = typeof<GetIt.Player>
                Description = "The second player that might be touched." } ]
          Result = { Type = typeof<bool>; Description = "True, if the two players touch each other, otherwise false." }
          Body =
            [ "let maxLeftX = Math.Max(player.Bounds.Left, other.Bounds.Left)"
              "let minRightX = Math.Min(player.Bounds.Right, other.Bounds.Right)"
              "let maxBottomY = Math.Max(player.Bounds.Bottom, other.Bounds.Bottom)"
              "let minTopY = Math.Min(player.Bounds.Top, other.Bounds.Top)"
              "maxLeftX < minRightX && maxBottomY < minTopY" ] }

        { Name = "bounceOffWall"
          CompiledName = "BounceOffWall"
          Summary = "Bounces the player off the wall if it currently touches it."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should bounce off the wall." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body =
            [ "if touchesTopOrBottomEdge player then setDirection player (Degrees.zero - player.Direction)"
              "elif touchesLeftOrRightEdge player then setDirection player (Degrees.op_Implicit 180. - player.Direction)" ] }

        { Name = "sleep"
          CompiledName = "Sleep"
          Summary = "Pauses execution of the player for a given time."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that pauses execution." }
              { Name = "durationInMilliseconds"
                Type = typeof<float>
                Description = "The length of the pause in milliseconds." } ]
          Result = { Type = typeof<unit>; Description = "" }
          Body = [ "Thread.Sleep(TimeSpan.FromMilliseconds(durationInMilliseconds))" ] }

    ]

[<EntryPoint>]
let main _argv =
    let rawFuncs =
        commands
        |> List.map (fun command ->
            [
                let parameterList =
                    command.Parameters
                    |> List.map (fun p -> sprintf "(%s: %s)" p.Name p.Type.FullName)
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
        |> List.append
            [ "module private Raw ="
              "    let private rand = Random()"
              ""
              "    let private touchesTopOrBottomEdge (player: GetIt.Player) ="
              "        player.Bounds.Top > Model.current.SceneBounds.Top || player.Bounds.Bottom < Model.current.SceneBounds.Bottom"
              ""
              "    let private touchesLeftOrRightEdge (player: GetIt.Player) ="
              "        player.Bounds.Right > Model.current.SceneBounds.Right || player.Bounds.Left < Model.current.SceneBounds.Left"
              "" ]

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
                    |> List.map (fun p -> sprintf "(%s: %s)" p.Name p.Type.FullName)
                    |> function
                    | [] -> [ "()" ]
                    | x -> x
                    |> String.concat " "
                let parameterNames =
                    parameters
                    |> List.map (fun p -> p.Name)
                    |> List.append [ "(getTurtleOrFail ())" ]
                    |> String.concat " "
                yield sprintf "[<CompiledName(\"%s\")>]" command.CompiledName
                yield sprintf "let %s %s =" command.Name parameterListWithTypes
                yield sprintf "    Raw.%s %s" command.Name parameterNames
            ]
            |> List.map (sprintf "    %s")
        )
        |> List.intersperse [ "" ]
        |> List.collect id
        |> List.append
            [ yield "module Turtle ="
              yield!
                [ "let private getTurtleOrFail () ="
                  "    match Game.defaultTurtle with"
                  "    | Some player -> player"
                  "    | None -> failwith \"Default player hasn't been added to the scene. Consider calling `Game.ShowSceneAndAddTurtle()` at the beginning.\"" ]
                |> List.map (sprintf "    %s")
              yield "" ]

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
                    |> List.map (fun p -> sprintf "%s: %s" p.Name p.Type.FullName)
                    |> String.concat ", "
                let parameterNames =
                    command.Parameters
                    |> List.map (fun p -> p.Name)
                    |> String.concat " "
                yield "[<Extension>]"
                yield sprintf "static member %s(%s) =" command.CompiledName parameterListWithTypes
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

    let lines =
        [
            [ "namespace GetIt"; ""; "open System"; "open System.Threading" ]
            rawFuncs
            defaultTurtleFuncs
            extensionMethods
        ]
        |> List.intersperse [ "" ]
        |> List.collect id
    File.WriteAllLines("GetIt.Controller\\Player.fs", lines)
    0
