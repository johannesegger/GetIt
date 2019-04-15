namespace GetIt

open System
open System.IO.Pipes
open System.Reactive.Linq
open System.Windows
open System.Windows.Media.Imaging
open FSharp.Control.Reactive
open Xamarin.Forms
open Xamarin.Forms.Platform.WPF
open Xamarin.Forms.Platform.WPF.Helpers
open GetIt.Windows

[<assembly: ExportRenderer(typeof<SkiaSharp.Views.Forms.SKCanvasView>, typeof<SkiaSharp.Wpf.SKCanvasViewRenderer>)>]
do ()

type MainWindow() =
    inherit FormsApplicationPage()

module Main =
    let private eventSubject = new System.Reactive.Subjects.Subject<UIEvent>()

    let private tryGetPositionOnSceneControl positionOnScreen =
        System.Windows.Application.Current.Dispatcher.Invoke(fun () ->
            if isNull System.Windows.Application.Current.MainWindow then None
            else
                let window = System.Windows.Application.Current.MainWindow :?> MainWindow

                // TODO simplify if https://github.com/xamarin/Xamarin.Forms/issues/5921 is resolved
                let navigationPage =
                    TreeHelper.FindChildren<Xamarin.Forms.Platform.WPF.Controls.FormsNavigationPage>(window, forceUsingTheVisualTreeHelper = true)
                    |> Seq.head
                TreeHelper.FindChildren<FormsPanel>(navigationPage, forceUsingTheVisualTreeHelper = true)
                |> Seq.filter (fun p -> p.Element.AutomationId = "scene")
                |> Seq.tryHead
                |> Option.bind (fun scene ->
                    try
                        let virtualDesktopLeft = Win32.GetSystemMetrics(Win32.SystemMetric.SM_XVIRTUALSCREEN)
                        let virtualDesktopTop = Win32.GetSystemMetrics(Win32.SystemMetric.SM_YVIRTUALSCREEN)
                        let virtualDesktopWidth = Win32.GetSystemMetrics(Win32.SystemMetric.SM_CXVIRTUALSCREEN)
                        let virtualDesktopHeight = Win32.GetSystemMetrics(Win32.SystemMetric.SM_CYVIRTUALSCREEN)

                        let screenPoint =
                            System.Windows.Point(
                                float virtualDesktopWidth * positionOnScreen.X + float virtualDesktopLeft,
                                float virtualDesktopHeight * positionOnScreen.Y + float virtualDesktopTop
                            )
                    
                        let scenePoint = scene.PointFromScreen(screenPoint)
                        Some { X = scenePoint.X; Y = scenePoint.Y }
                    with _ -> None
            )
        )

    let windowIcon =
        use stream = typeof<GetIt.App>.Assembly.GetManifestResourceStream("GetIt.UI.icon.png")
        let bitmap = BitmapImage()
        bitmap.BeginInit()
        bitmap.StreamSource <- stream
        bitmap.CacheOption <- BitmapCacheOption.OnLoad
        bitmap.EndInit()
        bitmap.Freeze()
        bitmap

    let executeCommand cmd =
        match cmd with
        | UIMsgProcessed -> None
        | ShowScene windowSize ->
            let start onStarted onClosed =
                let app = System.Windows.Application()
                app.Exit.Subscribe(fun args -> onClosed()) |> ignore
                Forms.Init()
                let window = MainWindow()
                window.Width <- windowSize.Width
                window.Height <- windowSize.Height
                window.Title <- "Get It"
                window.LoadApplication(GetIt.App eventSubject.OnNext)
                window.Icon <- windowIcon
                onStarted()
                app.Run(window)
            GetIt.App.showScene start
            Some ControllerMsgProcessed
        | ClearScene ->
            GetIt.App.clearScene ()
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
        | ControllerEvent (KeyDown key) ->
            Some ControllerMsgProcessed
        | ControllerEvent (KeyUp key) ->
            Some ControllerMsgProcessed
        | ControllerEvent (MouseMove position) ->
            tryGetPositionOnSceneControl position
            |> Option.iter GetIt.App.setMousePosition
            Some ControllerMsgProcessed
        | ControllerEvent (MouseClick (mouseButton, position)) ->
            tryGetPositionOnSceneControl position
            |> Option.iter (GetIt.App.applyMouseClick mouseButton)
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
