#r "paket: groupref build //"
#load "./.fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "netstandard"
#r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

open System

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.Api

let serverPath = Path.getFullName "./src/Server"
let clientPath = Path.getFullName "./src/Client"
let deployDir = Path.getFullName "./deploy"

let platformTool tool winTool =
    let tool = if Environment.isUnix then tool else winTool
    match Process.tryFindFileOnPath tool with
    | Some t -> t
    | _ ->
        let errorMsg =
            tool + " was not found in path. " +
            "Please install it and make sure it's available from your path. " +
            "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
        failwith errorMsg

let nodeTool = platformTool "node" "node.exe"
let yarnTool = platformTool "yarn" "yarn.cmd"

let runTool cmd args workingDir =
    let result =
        Process.execSimple (fun info ->
            { info with
                FileName = cmd
                WorkingDirectory = workingDir
                Arguments = args })
            TimeSpan.MaxValue
    if result <> 0 then failwithf "'%s %s' failed" cmd args

let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

let openBrowser url =
    let result =
        //https://github.com/dotnet/corefx/issues/10361
        Process.execSimple (fun info ->
            { info with
                FileName = url
                UseShellExecute = true })
            TimeSpan.MaxValue
    if result <> 0 then failwithf "opening browser failed"

Target.create "Clean" (fun _ ->
    Shell.cleanDirs [deployDir]
)

Target.create "InstallClient" (fun _ ->
    printfn "Node version:"
    runTool nodeTool "--version" __SOURCE_DIRECTORY__
    printfn "Yarn version:"
    runTool yarnTool "--version" __SOURCE_DIRECTORY__
    runTool yarnTool "install --frozen-lockfile" __SOURCE_DIRECTORY__
    runDotNet "restore" clientPath
)

Target.create "RestoreServer" (fun _ ->
    runDotNet "restore" serverPath
)

Target.create "Build" (fun _ ->
    runDotNet "build" serverPath
    runDotNet "fable webpack --port free -- -p" clientPath
)

Target.create "Run" (fun _ ->
    let server = async {
        runDotNet "watch run" serverPath
    }
    let client = async {
        runDotNet "fable webpack-dev-server --port free" clientPath
    }
    let browser = async {
        do! Async.Sleep 5000
        openBrowser "http://localhost:8080"
    }

    [ server; client; browser ]
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
)

let runtime = "win7-x64"

Target.create "Bundle" (fun _ ->
    let serverDir = Path.combine deployDir "Server"
    let clientDir = Path.combine deployDir "Client"
    let publicDir = Path.combine clientDir "public"

    let publishArgs = sprintf "publish -c Release -o \"%s\" -r \"%s\"" serverDir runtime
    runDotNet publishArgs serverPath

    let files = System.IO.Directory.GetFiles(deployDir, "*", System.IO.SearchOption.AllDirectories)
    Zip.zip deployDir (sprintf "%s/play-and-learn-%s.zip" deployDir runtime) files
)

Target.create "GitHubRelease" (fun _ ->
    let token =
        match Environment.environVarOrDefault "github_token" "" with
        | s when not (System.String.IsNullOrWhiteSpace s) -> s
        | _ -> failwith "Please set the github_token environment variable to a github personal access token with repro access."

    let version =
        match Environment.environVarOrDefault "release_version" "" with
        | s when SemVer.isValid s -> SemVer.parse s
        | _ -> failwith "Please set the release_version environment variable to a SemVer compatible version."

    let releaseNotes =
        match Environment.environVarOrDefault "release_notes" "" with
        | s when not (System.String.IsNullOrWhiteSpace s) -> String.splitStr Environment.NewLine s
        | _ -> failwith "Please set the release_notes environment variable to a non-empty string."

    let files =
        [ runtime ]
        |> List.map (fun n -> sprintf "%s/play-and-learn-%s.zip" deployDir n)

    GitHub.createClientWithToken token
    |> GitHub.draftNewRelease "johannesegger" "PlayAndLearn" version.AsString (version.PreRelease <> None) releaseNotes
    |> GitHub.uploadFiles files
    |> GitHub.publishDraft
    |> Async.RunSynchronously
)

open Fake.Core.TargetOperators

"Clean"
    ==> "InstallClient"
    ==> "Build"
    ==> "Bundle"
    ==> "GitHubRelease"

"Clean"
    ==> "InstallClient"
    ==> "RestoreServer"
    ==> "Run"

Target.runOrDefault "Build"
