namespace GetIt

type RequestMsg =
    | ShowScene
    | AddPlayer of PlayerData
    | RemovePlayer of PlayerId
    | SetPosition of PlayerId * Position
    | SetDirection of PlayerId * Degrees

type ResponseMsg =
    | InitializedScene of sceneBounds: Rectangle
    | PlayerAdded of PlayerId * PlayerData
    | PlayerRemoved of PlayerId
    | PositionSet of PlayerId * Position
    | DirectionSet of PlayerId * Degrees
