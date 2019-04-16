module Backgrounds

open System
open System.IO
open System.Net.Http
open FSharp.Data

type ScratchBackdropProvider = JsonProvider<"backdrops.json">

type Background = {
    Name: string
    Categories: string list
    Image: SvgImage
}

let generate (httpClient: HttpClient) =
    ScratchBackdropProvider.GetSamples()
    |> Seq.filter (fun background -> background.Md5.EndsWith(".svg", StringComparison.InvariantCultureIgnoreCase))
    |> Seq.map (fun background -> async {
        printfn "Loading background %s" background.Name
        let url = sprintf "https://cdn.assets.scratch.mit.edu/internalapi/asset/%s/get/" background.Md5
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

            let image = SvgImage.fromSvgData content
            printfn "Loaded background %s" background.Name
            return
                {
                    Name = background.Name
                    Categories = background.Tags |> Seq.map String.firstToUpper |> Seq.toList
                    Image = image
                }
        with e ->
            failwithf "Error while loading data for %s at %s: %O" background.Name url e
            return Unchecked.defaultof<_>
    })
    |> Async.Parallel
    |> Async.RunSynchronously
    |> Seq.append [
        { Name = "None"; Categories = []; Image = { Width = 1.; Height = 1.; Data = """<svg width="1" height="1"><rect x="0" y="0" width="1" height="1" style="fill:#FFFFFF;" /></svg>""" } }
    ]
    |> Seq.sortBy (fun p -> p.Name)
    |> Seq.map (fun background ->
        sprintf "static member %s = { Size = { Width = %f; Height = %f }; SvgData = \"\"\"%s\"\"\" }"
            (String.toCamelCase background.Name)
            background.Image.Width
            background.Image.Height
            (String.removeNewLines background.Image.Data)
    )
    |> fun lines -> File.WriteAllLines("backgrounds.fs", lines)

