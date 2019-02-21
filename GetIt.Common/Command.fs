namespace GetIt

type RequestMsg =
    | ShowScene
    | AddPlayer of PlayerData
    | RemovePlayer of PlayerId
    | SetPosition of PlayerId * Position
    | SetDirection of PlayerId * Degrees
    | SetSpeechBubble of PlayerId * SpeechBubble option
    | SetPen of PlayerId * Pen
    | SetSizeFactor of PlayerId * float
    | SetNextCostume of PlayerId

type ResponseMsg =
    | InitializedScene of sceneBounds: Rectangle
    | PlayerAdded of PlayerId * PlayerData
    | PlayerRemoved of PlayerId
    | PositionSet of PlayerId * Position
    | DirectionSet of PlayerId * Degrees
    | SpeechBubbleSet of PlayerId * SpeechBubble option
    | PenSet of PlayerId * Pen
    | SizeFactorSet of PlayerId * float
    | NextCostumeSet of PlayerId
