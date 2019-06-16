namespace GetIt

open Grpc.Core

module Server =
    let start host port executeCommand uiMessages =
        let server = Server()
        server.Ports.Add(host, port, ServerCredentials.Insecure) |> ignore
        let directResponseSubject = new System.Reactive.Subjects.Subject<_>()
        let executeCommand' cmd =
            executeCommand cmd
            |> Option.iter directResponseSubject.OnNext
        let allUIMessages =
            uiMessages
            |> Observable.map UIResponseMsg
            |> Observable.merge directResponseSubject
        server.Services.Add(Ui.UI.BindService(UIServer(executeCommand', allUIMessages)))
        server.Start()
        server
