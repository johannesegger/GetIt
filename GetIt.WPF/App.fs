namespace GetIt

open System
open System.IO.Pipes
open System.Reactive.Linq
open FSharp.Control.Reactive
open Xamarin.Forms
open Xamarin.Forms.Platform.WPF

[<assembly: ExportRenderer(typeof<SkiaSharp.Views.Forms.SKCanvasView>, typeof<SkiaSharp.Wpf.SKCanvasViewRenderer>)>]
do ()

type MainWindow() =
    inherit FormsApplicationPage()
    // do
    //     base.SizeToContent <- Windows.SizeToContent.WidthAndHeight

module Main =
    let private eventSubject = new System.Reactive.Subjects.Subject<UIEvent>()

    let executeCommand cmd =
        match cmd with
        | UIMsgProcessed -> None
        | ShowScene sceneBounds ->
            let start onStarted onClosed =
                let app = System.Windows.Application()
                Forms.Init()
                // ApplicationView.PreferredLaunchViewSize = new Size(480, 800);
                let window = MainWindow()
                Windows.Application.Current.Exit.Subscribe(fun args -> onClosed()) |> ignore
                window.LoadApplication(GetIt.App eventSubject.OnNext)
                onStarted()
                app.Run(window)
            GetIt.App.showScene start
            GetIt.App.setSceneBounds sceneBounds
            Some ControllerMsgProcessed
        | AddPlayer (playerId, player) ->
            GetIt.App.addPlayer playerId player
            Some ControllerMsgProcessed
        | RemovePlayer playerId ->
            GetIt.App.removePlayer playerId
            Some ControllerMsgProcessed
        | SetPosition (playerId, position) ->
            GetIt.App.setPosition playerId position
            Some ControllerMsgProcessed
        | SetDirection (playerId, angle) ->
            GetIt.App.setDirection playerId angle
            Some ControllerMsgProcessed
        | SetSpeechBubble (playerId, speechBubble) ->
            GetIt.App.setSpeechBubble playerId speechBubble
            Some ControllerMsgProcessed
        | SetPen (playerId, pen) ->
            GetIt.App.setPen playerId pen
            Some ControllerMsgProcessed
        | SetSizeFactor (playerId, sizeFactor) ->
            GetIt.App.setSizeFactor playerId sizeFactor
            Some ControllerMsgProcessed
        | SetNextCostume playerId ->
            GetIt.App.setNextCostume playerId
            Some ControllerMsgProcessed
        | MouseMove position ->
            // TODO translate absolute position to scene position and trigger UIEvent if needed
            Some ControllerMsgProcessed
        | MouseClick ->
            // TODO translate absolute position to scene position and trigger UIEvent if needed
            Some ControllerMsgProcessed

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

                use eventSubscription =
                    eventSubject
                    |> Observable.map (fun evt -> IdentifiableMsg (Guid.NewGuid(), UIEvent evt))
                    |> Observable.subscribe subject.OnNext

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
