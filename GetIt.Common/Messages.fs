namespace GetIt

type ControllerMsg =
    | AddPlayer of PlayerId * PlayerData
    | RemovePlayer of PlayerId

type UIMsg =
    | SetSceneBounds of Rectangle
