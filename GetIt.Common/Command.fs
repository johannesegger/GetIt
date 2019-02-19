namespace GetIt

type RequestMsg =
    | ShowScene
    | AddPlayer of PlayerData
    | UpdatePosition of PlayerId * Position
    | RemovePlayer of PlayerId

type ResponseMsg =
    | InitializedScene of sceneBounds: Rectangle
    | AddedPlayer of PlayerId * PlayerData
    | UpdatedPosition of PlayerId * Position 
    | RemovedPlayer of PlayerId
