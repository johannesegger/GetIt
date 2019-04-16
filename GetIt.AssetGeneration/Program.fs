open System.Net.Http

[<EntryPoint>]
let main argv =
    use httpClient = new HttpClient()
    Sprites.generate httpClient
    Backgrounds.generate httpClient
    0
