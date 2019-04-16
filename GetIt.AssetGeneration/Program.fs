open System.Net.Http

[<EntryPoint>]
let main argv =
    use httpClient = new HttpClient()
    Sprites.generate httpClient
    0
