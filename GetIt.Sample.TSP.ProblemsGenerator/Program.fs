open System
open System.IO
open System.IO.Compression
open System.Net.Http
open System.Text
open ICSharpCode.SharpZipLib.Tar

let skipSafe count source =
    System.Linq.Enumerable.Skip(source, count)

let readCoordinates (contents: byte[]) =
    Encoding.ASCII.GetString(contents).Split([| "\r\n"; "\r"; "\n" |], StringSplitOptions.None)
    |> Seq.skipWhile (fun line -> line <> "NODE_COORD_SECTION")
    |> skipSafe 1
    |> Seq.takeWhile (fun line -> line.Trim() <> "EOF" && line <> "")
    |> Seq.mapi (fun index line ->
        let parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
        if parts.Length <> 3 then failwithf "Line %d (\"%s\") is invalid" index line
        float parts.[1], float parts.[2]
    )
    |> Seq.toList

let readOptimalTour (contents: byte[]) =
    Encoding.ASCII.GetString(contents).Split([| "\r\n"; "\r"; "\n" |], StringSplitOptions.None)
    |> Seq.skipWhile (fun line -> line <> "TOUR_SECTION")
    |> skipSafe 1
    |> Seq.takeWhile (fun line -> line <> "-1")
    |> Seq.collect (fun line -> line.Split(' ', StringSplitOptions.RemoveEmptyEntries) |> Seq.map int)
    |> Seq.toList

[<EntryPoint>]
let main argv =
    async {
        let url = "https://wwwproxy.iwr.uni-heidelberg.de/groups/comopt/software/TSPLIB95/tsp/ALL_tsp.tar.gz"
        use httpClient = new HttpClient()
        let! entries = async {
            use! stream = httpClient.GetStreamAsync(url) |> Async.AwaitTask
            use zipStream = new GZipStream(stream, CompressionMode.Decompress)
            use archive = new TarInputStream(zipStream)
            return
                [
                    let mutable entry = archive.GetNextEntry()
                    while not <| isNull entry do
                        use entryStream = new MemoryStream()
                        archive.CopyEntryContents(entryStream)
                        entryStream.Position <- 0L
                        use entryZipStream = new GZipStream(entryStream, CompressionMode.Decompress)
                        use targetStream = new MemoryStream()
                        entryZipStream.CopyTo(targetStream)
                        yield entry.Name, targetStream.ToArray()
                        entry <- archive.GetNextEntry()
                ]
        }

        entries
        |> Seq.filter (fun (name, contents) -> name.EndsWith ".tsp.gz")
        |> Seq.choose (fun (name, contents) ->
            let problemName = name.Substring(0, name.IndexOf('.'))
            printfn "Generating code for problem \"%s\"" problemName
            entries
            |> Seq.tryFind (fun (name, contents) -> name.Equals(sprintf "%s.opt.tour.gz" problemName, StringComparison.InvariantCultureIgnoreCase))
            |> Option.bind (snd >> fun optimalTourContents ->
                let coordinates = readCoordinates contents
                match coordinates with
                | [] -> None
                | coords ->
                    let coordinatesString =
                        coords
                        |> List.map (sprintf "%A")
                        |> String.concat "; "
                        |> sprintf "[ %s ]"
                    let optimalTourString =
                        readOptimalTour optimalTourContents
                        |> List.map string
                        |> String.concat "; "
                        |> sprintf "[ %s ]"
                    sprintf "let %s = { Coordinates = %s; OptimalTour = %s }" problemName coordinatesString optimalTourString
                    |> Some
            )
        )
        |> Seq.append [ "module GetIt.Sample.TSP.Samples"; "" ]
        |> fun lines -> File.WriteAllLines("GetIt.Sample.TSP\\Samples.generated.fs", lines)
        
        return ()
    }
    |> Async.RunSynchronously
    0
