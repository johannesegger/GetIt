namespace GetIt

open System
open System.IO
open System.Text.RegularExpressions
open System.Xml

exception GetItException of string

[<AutoOpen>]
module Utils =
    let curry fn a b = fn (a, b)

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

module RandomNumberGenerator =
    [<CompiledName("Default")>]
    let ``default`` = Random()
