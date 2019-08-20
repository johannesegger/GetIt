namespace GetIt

open System
open System.IO
open System.Text.RegularExpressions
open System.Xml

/// Thrown when an exception is encountered.
type GetItException =
    inherit Exception
    new (message: string) = { inherit Exception(message) }
    new (message: string, innerException: exn) = { inherit Exception (message, innerException) }

/// For internal use only.
[<AutoOpen>]
module Utils =
    let curry fn a b = fn (a, b)
    let uncurry fn (a, b) = fn a b
    let flip fn a b = fn b a

/// For internal use only.
module Svg =
    let parseViewBox (text: string) =
        let parts =
            text.Split ' '
            |> Array.map float
            |> Array.toList
        match parts with
        | x :: y :: width :: [ height ] -> Some (x, y, width, height)
        | _ -> None

    let parseLength (text: string) =
        if isNull text then None
        else
            let m = Regex.Match(text, @"^.*(?=(px)?$)")
            if m.Success then
                match System.Double.TryParse m.Value with
                | (true, v) -> Some v
                | _ -> None
            else
                None

    let parseSize widthText heightText =
        match parseLength widthText, parseLength heightText with
        | Some width, Some height -> Some (width, height)
        | _ -> None

#if !FABLE_COMPILER
    let getSizeFromSvgDocument content =
        use textReader = new StringReader(content)
        let readerSettings = XmlReaderSettings()
        readerSettings.DtdProcessing <- DtdProcessing.Ignore
        use reader = XmlReader.Create(textReader, readerSettings)
        while reader.NodeType <> XmlNodeType.Element do
            reader.Read() |> ignore

        let widthText = reader.GetAttribute("width")
        let heightText = reader.GetAttribute("height")
        let viewBoxText = reader.GetAttribute("viewBox")

        Option.ofObj viewBoxText
        |> Option.bind parseViewBox
        |> Option.map (fun (x, y, width, height) -> width, height)
        |> Option.orElse (parseSize widthText heightText)
        |> function
        | Some (width, height) -> width, height
        | None -> failwithf "Can't get size from svg data (Width = <%s>, Height = <%s>, ViewBox = <%s>)" widthText heightText viewBoxText
#endif

/// For internal use only.
module Result =
    let ofOption error = function
        | Some o -> Result.Ok o
        | None -> Result.Error error
    let toOption = function
        | Result.Ok o -> Some o 
        | Result.Error _ -> None 

/// For internal use only.
module Async =
    let map fn a = async {
        let! p = a
        return fn p
    }

