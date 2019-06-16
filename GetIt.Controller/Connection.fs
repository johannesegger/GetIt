namespace GetIt

open System
open FSharp.Control.Reactive
open Grpc.Core

module Connection =
    let mutable private current = None

    let hasCurrent () = Option.isSome current

    let run fn arg =
        match current with
        | Some (connection: Channel) ->
            try
                fn (Ui.UI.UIClient connection) arg
            with e ->
                // TODO verify it's the connection that failed 
                // TODO dispose subscriptions etc. ?
#if !DEBUG
                Environment.Exit 0
#endif
                raise (GetItException ("Error while executing command", e))
        | None ->
            raise (GetItException "Connection to UI not set up. Consider calling `Game.ShowSceneAndAddTurtle()` at the beginning.")

    let setup host port = async {
        let! connection = UICommunication.setupConnectionToUI host port

        let d0 =
            Observable.ofAsync (async {
                do! connection.WaitForStateChangedAsync(connection.State, Nullable<_>()) |> Async.AwaitTask
                return connection.State
            })
            |> Observable.repeat
            |> Observable.startWith [ connection.State ]
            |> Observable.pairwise
            // TODO not sure if this is the preferred way to detect server shutdown
            |> Observable.filter (function | (ChannelState.Ready, ChannelState.Idle) -> true | _ -> false)
            |> Observable.take 1
            |> Observable.subscribe (fun _ ->
                // Close the application if the UI has been closed (throwing an exception might be confusing)
                // TODO dispose subscriptions etc. ?
                Environment.Exit 0
            )

        current <- Some connection
    }
