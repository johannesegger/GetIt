namespace GetIt

open System

/// For internal use only.
type WindowSize =
    | SpecificSize of Size
    | Maximized

/// For internal use only.
type PngImage = PngImage of byte[]

/// For internal use only.
module PngImage =
    let toBase64String (PngImage data) =
        Convert.ToBase64String data
        |> sprintf "data:image/png;base64, %s"

module MessageChannel =
    let endpoint = "/msgs"
