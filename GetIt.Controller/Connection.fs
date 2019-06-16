namespace GetIt

open System
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
        current <- Some connection

    }
