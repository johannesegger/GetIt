namespace GetIt

open FSharp.Control.Reactive
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Hosting.Server.Features
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection.Extensions
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Hosting
open System
open System.Threading.Tasks
open System.Reactive.Subjects

module internal WebSocketServer =
    let private handleWebSocketRequest socketPath (messageSubject: ISubject<_, _>) appStoppingCt =
        fun (httpContext: HttpContext) (next: Func<Task>) ->
            async {
                if httpContext.Request.Path = PathString socketPath then
                    if httpContext.WebSockets.IsWebSocketRequest then
                        use! webSocket = httpContext.WebSockets.AcceptWebSocketAsync() |> Async.AwaitTask
                        let (wsConnection, wsSubject) = ReactiveWebSocket.setup webSocket
                        use __ = wsConnection
                        use __ =
                            messageSubject
                            |> Observable.subscribeObserver wsSubject
                        use __ =
                            wsSubject
                            |> Observable.subscribeObserver messageSubject
                        do! Async.Sleep Int32.MaxValue
                    else
                        httpContext.Response.StatusCode <- 400
                else
                    do! next.Invoke() |> Async.AwaitTask
            }
            |> fun wf -> Async.HandleCancellation(wf, (fun e cont econt ccont -> cont ()), appStoppingCt)
            |> fun wf -> Async.StartAsTask(wf, cancellationToken = appStoppingCt) :> Task

    let start messageSubject (loggerFactory: ILoggerFactory) = async {
        let socketPath = "/msgs"

        let webHost =
            WebHostBuilder()
                .UseKestrel()
                .ConfigureServices(fun services ->
                    services.Replace(ServiceDescriptor(typeof<ILoggerFactory>, loggerFactory))
                    |> ignore
                )
                .Configure(fun  (app: IApplicationBuilder) ->
                    let appLifetime = app.ApplicationServices.GetService<IHostApplicationLifetime> ()
                    app
                        .UseWebSockets()
                        .Use(handleWebSocketRequest socketPath messageSubject appLifetime.ApplicationStopping)
                        |> ignore
                )
                .UseUrls("http://[::1]:0")
                .Build()

        do! webHost.StartAsync() |> Async.AwaitTask

        let socketUrl =
            let serverUrl = webHost.ServerFeatures.Get<IServerAddressesFeature>().Addresses |> Seq.head
            let uriBuilder = UriBuilder(serverUrl)
            uriBuilder.Scheme <- "ws"
            uriBuilder.Path <- socketPath
            uriBuilder.ToString()

        let serverDisposable =
            Disposable.create (fun () ->
                webHost.StopAsync()
                |> Async.AwaitTask
                |> Async.RunSynchronously
            )
        return (socketUrl, serverDisposable)
    }