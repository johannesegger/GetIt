namespace GetIt

open System
open System.Globalization
open System.IO
open System.Text.RegularExpressions
open System.Xml

/// Thrown when an exception is encountered.
type GetItException =
    inherit Exception
    new (message: string) = { inherit Exception(message) }
    new (message: string, innerException: exn) = { inherit Exception (message, innerException) }

[<AutoOpen>]
module internal Utils =
    let curry fn a b = fn (a, b)
    let uncurry fn (a, b) = fn a b
    let flip fn a b = fn b a

module internal Result =
    let ofOption error = function
        | Some o -> Ok o
        | None -> Error error
    let toOption = function
        | Ok o -> Some o
        | Error _ -> None

module internal Async =
    let map fn a = async {
        let! p = a
        return fn p
    }

#if !FABLE_COMPILER
module internal Double =
    let tryParseCultureInvariant (text: string) =
        match Double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture) with
        | (true, v) -> Some v
        | (false, _) -> None

module internal Svg =
    let parseViewBox (text: string) =
        let parts =
            text.Split ' '
            |> Array.choose Double.tryParseCultureInvariant
            |> Array.toList
        match parts with
        | x :: y :: width :: [ height ] -> Some (x, y, width, height)
        | _ -> None

    let parseLength (text: string) =
        if isNull text then None
        else
            let m = Regex.Match(text, @"^.*(?=(px)?$)")
            if m.Success then
                Double.tryParseCultureInvariant m.Value
            else
                None

    let parseSize widthText heightText =
        match parseLength widthText, parseLength heightText with
        | Some width, Some height -> Some (width, height)
        | _ -> None

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
