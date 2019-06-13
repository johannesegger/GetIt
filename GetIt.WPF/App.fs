namespace GetIt

open System
open System.IO
open System.Reactive.Subjects
open System.Threading
open System.Windows
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Windows.Threading
open FSharp.Control.Reactive
open Grpc.Core
open Xamarin.Forms
open Xamarin.Forms.Platform.WPF
open Xamarin.Forms.Platform.WPF.Helpers
open GetIt.Windows

[<assembly: ExportRenderer(typeof<SkiaSharp.Views.Forms.SKCanvasView>, typeof<SkiaSharp.Wpf.SKCanvasViewRenderer>)>]
do ()

type MainWindow() =
    inherit FormsApplicationPage()

module Main =
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

            let result =
                match Option.ofObj System.Windows.Application.Current with
                | Some app ->
                    app.Dispatcher.Invoke(fun () ->
                        System.Windows.Application.Current.MainWindow
                        |> Option.ofObj
                        |> Result.ofOption "No main window"
                        |> Result.bind (fun window ->
                            let mainWindow = window :?> MainWindow
                            fn mainWindow
                        )
                    )
                | None -> Result.Error "Application.Current not set"
            match result with
            | Result.Ok p -> p
            | Result.Error e ->
                printfn "Executing function with window failed: %s (Retries: %d)" e retries
                System.Threading.Thread.Sleep(100)
                execute (retries - 1)
        execute 50

    let private doWithSceneControl fn =
        doWithWindow (fun window ->
            TreeHelper.FindChildren<FormsPanel>(window, forceUsingTheVisualTreeHelper = true)
            |> Seq.filter (fun p -> p.Element.AutomationId = "scene")
            |> Seq.tryHead
            |> function
            | Some sceneControl -> fn (sceneControl :> FrameworkElement) |> Result.Ok
            | None -> Result.Error "Scene control not found"
        )

    let private getPositionOnSceneControl positionOnScreen =
        doWithSceneControl (fun scene ->
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
            { X = scenePoint.X; Y = scenePoint.Y }
        )

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

    let executeCommand cmd =
        match cmd with
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
                window.LoadApplication(GetIt.App ())
                onStarted()
                app.Run(window)
            GetIt.App.showScene start
            // TODO remove if https://github.com/xamarin/Xamarin.Forms/issues/5910 is resolved
            doWithSceneControl (fun sceneControl -> sceneControl.ClipToBounds <- true)
            None
        | SetWindowTitle text ->
            doWithWindow (fun window -> setWindowTitle window text |> Result.Ok)
            None
        | MakeScreenshot ->
            let sceneImage =
                System.Windows.Application.Current.Dispatcher.Invoke(
                    (fun () -> controlToImage System.Windows.Application.Current.MainWindow),
                    DispatcherPriority.ApplicationIdle // ensure rendering happened
                )
            Some (Screenshot sceneImage)
        | MouseMoved virtualScreenPosition ->
            getPositionOnSceneControl virtualScreenPosition
            |> App.SetMousePosition
            |> App.uiMessages.OnNext
            None
        | MouseClicked virtualMouseClick ->
            let position = getPositionOnSceneControl virtualMouseClick.VirtualScreenPosition
            App.ApplyMouseClick (virtualMouseClick.Button, position)
            |> App.uiMessages.OnNext
            None
        | UIRequestMsg message ->
            App.uiMessages.OnNext message
            None

    [<EntryPoint>]
    let main(_args) =
        // System.Diagnostics.Debugger.Launch() |> ignore

        printfn "Starting message server."
        let server = Server()
        server.Ports.Add("localhost", 1503, ServerCredentials.Insecure) |> ignore
        let directResponseSubject = new System.Reactive.Subjects.Subject<_>()
        let executeCommand' cmd =
            executeCommand cmd
            |> Option.iter directResponseSubject.OnNext
        let uiMessages =
            App.uiMessages
            |> Observable.map UIResponseMsg
            |> Observable.merge directResponseSubject
        server.Services.Add(Ui.UI.BindService(UIServer(executeCommand', uiMessages)))
        server.Start()

        use mre = new ManualResetEventSlim()
        mre.Wait()

        0
