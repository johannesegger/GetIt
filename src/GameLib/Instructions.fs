namespace GameLib.Instructions

open GameLib.Instruction

module Player =
    let GoTo (x, y) = SetPosition { X = x; Y = y } |> PlayerInstruction
    let Move (x, y) = ChangePosition { X = x; Y = y } |> PlayerInstruction
    let MoveRight x = ChangePosition { X = x; Y = 0. } |> PlayerInstruction
    let MoveLeft x = ChangePosition { X = -x; Y = 0. } |> PlayerInstruction
    let MoveUp y = ChangePosition { X = 0.; Y = y } |> PlayerInstruction
    let MoveDown y = ChangePosition { X = 0.; Y = -y } |> PlayerInstruction
    let Go steps = Go steps |> PlayerInstruction
    let RotateClockwise angleInDegrees = ChangeDirection -angleInDegrees |> PlayerInstruction
    let RotateCounterClockwise angleInDegrees = ChangeDirection angleInDegrees |> PlayerInstruction
    let TurnUp () = SetDirection 90. |> PlayerInstruction
    let TurnRight () = SetDirection 0. |> PlayerInstruction
    let TurnDown () = SetDirection 270. |> PlayerInstruction
    let TurnLeft () = SetDirection 180. |> PlayerInstruction

    module Pen =
        let TurnOn () = TurnOn |> PenInstruction
        let TurnOff () = TurnOff |> PenInstruction
        let ToggleOnOff () = ToggleOnOff |> PenInstruction
        let SetColor color = SetColor color |> PenInstruction
        let ShiftColor offset = ShiftColor offset |> PenInstruction
        let SetWeight weight = SetWeight weight |> PenInstruction
        let ChangeWeight weight = ChangeWeight weight |> PenInstruction