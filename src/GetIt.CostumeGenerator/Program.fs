open System
open System.IO
open FSharp.Data

[<Literal>]
let XamlFilePath = __SOURCE_DIRECTORY__ + "\\..\\..\\assets\\Turtle.xaml"

type XamlProvider = XmlProvider<XamlFilePath>

[<EntryPoint>]
let main argv =
    let doc = XamlProvider.GetSample()
    doc.Canvas.Canvas.Paths
    |> Seq.map (fun p ->
        let (red, green, blue, alpha) =
            p.Fill
            |> Seq.skip 1
            |> Seq.chunkBySize 2
            |> Seq.map (Seq.toArray >> System.String >> fun s -> Convert.ToInt32(s, 16))
            |> Seq.toArray
            |> fun c -> c.[1], c.[2], c.[3], c.[0]
        sprintf
            "new GeometryPath(fill: new RGBA(0x%02x, 0x%02x, 0x%02x, 0x%02x), data: \"%s\")"
            red green blue alpha p.PathData.PathGeometry.Figures
    )
    |> Seq.map (sprintf ".Add(%s)")
    |> fun geometryPaths ->
        [
            yield "new Costume("
            yield sprintf "    new Size(%d, %d)," doc.Canvas.Width doc.Canvas.Height
            yield "    ImmutableList<GeometryPath>.Empty"
            yield! geometryPaths |> Seq.map (sprintf "        %s")
            yield ")"
        ]
    |> Seq.map (sprintf "                    %s")
    |> String.concat Environment.NewLine
    |> printfn "%s"
    0
