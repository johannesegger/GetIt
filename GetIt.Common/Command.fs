namespace GetIt

type RequestMsg =
    | ShowScene
    | AddPlayer of Player
    | MoveTo of PlayerId * Position

type ResponseMsg =
    | InitializedScene of sceneBounds: Rectangle
    | AddedPlayer of PlayerId * Player
    | UpdatedPosition of PlayerId * Position 
