open System.IO
open System.Reflection

open Giraffe
open Giraffe.Serialization
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open MirrorSharp
open MirrorSharp.Advanced
open MirrorSharp.AspNetCore
open MirrorSharp.Extensions
open Saturn
open GameLib.Instruction

let publicPath = Path.GetFullPath "../Client/public"
let port = 8085us

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
]
let compilationOptions =
    CSharpCompilationOptions(
        OutputKind.DynamicallyLinkedLibrary,
        usings = [ "GameLib.Data"; "GameLib.Instructions" ]
    )

let update (session: IWorkSession) (diagnostics: Diagnostic seq) ct = async {
    if diagnostics |> Seq.exists (fun d -> d.Severity = DiagnosticSeverity.Error)
    then
        return Seq.empty
    else
        let tree = session.TryGetGenericLanguageSession<SyntaxTree>().Data
        return!
            UserScript.rewriteForExecution tree
            |> UserScript.run metadataReferences compilationOptions.Usings
}

open Newtonsoft.Json
let jsonConverter = Fable.JsonConverter() :> JsonConverter

let writeUpdateResult (writer: IFastJsonWriter) (result: obj) session =
    JsonConvert.SerializeObject(result, [|jsonConverter|])
    |> writer.WriteValue

let app = application {
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
                                let! result = update session diagnostics ct
                                return result :> obj
                            }
                            |> Async.StartAsTask
                        member __.WriteResult(writer, result, session) =
                            writeUpdateResult writer result session
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

        let language =
            GenericLanguage(
                "C# Script",
                fun text -> CSharpScriptSession.Create(text, metadataReferences, compilationOptions, typeof<obj>)
            )

        app.UseMirrorSharp(language, options)
    )
}

run app
