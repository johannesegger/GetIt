namespace GameLib.Instruction

open GameLib.Data

type PlayerInstruction =
    | SetPosition of Position
    | ChangePosition of Position
    | Go of float
    | SetDirection of float
    | ChangeDirection of float

type PenInstruction =
    | TurnOn
    | TurnOff
    | ToggleOnOff
    | SetColor of RGBColor
    | ShiftColor of float
    | SetWeight of float
    | ChangeWeight of float

type GameInstruction =
    | PlayerInstruction of PlayerInstruction
    | PenInstruction of PenInstruction