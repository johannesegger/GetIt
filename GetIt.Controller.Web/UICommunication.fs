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

type ControllerMessage = class end

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
    type CommunicationState = {
        Disposable: IDisposable
        MessageSubject: System.Reactive.Subjects.Subject<ControllerMessage>
    }
    let private communicationStateGate = obj()
    let mutable private communicationState = None

    let showScene windowSize =
        let (subscriptionDisposable, webServerStopDisposable, msgs) = lock communicationStateGate (fun () ->
            if Option.isSome communicationState then
                raise (GetItException "Connection to UI already set up. Do you call `Game.ShowSceneAndAddTurtle()` multiple times?")

            let subscriptionDisposable = new SingleAssignmentDisposable()
            let webServerStopDisposable = new CancellationDisposable()
            let msgs = new System.Reactive.Subjects.Subject<_>()
            communicationState <-
                Some {
                    Disposable =
                        subscriptionDisposable
                        |> Disposable.compose webServerStopDisposable
                    MessageSubject = msgs
                }
            (subscriptionDisposable, webServerStopDisposable, msgs)
        )

        let subscribe model =
            Cmd.ofSub (fun dispatch ->
                subscriptionDisposable.Disposable <-
                    Observable.subscribe dispatch msgs
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
#if DEBUG
                yield "--app", "http://localhost:8080" |> Some
#else
                yield "--app", url |> Some
#endif
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

        // TODO fail if process couldn't be started

        ()
