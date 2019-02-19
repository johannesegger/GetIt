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

type Command =
    { Name: string
      CompiledName: string
      Summary: string
      Parameters: Parameter list
      ReturnType: Type
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
          ReturnType = typeof<unit>
          Body = [ "InterProcessCommunication.sendCommands [ UpdatePosition (player.PlayerId, position) ]" ] }

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
          ReturnType = typeof<unit>
          Body = [ "moveTo player { X = x; Y = y; }" ] }

        { Name = "moveToCenter"
          CompiledName = "MoveToCenter"
          Summary = "Moves the player to the center of the scene."
          Parameters =
            [ { Name = "player"
                Type = typeof<GetIt.Player>
                Description = "The player that should be moved." } ]
          ReturnType = typeof<unit>
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
          ReturnType = typeof<unit>
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
          ReturnType = typeof<unit>
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
          ReturnType = typeof<unit>
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
          ReturnType = typeof<unit>
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
          ReturnType = typeof<unit>
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
          ReturnType = typeof<unit>
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
          ReturnType = typeof<unit>
          Body =
            [ "let x = rand.Next(int Model.current.SceneBounds.Left, int Model.current.SceneBounds.Right + 1)"
              "let y = rand.Next(int Model.current.SceneBounds.Bottom, int Model.current.SceneBounds.Top + 1)"
              "moveToXY player (float x) (float y)" ] }

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
                yield sprintf "[<CompiledName(\"%s\")>]" command.CompiledName
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
                [ "let private getTurtleIdOrFail () ="
                  "    match Model.defaultTurtleId with"
                  "    | Some v -> v"
                  "    | None -> failwith \"Default player hasn't been added to the scene. Consider calling `Game.ShowSceneAndAddTurtle()` at the beginning.\""
                  ""
                  "let private getTurtleOrFail () ="
                  "    let turtleId = getTurtleIdOrFail()"
                  "    new Player(turtleId)" ]
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
            [ "namespace GetIt"; ""; "open System" ]
            rawFuncs
            defaultTurtleFuncs
            extensionMethods
        ]
        |> List.intersperse [ "" ]
        |> List.collect id
    File.WriteAllLines("GetIt.Controller\\Player.fs", lines)
    0
