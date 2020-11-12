namespace GetIt

open System

type internal SceneSize =
    | SpecificSize of Size
    | Maximized

type internal PngImage = PngImage of byte[]

module internal PngImage =
    let toBase64String (PngImage data) =
        Convert.ToBase64String data
        |> sprintf "data:image/png;base64, %s"
