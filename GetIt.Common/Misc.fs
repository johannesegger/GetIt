namespace GetIt

open System

/// For internal use only.
type SceneSize =
    | SpecificSize of Size
    | Maximized

/// For internal use only.
type PngImage = PngImage of byte[]

/// For internal use only.
module PngImage =
    let toBase64String (PngImage data) =
        Convert.ToBase64String data
        |> sprintf "data:image/png;base64, %s"