module Sprites

open System
open System.IO
open System.Net.Http
open FSharp.Data

type ScratchSpriteProvider = JsonProvider<"sprites.json">

type Asset = {
    Name: string
    Categories: string list
    Costumes: SvgImage list
}

let generate (httpClient: HttpClient) =
    ScratchSpriteProvider.GetSamples()
    |> Seq.filter (fun asset -> asset.Md5.EndsWith(".svg", StringComparison.InvariantCultureIgnoreCase))
    |> Seq.map (fun asset -> async {
        printfn "Loading %d assets for %s" asset.Json.Costumes.Length asset.Name
        let! costumes =
            asset.Json.Costumes
            |> Seq.map (fun costume -> async {
                let url = sprintf "https://cdn.assets.scratch.mit.edu/internalapi/asset/%s/get/" costume.BaseLayerMd5
                try
                    let! content =
                        Http.retryPolicy.ExecuteAsync(fun () ->
                            async {
                                let! response = httpClient.GetAsync(url) |> Async.AwaitTask
                                return! response.Content.ReadAsStringAsync() |> Async.AwaitTask
                            }
                            |> Async.StartAsTask
                        )
                        |> Async.AwaitTask

                    return SvgImage.fromSvgData content
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
                SvgImage.fromSvgFile @"assets\Turtle1.svg"
                SvgImage.fromSvgFile @"assets\Turtle2.svg"
                SvgImage.fromSvgFile @"assets\Turtle3.svg"
            ]
        }
        {
            Name = "Ant"
            Categories = []
            Costumes = [
                SvgImage.fromSvgFile @"assets\Ant1.svg"
            ]
        }
        {
            Name = "Bug"
            Categories = []
            Costumes = [
                SvgImage.fromSvgFile @"assets\Bug1.svg"
            ]
        }
        {
            Name = "Spider"
            Categories = []
            Costumes = [
                SvgImage.fromSvgFile @"assets\Spider1.svg"
            ]
        }
    ]
    |> Seq.sortBy (fun p -> p.Name)
    |> Seq.map (fun asset ->
        let costumes =
            asset.Costumes
            |> List.map (fun c ->
                sprintf "{ Size = { Width = %f; Height = %f }; SvgData = \"\"\"%s\"\"\" }"
                    c.Width c.Height (String.removeNewLines c.Data)
            )
        sprintf "static member %s = PlayerData.Create [ %s ]" (String.toCamelCase asset.Name) (String.concat "; " costumes)
    )
    |> fun lines -> File.WriteAllLines("sprites.fs", lines)

