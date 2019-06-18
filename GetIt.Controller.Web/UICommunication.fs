namespace GetIt

open Elmish
open Elmish.Bridge
open FSharp.Control.Reactive
open global.Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open System
open System.Diagnostics
open System.IO
open System.Reactive.Disposables

module Shared =
    let endpoint = "/socket"

type internal Model =
    {
        SceneBounds: Rectangle
        Players: Map<PlayerId, PlayerData>
        MouseState: MouseState
        KeyboardState: KeyboardState
    }

module internal Model =
    let init dispatch () =
        {
            SceneBounds = Rectangle.zero
            Players = Map.empty
            MouseState = MouseState.empty
            KeyboardState = KeyboardState.empty
        },
        Cmd.none

    let update dispatch msg model = model, Cmd.none

module UICommunication =
    let showScene msgs windowSize =
        let subscriptionDisposable = new SingleAssignmentDisposable()
        let subscribe model =
            Cmd.ofSub (fun dispatch ->
                subscriptionDisposable.Disposable <-
                    msgs
                    |> Observable.subscribe dispatch
            )
        let server =
            Bridge.mkServer Shared.endpoint Model.init Model.update
            |> Bridge.withSubscription subscribe
            |> Bridge.run Giraffe.server

        let webApp = server

        let configureApp (app: IApplicationBuilder) =
            app
                .UseWebSockets()
                .UseGiraffe(webApp)

        let configureServices (services: IServiceCollection) =
            services.AddGiraffe() |> ignore

        let webServerStopDisposable = new CancellationDisposable()

        let url = "http://localhost:1503/"

        let webServerRunTask =
            WebHostBuilder()
                .UseKestrel()
                .UseWebRoot(Path.GetFullPath "../Client/public")
                .Configure(Action<IApplicationBuilder> configureApp)
                .ConfigureServices(configureServices)
                .UseUrls(url)
                .Build()
                .RunAsync(webServerStopDisposable.Token)

        // TODO check that chrome is installed and/or support other browsers
        let args =
            [
                yield "--user-data-dir", Path.Combine(Path.GetTempPath(), "chrome-workspace-for-getit") |> Some
                yield "--app", url |> Some
                match windowSize with
                | SpecificSize windowSize ->
                    yield "--window-size", sprintf "%d,%d" (int windowSize.Width) (int windowSize.Height) |> Some
                | Maximized ->
                    yield "--start-maximized", None
            ]
            |> List.map (function
                | key, Some value -> sprintf "%s=\"%s\"" key value
                | key, None -> key
            )
            |> String.concat " "
        let proc = Process.Start("chrome.exe", args)

        subscriptionDisposable
        |> Disposable.compose webServerStopDisposable
