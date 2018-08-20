open System.IO
open System.Threading.Tasks

open Giraffe
open Giraffe.Serialization
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open MirrorSharp
open MirrorSharp.AspNetCore
open Saturn
open Shared

let publicPath = Path.GetFullPath "../Client/public"
let port = 8085us

let getInitCounter() : Task<Counter> = task { return 42 }

let webApp = router {
    get "/api/init" (fun next ctx ->
        task {
            let! counter = getInitCounter()
            return! Successful.OK counter next ctx
        })
}

let configureSerialization (services:IServiceCollection) =
    let fableJsonSettings = Newtonsoft.Json.JsonSerializerSettings()
    fableJsonSettings.Converters.Add(Fable.JsonConverter())
    services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer fableJsonSettings)

let app = application {
    url ("http://0.0.0.0:" + port.ToString() + "/")
    use_router webApp
    memory_cache
    use_static publicPath
    service_config configureSerialization
    use_gzip
    app_config (fun app -> app.UseWebSockets())
    app_config (fun app -> app.UseMiddleware<WebSocketMiddleware>([| MirrorSharpOptions() :> obj |]))
}

run app
