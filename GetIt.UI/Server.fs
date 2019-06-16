namespace GetIt

open Grpc.Core

module Server =
    let private start host port executeCommand uiMessages =
        let server = Server()
        let port = server.Ports.Add(host, port, ServerCredentials.Insecure)
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
        (server, port)

    let startWithSpecificPort host port executeCommand uiMessages =
        start host port executeCommand uiMessages
        |> fst

    let startWithAutoPort host executeCommand uiMessage =
        start host 0 executeCommand uiMessage
