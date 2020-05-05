open Chromely
open Chromely.CefGlue.Browser.EventParams
open Chromely.Core
open Chromely.Core.Configuration
open Chromely.Core.Helpers
open Chromely.Core.Host
open System
open System.Web
open System.IO

module Win32 =
    open System.Runtime.InteropServices
    [<DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)>]
    extern bool SetWindowText(IntPtr hWnd, string lpString)

type ChromelyApp() =
    inherit ChromelyBasicApp()
    override x.RegisterEvents(container: IChromelyContainer) =
        let eventHandler =
            ChromelyEventHandler<TitleChangedEventArgs>(
                CefEventKey.TitleChanged,
                fun sender e -> Win32.SetWindowText(x.Window.Handle, e.Title) |> ignore
            )
        container.RegisterInstance(typeof<CefEventHandlerTypes.ITitleChangedHandler>, eventHandler.Key, eventHandler)

let startUI cliArgs url windowSize startMaximized =
    let config = DefaultConfiguration.CreateForRuntimePlatform()
    config.WindowOptions.Title <- "Get It"
    config.WindowOptions.RelativePathToIconFile <- "icon.ico"
    windowSize
    |> Option.defaultValue (800, 600)
    |> fun (width, height) -> config.WindowOptions.Size <- WindowSize(width, height)
    config.WindowOptions.WindowState <- if startMaximized then WindowState.Maximize else WindowState.Normal
    config.StartUrl <- url
    config.CefDownloadOptions.DownloadSilently <- true
#if DEBUG
    config.DebuggingMode <- true
#endif
    AppBuilder
       .Create()
       .UseApp<ChromelyApp>()
       .UseConfiguration(config)
       .Build()
       .Run(cliArgs)

let tryGetEnvVar = Environment.GetEnvironmentVariable >> Option.ofObj

let tryParseInt (text: string) =
    match Int32.TryParse(text) with
    | (true, value) -> Some value
    | _ -> None

let tryParseSize (text: string) =
    let parts = text.Split('x')
    let width = parts |> Array.tryItem 0 |> Option.bind tryParseInt
    let height = parts |> Array.tryItem 1 |> Option.bind tryParseInt
    match (width, height) with
    | Some width, Some height -> Some (width, height)
    | _ -> None

[<EntryPoint>]
let main argv =
    // System.Diagnostics.Debugger.Launch() |> ignore
    let windowSize = tryGetEnvVar "GET_IT_WINDOW_SIZE" |> Option.bind tryParseSize
    let startMaximized = tryGetEnvVar "GET_IT_START_MAXIMIZED" |> Option.isSome
    let socketUrl = tryGetEnvVar "GET_IT_SOCKET_URL"
    let indexUrl = tryGetEnvVar "GET_IT_INDEX_URL"
    match socketUrl, indexUrl with
    | Some socketUrl, Some indexUrl ->
        let url =
            let absoluteIndexUrl =
                match Uri.TryCreate(indexUrl, UriKind.Relative) with
                | (true, _) -> Path.GetFullPath indexUrl
                | (false, _) -> indexUrl
            let builder = UriBuilder(absoluteIndexUrl)
            let query = builder.Query |> HttpUtility.ParseQueryString
            query.Add("socketUrl", Uri.EscapeDataString socketUrl)
            builder.Query <- query.ToString()
            builder.ToString()
        startUI argv url windowSize startMaximized
        0
    | _ ->
        eprintfn "Missing environment variable \"GET_IT_SOCKET_URL\" and/or \"GET_IT_INDEX_URL\"."
        1
