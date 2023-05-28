module internal Subject

open FSharp.Control.Reactive
open System
open System.IO
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Text
open System.Threading.Tasks

let private getReceiveObservable (stream: Stream) =
    Observable.Create(fun (observer: IObserver<_>) ct ->
        async {
            use reader = new StreamReader(stream, UTF8Encoding())
            let mutable isDone = false
            while not isDone do
                let! message = reader.ReadLineAsync() |> Async.AwaitTask
                if not <| isNull message then
                    observer.OnNext(message)
                else
                    observer.OnCompleted()
                    isDone <- true
        }
        |> fun wf -> Async.StartAsTask(wf, cancellationToken = ct) :> Task
    )

let fromStream stream =
    let sendSubject = new Subject<_>()

    // Note: Returning subscription to caller for disposal
    // makes the code harder to read and is not strictly necessary
    let sendSubscription =
        sendSubject
        |> Observable.map (fun (message: string) ->
            Observable.ofAsync(async {
                use writer = new StreamWriter(stream, UTF8Encoding(), leaveOpen = true)
                do! writer.WriteLineAsync(message) |> Async.AwaitTask
            })
        )
        |> Observable.concatInner
        |> Observable.subscribe ignore

    let receiveObservable =
        getReceiveObservable stream
        |> Observable.publish
        |> Observable.refCount

    Subject.Create<_, _>(sendSubject, receiveObservable)
