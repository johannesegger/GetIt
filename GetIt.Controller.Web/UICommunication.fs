namespace GetIt

open Elmish.Streams.AspNetCore.Middleware
open FSharp.Control
open FSharp.Control.Reactive
open global.Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open System
open System.Diagnostics
open System.IO
open System.Reactive.Disposables
open Thoth.Json.Net

module UICommunication =
    type CommunicationState = {
        Disposable: IDisposable
        MessageSubject: System.Reactive.Subjects.Subject<ControllerMsg>
    }
    let private communicationStateGate = obj()
    let mutable private communicationState = None

    let showScene windowSize =
        let (webServerCt, controllerMsgs) = lock communicationStateGate (fun () ->
            if Option.isSome communicationState then
                raise (GetItException "Connection to UI already set up. Do you call `Game.ShowSceneAndAddTurtle()` multiple times?")

            let subscriptionDisposable = new SingleAssignmentDisposable()
            let webServerStopDisposable = new CancellationDisposable()
            let controllerMsgs = new System.Reactive.Subjects.Subject<_>()
            communicationState <-
                Some {
                    Disposable =
                        subscriptionDisposable
                        |> Disposable.compose webServerStopDisposable
                    MessageSubject = controllerMsgs
                }
            (webServerStopDisposable.Token, controllerMsgs)
        )

        let stream (connectionId: ConnectionId) (msgs: IAsyncObservable<ChannelMsg * ConnectionId>) : IAsyncObservable<ChannelMsg * ConnectionId> =
            printfn "Client %s connected" connectionId
            let controllerMsgs =
                AsyncRx.create (fun obs -> async {
                    return
                        controllerMsgs
                        |> Observable.subscribeWithCallbacks
                            (obs.OnNextAsync >> Async.Start)
                            (obs.OnErrorAsync >> Async.Start)
                            (obs.OnCompletedAsync >> Async.Start)
                        |> fun d -> AsyncDisposable.Create (fun () -> async { d.Dispose() })
                })
                |> AsyncRx.map (fun msg -> ControllerMsg msg, "")
            
            msgs
            |> AsyncRx.flatMap(fun (msg, connId) ->
                match msg with
                | ControllerMsg msg -> AsyncRx.empty ()
                | UIMsg (SetSceneBounds sceneBounds) ->
                    Model.updateCurrent (fun model -> Some (SetSceneBounds sceneBounds), { model with SceneBounds = sceneBounds })
                    AsyncRx.empty ()
                | UIMsg (ApplyMouseClick mouseClick) ->
                    Model.updateCurrent (fun model -> Some (ApplyMouseClick mouseClick), model)
                    AsyncRx.empty ()
            )
            |> AsyncRx.merge controllerMsgs

        let configureApp (app: IApplicationBuilder) =
            app
                .UseWebSockets()
                .UseStream(fun options ->
                    { options with
                       Stream = stream
                       Encode = Encode.channelMsg >> Encode.toString 0
                       Decode = Decode.fromString Decode.channelMsg >> Result.toOption
                       RequestPath = MessageChannel.endpoint
                    }
                )
                // .UseGiraffe(webApp)
                |> ignore

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
                .RunAsync(webServerCt)

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

        printfn "Waiting for scene bounds"

        Model.observable
        |> Observable.firstIf (fst >> function | Some (SetSceneBounds _) -> true | _ -> false)
        |> Observable.wait
        |> ignore

        printfn "Setup complete"

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
        Model.updateCurrent (fun m -> None, { m with Players = Map.add playerId playerData m.Players |> Player.sendToBack playerId })
        playerId

    let removePlayer playerId =
        let playerId = PlayerId.create ()
        sendMessage <| RemovePlayer playerId
        Model.updateCurrent (fun m -> None, { m with Players = Map.remove playerId m.Players })
