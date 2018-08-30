namespace GameLib.Server

open System
open System.Runtime.CompilerServices
open GameLib.Data
open GameLib.Data.Global
open Spectrum

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
    static member GoTo(player, x, y) = { player with Position = { X = x; Y = y } }

    [<Extension>]
    static member Move(player, x, y) = { player with Position = player.Position + { X = x; Y = y } }
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
        player.Move(Math.Cos(directionRadians) * steps, -Math.Sin(directionRadians) * steps)
    [<Extension>]
    static member RotateClockwise(player, angleInDegrees) = { player with Direction = player.Direction - angleInDegrees |> Helpers.wrapAngle }
    [<Extension>]
    static member RotateCounterClockwise(player, angleInDegrees) = { player with Direction = player.Direction + angleInDegrees |> Helpers.wrapAngle }
    [<Extension>]
    static member TurnUp(player) = { player with Direction = 90. }
    [<Extension>]
    static member TurnRight(player) = { player with Direction = 0. }
    [<Extension>]
    static member TurnDown(player) = { player with Direction = 270. }
    [<Extension>] 
    static member TurnLeft(player) = { player with Direction = 180. }
    [<Extension>] 
    static member TurnOnPen(player) = { player with Pen = { player.Pen with IsOn = true} }
    [<Extension>] 
    static member TurnOffPen(player) = { player with Pen = { player.Pen with IsOn = false} }
    [<Extension>] 
    static member TogglePenOnOff(player) = { player with Pen = { player.Pen with IsOn = not player.Pen.IsOn } }
    [<Extension>] 
    static member SetPenColor(player, color) = { player with Pen = { player.Pen with Color = color } }
    [<Extension>] 
    static member ShiftPenColor(player, offset) = { player with Pen = { player.Pen with Color = Helpers.shiftColor offset player.Pen.Color } }
    [<Extension>] 
    static member SetPenWeight(player, weight) = { player with Pen = { player.Pen with Weight = weight } }
    [<Extension>] 
    static member ChangePenWeight(player, weight) = { player with Pen = { player.Pen with Weight = player.Pen.Weight + weight } }