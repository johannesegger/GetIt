module ReactiveWebSocket

open FSharp.Control.Reactive
open System
open System.Net.WebSockets
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Text
open System.Threading
open System.Threading.Tasks

let private receiveMessage (connection: WebSocket) =
    let rec fn messages = async {
        let buffer = ArraySegment<_>(Array.zeroCreate 4096)
        // Don't use cancellation token because WebSocket connection goes into 'Aborted' state and is not gracefully closed when cancellation is requested
        let! result = connection.ReceiveAsync(buffer, CancellationToken.None) |> Async.AwaitTask
        if result.MessageType = WebSocketMessageType.Close then
            return Choice2Of2 result.CloseStatus.Value
        elif result.EndOfMessage then
            return
                buffer.ToArray() :: messages
                |> List.rev
                |> Array.concat
                |> Encoding.UTF8.GetString
                |> Choice1Of2
        else
            return! fn (buffer.ToArray() :: messages)
    }
    fn []

let private getReceiveObservable (connection: WebSocket) =
    let rec fn (observer: IObserver<string>) = async {
        let! message = receiveMessage connection
        match message with
        | Choice1Of2 message ->
            observer.OnNext message
            return! fn observer
        | Choice2Of2 _ -> observer.OnCompleted()
    }
    Observable.Create(fun observer ct ->
        Async.StartAsTask(fn observer, cancellationToken = ct) :> Task
    )

let setup (connection: WebSocket) = async {
    let sendSubject = new Subject<_>()
    let sendSubscription =
        sendSubject
        |> Observable.map (fun (message: string) ->
            Observable.ofAsync(async {
                let buffer = Encoding.UTF8.GetBytes message |> ArraySegment<_>
                // Don't use cancellation token because WebSocket connection goes into 'Aborted' state and is not gracefully closed when cancellation is requested
                do! connection.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None) |> Async.AwaitTask
            })
        )
        |> Observable.concatInner
        |> Observable.subscribe ignore

    let receiveObservable =
        getReceiveObservable connection
        |> Observable.publish
        |> Observable.refCount
    let d = Disposable.create (fun () ->
        sendSubscription.Dispose()
        connection.CloseAsync(WebSocketCloseStatus.NormalClosure, "Shut down process", CancellationToken.None) |> Async.AwaitTask |> Async.RunSynchronously
        connection.Dispose()
    )
    return d, Subject.Create<_, _>(sendSubject, receiveObservable)
}
