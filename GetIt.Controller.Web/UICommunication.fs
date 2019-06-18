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

module UICommunication =
    type CommunicationState = {
        Disposable: IDisposable
        MessageSubject: System.Reactive.Subjects.Subject<ControllerMsg>
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
            printfn "Subscribe using model %A" model
            Cmd.ofSub (fun dispatch ->
                printfn "Setup subscription"
                subscriptionDisposable.Disposable <-
                    Observable.subscribe
                        (fun x ->
                            printfn "Dispatch message %A" x
                            dispatch x
                        )
                        msgs
            )
        let server =
            Bridge.mkServer CommunicationBridge.endpoint Model.init Model.update
#if DEBUG
            |> Bridge.withConsoleTrace
#endif
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

    let private sendMessage msg =
        match communicationState with
        | Some state ->
            state.MessageSubject.OnNext msg
        | None ->
            raise (GetItException "Connection to UI not set up. Consider calling `Game.ShowScene()` at the beginning.")

    let addPlayer playerData =
        let playerId = PlayerId.create ()
        sendMessage <| AddPlayer (playerId, playerData)
        Model.updateCurrent (fun m -> { m with Players = Map.add playerId playerData m.Players |> Player.sendToBack playerId })
        playerId

    let removePlayer playerId =
        let playerId = PlayerId.create ()
        sendMessage <| RemovePlayer playerId
        Model.updateCurrent (fun m -> { m with Players = Map.remove playerId m.Players })
