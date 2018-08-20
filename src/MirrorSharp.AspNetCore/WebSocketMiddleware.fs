module MirrorSharp.AspNetCore

open System
open System.Net.WebSockets
open System.Text
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open MirrorSharp.Internal


type WebSocketMiddleware(options, next : RequestDelegate) =
    inherit MiddlewareBase(options)

    member this.Invoke(ctx : HttpContext) =
        async {
            if ctx.Request.Path = PathString("/mirrorsharp") then
                if ctx.WebSockets.IsWebSocketRequest
                then
                    let! webSocket = ctx.WebSockets.AcceptWebSocketAsync() |> Async.AwaitTask

                    do!
                        async {
                            do! this.Loop(webSocket, CancellationToken.None) |> Async.AwaitIAsyncResult |> Async.Ignore
                        }
                        |> Async.StartAsTask
                        |> Async.AwaitTask
                else
                    ctx.Response.StatusCode <- 400
            else
                next.Invoke ctx |> ignore
        }
        |> Async.StartAsTask
        :> Task

    member this.Loop(webSocket, ct) = base.WebSocketLoopAsync(webSocket, ct)