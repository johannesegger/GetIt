open Expecto
open FSharp.Control.Reactive
open GetIt

let tests =
    testList "Client-Server-Communication" [
        yield testCaseAsync "ShowScene with specific size" (async {
            let executeCommand cmd =
                match cmd with
                | ShowScene (SpecificSize windowSize) ->
                    let sceneBounds =
                        {
                            Position = { X = 1.; Y = 2. }
                            Size = { Width = windowSize.Width - 1.; Height = windowSize.Height - 2. }
                        }
                    let model = { App.initModel with SceneBounds = sceneBounds }
                    Some (UIResponseMsg (App.SetSceneBounds sceneBounds, model))
                | cmd -> failwithf "Unexpected command %A" cmd

            let (server, port) = Server.startWithAutoPort "localhost" executeCommand Observable.empty
            let! connection = UICommunication.setupConnectionToUI "localhost" port
            let client = Ui.UI.UIClient connection
            try
                let bounds =
                    SpecificSize { Width = 300.; Height = 200. }
                    |> Message.WindowSize.FromDomain
                    |> client.ShowScene
                    |> Message.Rectangle.ToDomain
                let expected = { Position = { X = 1.; Y = 2. }; Size = { Width = 299.; Height = 198. } }
                Expect.equal bounds expected "Scene bounds should come from UI"
            finally
                connection.ShutdownAsync() |> Async.AwaitTask |> Async.RunSynchronously
                server.ShutdownAsync() |> Async.AwaitTask |> Async.RunSynchronously
        })
    ]

[<EntryPoint>]
let main argv =
    runTests defaultConfig tests
