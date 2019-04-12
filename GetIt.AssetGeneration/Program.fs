open System
open System.IO
open System.Net.Http
open System.Text.RegularExpressions
open System.Xml
open FSharp.Data
open GetIt
open Polly

module String =
    let firstToUpper (t: string) =
        if String.IsNullOrEmpty t then t
        else sprintf "%c%s" (Char.ToUpper t.[0]) (t.Substring 1)

type ScratchAssetProvider = JsonProvider<"sprites.json">

type Costume = {
    Width: float
    Height: float
    Data: string
}

type Asset = {
    Name: string
    Categories: string list
    Costumes: Costume list
}

let getCostumeFromSvgData content =
    let (width, height) = Svg.getSizeFromSvgDocument content
    {
        Width = width
        Height = height
        Data = content
    }

let getCostumeFromSvgFile path =
    File.ReadAllText path
    |> getCostumeFromSvgData

let getPlayerName (assetName: string) =
    Regex.Replace(assetName, @"\s", "")
    |> fun s -> Regex.Replace(s, @"-(?<c>.)", fun m -> m.Groups.["c"].Value.ToUpper())

let httpPolicy =
    let delays =
        Seq.initInfinite (float >> TimeSpan.FromSeconds)
        |> Seq.map (fun t -> if t.TotalSeconds < 10. then t else TimeSpan.FromSeconds(10.))
    Policy
        .HandleInner<HttpRequestException>()
        .WaitAndRetryAsync(delays)

let removeNewLines (text: string) =
    text.Replace("\r", "").Replace("\n", "")

[<EntryPoint>]
let main argv =
    use httpClient = new HttpClient()
    ScratchAssetProvider.GetSamples()
    |> Seq.filter (fun asset -> asset.Md5.EndsWith(".svg", StringComparison.InvariantCultureIgnoreCase))
    |> Seq.map (fun asset -> async {
        printfn "Loading %d assets for %s" asset.Json.Costumes.Length asset.Name
        let! costumes =
            asset.Json.Costumes
            |> Seq.map (fun costume -> async {
                let url = sprintf "https://cdn.assets.scratch.mit.edu/internalapi/asset/%s/get/" costume.BaseLayerMd5
                try
                    let! content =
                        httpPolicy.ExecuteAsync(fun () ->
                            async {
                                let! response = httpClient.GetAsync(url) |> Async.AwaitTask
                                return! response.Content.ReadAsStringAsync() |> Async.AwaitTask
                            }
                            |> Async.StartAsTask
                        )
                        |> Async.AwaitTask

                    return getCostumeFromSvgData content
                with e ->
                    failwithf "Error while loading data for %s at %s: %O" asset.Name url e
                    return Unchecked.defaultof<_>
            })
            |> Async.Parallel
        printfn "Loaded %d assets for %s" asset.Json.Costumes.Length asset.Name
        return
            {
                Name = asset.Name
                Categories = asset.Tags |> Seq.map String.firstToUpper |> Seq.toList
                Costumes = Array.toList costumes
            }
    })
    |> Async.Parallel
    |> Async.RunSynchronously
    |> Seq.append [
        {
            Name = "Turtle"
            Categories = []
            Costumes = [
                getCostumeFromSvgFile @"assets\Turtle1.svg"
                getCostumeFromSvgFile @"assets\Turtle2.svg"
                getCostumeFromSvgFile @"assets\Turtle3.svg"
            ]
        }
        {
            Name = "Ant"
            Categories = []
            Costumes = [
                getCostumeFromSvgFile @"assets\Ant1.svg"
            ]
        }
        {
            Name = "Bug"
            Categories = []
            Costumes = [
                getCostumeFromSvgFile @"assets\Bug1.svg"
            ]
        }
        {
            Name = "Spider"
            Categories = []
            Costumes = [
                getCostumeFromSvgFile @"assets\Spider1.svg"
            ]
        }
    ]
    |> Seq.sortBy (fun p -> p.Name)
    |> Seq.map (fun asset ->
        let costumes =
            asset.Costumes
            |> List.map (fun c ->
                sprintf "{ Size = { Width = %f; Height = %f }; SvgData = \"\"\"%s\"\"\" }"
                    c.Width c.Height (removeNewLines c.Data)
            )
        sprintf "static member %s = PlayerData.Create [ %s ]" (getPlayerName asset.Name) (String.concat "; " costumes)
    )
    |> fun lines -> File.WriteAllLines("sprites.fs", lines)
    0
