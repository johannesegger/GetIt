namespace GetIt

open System
open System.IO
open System.IO.Pipes
open System.Text
open Newtonsoft.Json
open Xamarin.Forms
open Xamarin.Forms.Platform.WPF

[<assembly: ExportRenderer(typeof<SkiaSharp.Views.Forms.SKCanvasView>, typeof<SkiaSharp.Wpf.SKCanvasViewRenderer>)>]
do ()

type MainWindow() =
    inherit FormsApplicationPage()
    // do
    //     base.SizeToContent <- Windows.SizeToContent.WidthAndHeight


module Main =
    let executeCommand cmd =
        match cmd with
        | ShowScene ->
            let start onStarted onClosed =
                let app = System.Windows.Application()
                Forms.Init()
                // ApplicationView.PreferredLaunchViewSize = new Size(480, 800);
                let window = MainWindow()
                Windows.Application.Current.Exit.Subscribe(fun args -> onClosed()) |> ignore
                window.LoadApplication(GetIt.App())
                onStarted()
                app.Run(window)
            let sceneBounds = GetIt.App.showScene start
            [ InitializedScene sceneBounds ]
        | AddPlayer player ->
            let playerId = GetIt.App.addPlayer player
            [ AddedPlayer (playerId, player) ]
        | UpdatePosition (playerId, position) ->
            GetIt.App.updatePlayerPosition playerId position
            [ UpdatedPosition (playerId, position) ]
        | RemovePlayer playerId ->
            GetIt.App.removePlayer playerId
            [ RemovedPlayer playerId ]

    [<EntryPoint>]
    let main(_args) =
        // System.Diagnostics.Debugger.Launch() |> ignore

        while true do
            try
                use pipeServer = new NamedPipeServerStream("GetIt", PipeDirection.InOut)
                pipeServer.WaitForConnection()

                use pipeReader = new StreamReader(pipeServer)
                use pipeWriter = new StreamWriter(pipeServer)
                let serializerSettings = JsonSerializerSettings(Formatting = Formatting.None)

                let mutable requestLine = pipeReader.ReadLine()
                while not <| isNull requestLine do
                    let requestMessages = JsonConvert.DeserializeObject<RequestMsg list>(requestLine, serializerSettings)
                    let responseMessages =
                        requestMessages
                        |> List.collect executeCommand

                    let responseLine = JsonConvert.SerializeObject(responseMessages, serializerSettings)
                    pipeWriter.WriteLine(responseLine)
                    pipeWriter.Flush()

                    requestLine <- pipeReader.ReadLine()
            with
            | :? IOException as e when e.Message = "Pipe is broken." ->
                printfn "Pipe error. Controller might have closed the pipe. %O" e
            | e -> eprintfn "=== Unexpected exception: %O" e

        printfn "Pipe closed."

        0
