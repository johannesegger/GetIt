namespace GameLib.Server

open System
open System.Runtime.CompilerServices
open GameLib.Data
open GameLib.Data.Server
open GameLib.Execution
open Spectrum

type Scene() = class end

type ScriptState =
    { Player: Player
      Scene: Scene }

module ScriptStateModule =
    let applyPlayerInstruction player = function
        | SetPositionInstruction position -> { player with Position = position }
        | SetDirectionInstruction direction -> { player with Direction = direction }
        | SayInstruction _ -> player
        | SetPenOnInstruction isOn -> { player with Pen = { player.Pen with IsOn = isOn } }
        | SetPenColorInstruction color -> { player with Pen = { player.Pen with Color = color } }
        | SetPenWeigthInstruction weight -> { player with Pen = { player.Pen with Weight = weight } }

    let applySceneInstruction scene = function
        | ClearLinesInstruction -> scene

    let applyInstruction state = function
        | PlayerInstruction instruction -> { state with Player = applyPlayerInstruction state.Player instruction }
        | SceneInstruction instruction -> { state with Scene = applySceneInstruction state.Scene instruction }

module private Helpers =
    let wrapAngle value = (value % 360. + 360.) % 360.

    let shiftColor offset color =
        let result =
            Color.RGB(color.Red, color.Green, color.Blue)
                .ToHSL()
                .ShiftHue(offset * 360.)
                .ToRGB()
        { Red = result.R; Green = result.G; Blue = result.B }

[<Extension>]
type PlayerExtensions() =
    [<Extension>]
    static member GoTo(player: Player, x, y) = SetPositionInstruction { X = x; Y = y } |> PlayerInstruction
    [<Extension>]
    static member Move(player, x, y) = SetPositionInstruction (player.Position + { X = x; Y = y }) |> PlayerInstruction
    [<Extension>]
    static member MoveRight(player, x) = player.Move(x, 0.)
    [<Extension>]
    static member MoveLeft(player, x) = player.Move(-x, 0.)
    [<Extension>]
    static member MoveUp(player, y) = player.Move(0., y)
    [<Extension>]
    static member MoveDown(player, y) = player.Move(0., -y)
    [<Extension>]
    static member Go(player, steps) =
        let directionRadians = player.Direction / 180. * Math.PI
        player.Move(Math.Cos(directionRadians) * steps, Math.Sin(directionRadians) * steps)
    [<Extension>]
    static member RotateClockwise(player, angleInDegrees) =
        player.Direction - angleInDegrees
        |> Helpers.wrapAngle
        |> SetDirectionInstruction
        |> PlayerInstruction
    [<Extension>]
    static member RotateCounterClockwise(player, angleInDegrees) =
        player.Direction + angleInDegrees
        |> Helpers.wrapAngle
        |> SetDirectionInstruction
        |> PlayerInstruction
    [<Extension>]
    static member TurnUp(player: Player) = SetDirectionInstruction 90. |> PlayerInstruction
    [<Extension>]
    static member TurnRight(player: Player) = SetDirectionInstruction 0. |> PlayerInstruction
    [<Extension>]
    static member TurnDown(player: Player) = SetDirectionInstruction 270. |> PlayerInstruction
    [<Extension>] 
    static member TurnLeft(player: Player) = SetDirectionInstruction 180. |> PlayerInstruction
    [<Extension>] 
    static member Say(player: Player, text) =
        (text, None)
        |> SayInstruction
        |> PlayerInstruction
    [<Extension>] 
    static member Say(player: Player, text, durationInSeconds) =
        (text, TimeSpan.FromSeconds durationInSeconds |> Some)
        |> SayInstruction
        |> PlayerInstruction
    [<Extension>] 
    static member TurnOnPen(player: Player) =
        SetPenOnInstruction true
        |> PlayerInstruction
    [<Extension>] 
    static member TurnOffPen(player: Player) =
        SetPenOnInstruction false
        |> PlayerInstruction
    [<Extension>] 
    static member TogglePenOnOff(player) =
        SetPenOnInstruction (not player.Pen.IsOn)
        |> PlayerInstruction
    [<Extension>] 
    static member SetPenColor(player: Player, color) =
        SetPenColorInstruction color
        |> PlayerInstruction
    [<Extension>] 
    static member ShiftPenColor(player, offset) =
        Helpers.shiftColor offset player.Pen.Color
        |> SetPenColorInstruction
        |> PlayerInstruction
    [<Extension>] 
    static member SetPenWeight(player: Player, weight) =
        SetPenWeigthInstruction weight
        |> PlayerInstruction
    [<Extension>] 
    static member ChangePenWeight(player, weight) =
        SetPenWeigthInstruction (player.Pen.Weight + weight)
        |> PlayerInstruction

[<Extension>]
type SceneExtensions() =
    [<Extension>]
    static member ClearLines(scene: Scene) = ClearLinesInstruction