namespace GetIt.WPF

open System

open Xamarin.Forms
open Xamarin.Forms.Platform.WPF

[<assembly: ExportRenderer(typeof<SkiaSharp.Views.Forms.SKCanvasView>, typeof<SkiaSharp.Wpf.SKCanvasViewRenderer>)>]
do ()

module Main = 
    [<EntryPoint>]
    let main(_args) =
        let start onStarted onClosed =
            let app = System.Windows.Application()
            Forms.Init()
            let window = FormsApplicationPage()
            Windows.Application.Current.Exit.Subscribe(fun args -> onClosed()) |> ignore
            window.LoadApplication(GetIt.App())
            onStarted()
            app.Run(window)

        GetIt.App.showScene start

        0
