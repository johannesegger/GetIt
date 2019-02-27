namespace GetIt

open System
open System.IO
open System.IO.Pipes
open System.Text
open FSharp.Control.Reactive
open Thoth.Json.Net
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
        | ControllerToUIMsg.MsgProcessed -> None
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
            InitializedScene sceneBounds |> Some
        | AddPlayer (playerId, player) ->
            GetIt.App.addPlayer playerId player
            Some UIToControllerMsg.MsgProcessed
        | RemovePlayer playerId ->
            GetIt.App.removePlayer playerId
            Some UIToControllerMsg.MsgProcessed
        | SetPosition (playerId, position) ->
            GetIt.App.setPosition playerId position
            Some UIToControllerMsg.MsgProcessed
        | SetDirection (playerId, angle) ->
            GetIt.App.setDirection playerId angle
            Some UIToControllerMsg.MsgProcessed
        | SetSpeechBubble (playerId, speechBubble) ->
            GetIt.App.setSpeechBubble playerId speechBubble
            Some UIToControllerMsg.MsgProcessed
        | SetPen (playerId, pen) ->
            GetIt.App.setPen playerId pen
            Some UIToControllerMsg.MsgProcessed
        | SetSizeFactor (playerId, sizeFactor) ->
            GetIt.App.setSizeFactor playerId sizeFactor
            Some UIToControllerMsg.MsgProcessed
        | SetNextCostume playerId ->
            GetIt.App.setNextCostume playerId
            Some UIToControllerMsg.MsgProcessed

    [<EntryPoint>]
    let main(_args) =
        // System.Diagnostics.Debugger.Launch() |> ignore

        while true do
            try
                use pipeServer =
                    new NamedPipeServerStream(
                        "GetIt",
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous)
                pipeServer.WaitForConnection()

                let subject = MessageProcessing.forStream pipeServer UIToControllerMsg.encode ControllerToUIMsg.decode

                subject
                |> Observable.toEnumerable
                |> Seq.iter (fun (IdentifiableMsg (mId, msg)) ->
                    executeCommand msg
                    |> Option.map (fun response -> IdentifiableMsg (mId, response))
                    |> Option.iter subject.OnNext
                )
            with
            | e -> eprintfn "=== Unexpected exception: %O" e

        0
