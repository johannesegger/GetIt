namespace GetIt

open System

type internal ControllerMsg =
    | AddPlayer of PlayerId * PlayerData
    | RemovePlayer of PlayerId
    | SetWindowTitle of string option
    | SetBackground of SvgImage
    | ClearScene
    | SetPosition of PlayerId * Position
    | ChangePosition of PlayerId * Position
    | SetDirection of PlayerId * Degrees
    | ChangeDirection of PlayerId * Degrees
    | SetSpeechBubble of PlayerId * SpeechBubble option
    | SetPenState of PlayerId * bool
    | TogglePenState of PlayerId
    | SetPenColor of PlayerId * RGBAColor
    | ShiftPenColor of PlayerId * Degrees
    | SetPenWeight of PlayerId * float
    | ChangePenWeight of PlayerId * float
    | SetSizeFactor of PlayerId * float
    | ChangeSizeFactor of PlayerId * float
    | SetNextCostume of PlayerId
    | SendToBack of PlayerId
    | BringToFront of PlayerId
    | SetVisibility of PlayerId * bool
    | ToggleVisibility of PlayerId
    | CaptureScene
    | StartBatch
    | ApplyBatch

type internal UIMsg =
    | SetSceneBounds of Rectangle
    | AnswerStringQuestion of PlayerId * string
    | AnswerBoolQuestion of PlayerId * bool
    | CapturedScene of PngImage

type internal ControllerToUIMsg =
    | ControllerMsg of Guid * ControllerMsg

type internal UIToControllerMsg =
    | ControllerMsgConfirmation of Guid
    | UIMsg of UIMsg
