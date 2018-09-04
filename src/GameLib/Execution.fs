module GameLib.Execution

open GameLib.Data

type Span = {
    Start: int
    End: int
}

type CompilationError = {
    Message: string
    Span: Span
}

type SkipReason =
    | CompilationErrors of CompilationError list

type PlayerInstruction =
    | SetPositionInstruction of Position
    | SetDirectionInstruction of float
    | SayInstruction of (string * System.TimeSpan option)
    | SetPenOnInstruction of bool
    | SetPenColorInstruction of RGBColor
    | SetPenWeigthInstruction of float

type SceneInstruction =
    | ClearLinesInstruction

type Instruction =
    | PlayerInstruction of PlayerInstruction
    | SceneInstruction of SceneInstruction

type RunScriptResult =
    | Skipped of SkipReason
    | RanToCompletion of Instruction list
    | TimedOut