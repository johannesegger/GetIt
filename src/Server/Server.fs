open System.Collections.Immutable
open System.IO
open System.Reflection
open System.Threading
open Giraffe
open Giraffe.Serialization
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open MirrorSharp
open MirrorSharp.Advanced
open MirrorSharp.AspNetCore
open Newtonsoft.Json
open Saturn
open GameLib.Data.Global
open GameLib.Execution
open GameLib.Serialization

let publicPath = Path.GetFullPath "../Client"

let webApp = router {
    get "/api/init" (Successful.OK 42)
}

let configureSerialization (services:IServiceCollection) =
    let fableJsonSettings = Newtonsoft.Json.JsonSerializerSettings()
    fableJsonSettings.Converters.Add(Fable.JsonConverter())
    services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer fableJsonSettings)

let metadataReferences: MetadataReference list = [
    MetadataReference.CreateFromFile(typeof<obj>.Assembly.Location)
    MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location)
    MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location)
    MetadataReference.CreateFromFile(Assembly.Load("GameLib").Location)
    MetadataReference.CreateFromFile(Assembly.Load("GameLib.Server").Location)
]
let compilationOptions =
    CSharpCompilationOptions(
        OutputKind.DynamicallyLinkedLibrary,
        usings = [ "GameLib.Data"; "GameLib.Server" ]
    )

let setPlayer (session: IWorkSession) player =
    session.ExtensionData.["player"] <- player

let getPlayer (session: IWorkSession) =
    session.ExtensionData.["player"] :?> Player

let update (session: IWorkSession) (diagnostics: Diagnostic seq) = async {
    let compilationErrors =
        diagnostics
        |> Seq.filter (fun d -> d.Severity = DiagnosticSeverity.Error)
        |> Seq.map (fun d ->
            {
                Message = d.GetMessage()
                Span = { Start = d.Location.SourceSpan.Start; End = d.Location.SourceSpan.End }
            }
        )
        |> Seq.toList
    if compilationErrors |> List.isEmpty |> not
    then
        return Skipped (CompilationErrors compilationErrors)
    else
        let! ct = Async.CancellationToken
        let globals = {
            UserScript.ScriptGlobals.Player = getPlayer session
            UserScript.ScriptGlobals.CancellationToken = CancellationToken.None // is overwritten when running script
        }
        let! tree =
            session.Roslyn.Project.Documents
            |> Seq.exactlyOne
            |> fun d -> d.GetSyntaxTreeAsync(ct)
            |> Async.AwaitTask
        return!
            UserScript.rewriteForExecution tree
            |> UserScript.run metadataReferences compilationOptions.Usings globals
}

let jsonConverter = Fable.JsonConverter() :> JsonConverter

let writeUpdateResult (writer: IFastJsonWriter) (result: obj) session =
    JsonConvert.SerializeObject(result, [|jsonConverter|])
    |> writer.WriteValue

let app port = application {
    url (sprintf "http://0.0.0.0:%d/" port)
    use_router webApp
    memory_cache
    use_static publicPath
    service_config configureSerialization
    use_gzip
    app_config (fun app -> app.UseWebSockets())
    app_config (fun app ->
        let options =
            MirrorSharpOptions(
                SlowUpdate =
                    { new ISlowUpdateExtension with
                        member __.ProcessAsync(session, diagnostics, ct) =
                            async {
                                let! result = update session diagnostics
                                return result :> obj
                            }
                            |> fun a -> Async.StartAsTask(a, cancellationToken = ct)
                        member __.WriteResult(writer, result, session) =
                            writeUpdateResult writer result session
                    },
                SetOptionsFromClient =
                    { new ISetOptionsFromClientExtension with
                        member __.TrySetOption(session, name, value) =
                            match name with
                            | "x-player" ->
                                value
                                |> deserializePlayer jsonConverter
                                |> setPlayer session
                                true
                            | _ -> false
                    },
                IncludeExceptionDetails = true,
                ExceptionLogger =
                    { new IExceptionLogger with
                        member __.LogException(exn, session) =
                            eprintfn "=== Exception occured: %O" exn
                    },
                SelfDebugEnabled = true
            )
        options.CSharp.ParseOptions <- options.CSharp.ParseOptions.WithKind SourceCodeKind.Script
        options.CSharp.MetadataReferences <- ImmutableList.CreateRange metadataReferences
        options.CSharp.CompilationOptions <- compilationOptions.WithUsings(compilationOptions.Usings.Add("GameLib.DummyGlobals"))

        app.UseMirrorSharp(options)
    )
}

open Argu

type CLIArguments =
    | Port of port:int
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Port _ -> "specify a port the server listens at."

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<CLIArguments>()
    let results = parser.Parse argv
    let port = results.GetResult (Port, defaultValue = 8085)
    run (app port)
    0
