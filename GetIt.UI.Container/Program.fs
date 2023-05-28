module GetIt.UI.Container.Program

open Avalonia
open Avalonia.ReactiveUI
open Avalonia.Threading
open FSharp.Control.Reactive
open GetIt
open GetIt.UIV2
open GetIt.UIV2.ViewModels
open global.ReactiveUI
open System
open System.Net.Sockets
open System.Net
open System.Reactive
open System.Reactive.Subjects
open Thoth.Json.Net

let tryGetEnvVar = Environment.GetEnvironmentVariable >> Option.ofObj

let tryParseInt (text: string) =
    match Int32.TryParse(text) with
    | (true, value) -> Some value
    | _ -> None

let tryParseSize (text: string) =
    let parts = text.Split('x')
    let width = parts |> Array.tryItem 0 |> Option.bind tryParseInt
    let height = parts |> Array.tryItem 1 |> Option.bind tryParseInt
    match (width, height) with
    | Some width, Some height -> Some (width, height)
    | _ -> None

let tryParseIPEndPoint (text: string) =
    match IPEndPoint.TryParse(text) with
    | (true, v) -> Some v
    | _ -> None

[<EntryPoint>]
[<STAThread>]
let main argv =
    let (sceneWidth, sceneHeight) = tryGetEnvVar "GET_IT_SCENE_SIZE" |> Option.bind tryParseSize |> Option.defaultValue (800, 600)
    let startMaximized = tryGetEnvVar "GET_IT_START_MAXIMIZED" |> Option.isSome
    let serverAddress = tryGetEnvVar "GET_IT_SERVER_ADDRESS" |> Option.bind tryParseIPEndPoint
    match serverAddress with
    | Some serverAddress ->
        use connection = new TcpClient()
        connection.ConnectAsync(serverAddress) |> Async.AwaitTask |> Async.RunSynchronously
        use connectionStream = connection.GetStream()

        let messageSubject = Subject.fromStream connectionStream
        let (encode, decoder) = Encode.Auto.generateEncoder(), Decode.Auto.generateDecoder()
        let serverMessages = Subject.Create<_, _>(
            Observer.Create<UIToControllerMsg>(encode >> Encode.toString 0 >> messageSubject.OnNext),
            messageSubject |> Observable.map (Decode.fromString decoder)
        )

        let mainViewModel = MainWindowViewModel({ Width = float sceneWidth; Height = float sceneHeight }, startMaximized)
        use __ = MessageProcessing.run AvaloniaScheduler.Instance mainViewModel serverMessages

        AppBuilder.Configure<App>(fun () -> App(ViewModel = mainViewModel))
            .UsePlatformDetect()
            .LogToTrace(Logging.LogEventLevel.Debug)
            .UseReactiveUI()
            .StartWithClassicDesktopLifetime(argv)
    | _ ->
        eprintfn "Missing or invalid environment variable \"GET_IT_SERVER_ADDRESS\"."
        1
