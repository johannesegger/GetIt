namespace GetIt

open System
open System.IO
open System.IO.Pipes
open System.Reactive.Concurrency
open System.Windows
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Windows.Threading
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
                TreeHelper.FindChildren<Xamarin.Forms.Platform.WPF.Controls.FormsNavigationPage>(window, forceUsingTheVisualTreeHelper = true)
                |> Seq.tryHead
                |> Option.bind (fun navigationPage ->
                    TreeHelper.FindChildren<FormsPanel>(navigationPage, forceUsingTheVisualTreeHelper = true)
                    |> Seq.filter (fun p -> p.Element.AutomationId = "scene")
                    |> Seq.tryHead
                )
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

    let private windowIcon =
        use stream = typeof<GetIt.App>.Assembly.GetManifestResourceStream("GetIt.UI.icon.png")
        let bitmap = BitmapImage()
        bitmap.BeginInit()
        bitmap.StreamSource <- stream
        bitmap.CacheOption <- BitmapCacheOption.OnLoad
        bitmap.EndInit()
        bitmap.Freeze()
        bitmap

    let private doWithWindow fn =
        let rec execute retries =
            if retries = 0 then failwith "Can't execute function with window: No more retries left."

            try
                System.Windows.Application.Current.Dispatcher.Invoke(fun () ->
                    if isNull System.Windows.Application.Current.MainWindow then failwith "No main window"

                    let window = System.Windows.Application.Current.MainWindow :?> MainWindow
                    fn window
                )
            with e ->
                printfn "Executing function with window failed: %s (Retries: %d)" e.Message retries
                System.Threading.Thread.Sleep(100)
                execute (retries - 1)
        execute 50

    let private doWithSceneControl fn =
        let rec execute retries =
            if retries = 0 then failwith "Can't execute function with scene control: No more retries left."

            try
                System.Windows.Application.Current.Dispatcher.Invoke(fun () ->
                    if isNull System.Windows.Application.Current.MainWindow then failwith "No main window"

                    let window = System.Windows.Application.Current.MainWindow :?> MainWindow

                    // TODO simplify if https://github.com/xamarin/Xamarin.Forms/issues/5921 is resolved
                    TreeHelper.FindChildren<Xamarin.Forms.Platform.WPF.Controls.FormsNavigationPage>(window, forceUsingTheVisualTreeHelper = true)
                    |> Seq.tryHead
                    |> Option.bind (fun navigationPage ->
                        TreeHelper.FindChildren<FormsPanel>(navigationPage, forceUsingTheVisualTreeHelper = true)
                        |> Seq.filter (fun p -> p.Element.AutomationId = "scene")
                        |> Seq.tryHead
                    )
                    |> function
                    | Some sceneControl -> fn (sceneControl :> FrameworkElement)
                    | None -> failwith "Scene control not found"
                )
            with e ->
                printfn "Executing function with scene control failed: %s (Retries: %d)" e.Message retries
                System.Threading.Thread.Sleep(100)
                execute (retries - 1)
        execute 50

    let controlToImage (control: FrameworkElement) =
        let renderTargetBitmap = RenderTargetBitmap(int control.ActualWidth, int control.ActualHeight, 96., 96., PixelFormats.Pbgra32)
        renderTargetBitmap.Render control
        let encoder = PngBitmapEncoder()
        encoder.Frames.Add(BitmapFrame.Create renderTargetBitmap)
        use stream = new MemoryStream()
        encoder.Save stream
        stream.ToArray() |> PngImage

    let setWindowTitle (window: Window) text =
        let title =
            match text with
            | Some text -> sprintf "Get It - %s" text
            | None -> "Get It"
        window.Title <- title

    let rec controllerToUIMsgToUIMessage = function
        | UIMsgProcessed -> None
        | ShowScene windowSize -> None
        | SetWindowTitle text -> None
        | SetBackground background -> App.SetBackground background |> Some
        | ClearScene -> App.ClearScene |> Some
        | MakeScreenshot -> None
        | AddPlayer (playerId, player) -> App.AddPlayer (playerId, player) |> Some
        | RemovePlayer playerId -> App.RemovePlayer playerId |> Some
        | SetPosition (playerId, position) -> App.SetPlayerPosition (playerId, position) |> Some
        | SetDirection (playerId, angle) -> App.SetPlayerDirection (playerId, angle) |> Some
        | SetSpeechBubble (playerId, speechBubble) -> App.SetSpeechBubble (playerId, speechBubble) |> Some
        | SetPen (playerId, pen) -> App.SetPen (playerId, pen) |> Some
        | SetSizeFactor (playerId, sizeFactor) -> App.SetSizeFactor (playerId, sizeFactor) |> Some
        | SetNextCostume playerId -> App.NextCostume playerId |> Some
        | ControllerEvent (KeyDown key) -> None
        | ControllerEvent (KeyUp key) -> None
        | ControllerEvent (MouseMove position) ->
            tryGetPositionOnSceneControl position
            |> Option.map App.SetMousePosition
        | ControllerEvent (MouseClick (mouseButton, position)) ->
            tryGetPositionOnSceneControl position
            |> Option.map (fun p -> App.ApplyMouseClick (mouseButton, p))
        | Batch messages ->
            List.choose controllerToUIMsgToUIMessage messages
            |> App.Batch
            |> Some

    let executeCommand cmd =
        match cmd with
        | UIMsgProcessed -> None
        | ShowScene windowSize ->
            let start onStarted onClosed =
                let app = System.Windows.Application()
                app.Exit.Subscribe(fun args -> onClosed()) |> ignore
                Forms.Init()
                let window = MainWindow()
                match windowSize with
                | SpecificSize size ->
                    window.Width <- size.Width
                    window.Height <- size.Height
                | Maximized ->
                    window.WindowState <- WindowState.Maximized
                setWindowTitle window None
                window.Icon <- windowIcon
                window.LoadApplication(GetIt.App eventSubject.OnNext)
                onStarted()
                app.Run(window)
            GetIt.App.showScene start
            // TODO remove if https://github.com/xamarin/Xamarin.Forms/issues/5910 is resolved
            doWithSceneControl (fun sceneControl -> sceneControl.ClipToBounds <- true)
            Some ControllerMsgProcessed
        | SetWindowTitle text ->
            doWithWindow (fun window -> setWindowTitle window text)
            Some ControllerMsgProcessed
        | MakeScreenshot ->
            let sceneImage =
                System.Windows.Application.Current.Dispatcher.Invoke(
                    (fun () -> controlToImage System.Windows.Application.Current.MainWindow),
                    DispatcherPriority.ApplicationIdle // ensure rendering happened
                )
            Some (UIEvent (Screenshot sceneImage))
        | SetBackground _
        | ClearScene
        | AddPlayer _
        | RemovePlayer _
        | SetPosition _
        | SetDirection _
        | SetSpeechBubble _
        | SetPen _
        | SetSizeFactor _
        | SetNextCostume _
        | ControllerEvent _
        | Batch _ as x ->
            controllerToUIMsgToUIMessage x
            |> Option.iter App.dispatchMessage
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
                    |> Observable.observeOn ThreadPoolScheduler.Instance
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
