module GameLib.Execution

open GameLib.Data.Global

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

type RunScriptResult =
    | Skipped of SkipReason
    | RanToCompletion of Player list
    | TimedOut