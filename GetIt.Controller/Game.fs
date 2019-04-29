namespace GetIt

open System
open System.Diagnostics
open System.IO
open System.Runtime.InteropServices
open System.Text
open System.Threading
open FSharp.Control.Reactive
open Thoth.Json.Net

type PrintConfig =
    {
        TemplatePath: string
        TemplateParams: Map<string, string>
        PrinterName: string
    }
    with
        static member Create (templatePath, printerName) =
            {
                TemplatePath = templatePath
                TemplateParams = Map.empty
                PrinterName = printerName
            }
        /// Read the print config from the environment.
        static member CreateFromEnvironment () =
            let decoder =
                Decode.object (fun get ->
                    let templateParamsDecoder =
                        Decode.list (Decode.tuple2 Decode.string Decode.string)
                        |> Decode.map Map.ofList
                    {
                        TemplatePath = get.Required.Field "templatePath" Decode.string
                        TemplateParams =
                            get.Optional.Field "templateParams" templateParamsDecoder
                            |> Option.defaultValue Map.empty
                        PrinterName = get.Required.Field "printerName" Decode.string
                    }
                )
            let envVarName = "GET_IT_PRINT_CONFIG"
            let configString = Environment.GetEnvironmentVariable envVarName
            if isNull configString then raise (GetItException (sprintf "Can't read config from environment: Environment variable \"%s\" doesn't exist." envVarName))
            match Decode.fromString decoder configString with
            | Ok printConfig -> printConfig
            | Error e -> raise (GetItException (sprintf "Can't read config from environment: %s" e))

        /// Set the value of a template parameter.
        member this.Set (key, value) =
            { this with TemplateParams = Map.add key value this.TemplateParams }

module internal Game =
    let mutable defaultTurtle = None

    let showScene windowSize =
        UICommunication.setupLocalConnectionToUIProcess()

        do
            use enumerator =
                Model.observable
                |> Observable.skip 1 // Skip initial value
                |> Observable.filter (fun (modelChangeEvent, model) ->
                    match modelChangeEvent with
                    | UIToControllerMsg (UIEvent (SetSceneBounds sceneBounds)) -> true
                    | _ -> false
                )
                |> Observable.take 1
                |> Observable.getEnumerator

            UICommunication.sendCommand (ShowScene windowSize)

            if not <| enumerator.MoveNext() then
                raise (GetItException "UI didn't initialize properly: Didn't receive scene size).")

        do
            use enumerator =
                Model.observable
                |> Observable.skip 1 // Skip initial value
                |> Observable.filter (fun (modelChangeEvent, model) ->
                    match modelChangeEvent with
                    | UIToControllerMsg (UIEvent (SetMousePosition position)) -> true
                    | _ -> false
                )
                |> Observable.take 1
                |> Observable.getEnumerator

            let subject = new System.Reactive.Subjects.Subject<_>()
            let (mouseMoveObservable, otherEventsObservable) =
                subject
                |> Observable.split (function
                    | MouseMove _ as x -> Choice1Of2 x
                    | x -> Choice2Of2 x
                )
            let d1 =
                mouseMoveObservable
                |> Observable.sample (TimeSpan.FromMilliseconds 50.)
                |> Observable.subscribe (ControllerEvent >> UICommunication.sendCommand)

            let d2 =
                otherEventsObservable
                |> Observable.subscribe (ControllerEvent >> UICommunication.sendCommand)

            let d3 =
                if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
                    GetIt.Windows.DeviceEvents.register subject
                else
                    raise (GetItException (sprintf "Operating system \"%s\" is not supported." RuntimeInformation.OSDescription))

            if not <| enumerator.MoveNext() then
                raise (GetItException "UI didn't initialize properly: Didn't receive mouse position).")

        ()

    let addTurtle () =
        let turtleId = PlayerId.create ()
        UICommunication.sendCommand (AddPlayer (turtleId, PlayerData.Turtle))
        defaultTurtle <- Some (new Player (turtleId))

/// Defines methods to setup a game, add players, register global events and more.
[<AbstractClass; Sealed>]
type Game() =
    /// Initializes and shows an empty scene with the default size and no players on it.
    static member ShowScene () =
        Game.showScene (SpecificSize { Width = 800.; Height = 600. })

    /// Initializes and shows an empty scene with a specific size and no players on it.
    static member ShowScene (windowWidth, windowHeight) =
        Game.showScene (SpecificSize { Width = windowWidth; Height = windowHeight })

    /// Initializes and shows an empty scene with maximized size and no players on it.
    static member ShowMaximizedScene () =
        Game.showScene Maximized

    /// <summary>
    /// Adds a player to the scene.
    /// </summary>
    /// <param name="player">The definition of the player that should be added.</param>
    /// <returns>The added player.</returns>
    static member AddPlayer (playerData: PlayerData) =
        if obj.ReferenceEquals(playerData, null) then raise (ArgumentNullException "playerData")

        let playerId = PlayerId.create ()
        UICommunication.sendCommand (AddPlayer (playerId, playerData))
        new Player(playerId)

    /// <summary>
    /// Adds a player to the scene and calls a method to control the player.
    /// The method runs on a task pool thread so that multiple players can be controlled in parallel.
    /// </summary>
    /// <param name="player">The definition of the player that should be added.</param>
    /// <param name="run">The method that is used to control the player.</param>
    /// <returns>The added player.</returns>
    static member AddPlayer (playerData, run: Action<_>) =
        if obj.ReferenceEquals(playerData, null) then raise (ArgumentNullException "playerData")
        if obj.ReferenceEquals(run, null) then raise (ArgumentNullException "run")

        let player = Game.AddPlayer playerData
        async { run.Invoke player } |> Async.Start
        player

    /// Initializes and shows an empty scene and adds the default player to it.
    static member ShowSceneAndAddTurtle () =
        Game.ShowScene ()
        Game.addTurtle ()

    /// Initializes and shows an empty scene with a specific size and adds the default player to it.
    static member ShowSceneAndAddTurtle (windowWidth, windowHeight) =
        Game.showScene (SpecificSize { Width = windowWidth; Height = windowHeight })
        Game.addTurtle ()

    /// Initializes and shows an empty scene with maximized size and adds the default player to it.
    static member ShowMaximizedSceneAndAddTurtle () =
        Game.showScene Maximized
        Game.addTurtle ()

    static member SetWindowTitle (text) =
        let textOpt = if String.IsNullOrWhiteSpace text then None else Some text
        UICommunication.sendCommand (SetWindowTitle textOpt)

    /// Sets the scene background.
    static member SetBackground (background) =
        if obj.ReferenceEquals(background, null) then raise (ArgumentNullException "background")

        UICommunication.sendCommand (SetBackground background)

    /// Clears all drawings from the scene.
    static member ClearScene () =
        UICommunication.sendCommand ClearScene

    /// <summary>
    /// Prints the scene. Note that `wkhtmltopdf` and `SumatraPDF` must be installed.
    /// </summary>
    /// <param name="printConfig">The configuration used for printing.</param>
    static member Print printConfig =
        if obj.ReferenceEquals(printConfig, null) then raise (ArgumentNullException "printConfig")

        if not <| RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            raise (GetItException (sprintf "Printing is not supported for operating system \"%s\"." RuntimeInformation.OSDescription))

        use enumerator =
            Model.observable
            |> Observable.skip 1 // Skip initial value
            |> Observable.choose (fun (modelChangeEvent, model) ->
                match modelChangeEvent with
                | UIToControllerMsg (UIEvent (Screenshot (PngImage data))) -> Some data
                | _ -> None
            )
            |> Observable.take 1
            |> Observable.getEnumerator

        UICommunication.sendCommand MakeScreenshot

        if not <| enumerator.MoveNext() then
            raise (GetItException "Didn't receive screenshot from UI.")

        let base64ImageData =
            enumerator.Current
            |> Convert.ToBase64String
            |> sprintf "data:image/png;base64, %s"

        let instantiatePrintTemplate (templateContent: string) screenshotSrc =
            let templateParams =
                printConfig.TemplateParams
                |> Map.add "screenshot" screenshotSrc
            (templateContent, templateParams)
            ||> Map.fold (fun content key value -> content.Replace(sprintf "%%%s%%" key, value))

        let printHtmlDocument documentContent =
            let pdfPath = Path.Combine(Path.GetTempPath(), sprintf "%O.pdf" (Guid.NewGuid()))
            do
                let htmlPath = Path.Combine(Path.GetTempPath(), sprintf "%O.html" (Guid.NewGuid()))
                File.WriteAllText(htmlPath, documentContent, Encoding.UTF8)
                use d = Disposable.create (fun () -> try File.Delete(htmlPath) with _ -> ())

                let wkHtmlToPdfStartInfo = ProcessStartInfo("wkhtmltopdf", sprintf "\"%s\" \"%s\"" htmlPath pdfPath)
                let exitCode =
                    try
                        use wkHtmlToPdfProcess = Process.Start(wkHtmlToPdfStartInfo)
                        wkHtmlToPdfProcess.WaitForExit()
                        wkHtmlToPdfProcess.ExitCode
                    with e -> raise (GetItException ("Error while printing scene: Ensure `wkhtmltopdf` is installed", e))
                if exitCode <> 0 then
                    raise (GetItException (sprintf "wkhtmltopdf exited with non-zero exit code (%d)." exitCode))
            do
                use d = Disposable.create (fun () -> try File.Delete(pdfPath) with _ -> ())

                let sumatraStartInfo = ProcessStartInfo("sumatrapdf", sprintf "-print-to \"%s\" -silent -exit-when-done \"%s\"" printConfig.PrinterName pdfPath)
                let exitCode =
                    try
                        use sumatraProcess = Process.Start(sumatraStartInfo)
                        sumatraProcess.WaitForExit()
                        sumatraProcess.ExitCode
                    with e -> raise (GetItException ("Error while printing scene: Ensure `sumatrapdf` is installed.", e))
                if exitCode <> 0 then
                    raise (GetItException (sprintf "SumatraPDF exited with non-zero exit code (%d). Ensure that printer \"%s\" is connected." exitCode printConfig.PrinterName))

        let htmlTemplate =
            try
                File.ReadAllText printConfig.TemplatePath
            with e -> raise (GetItException ("Error while printing scene: Can't read print template.", e))

        let htmlDocument = instantiatePrintTemplate htmlTemplate base64ImageData
        printHtmlDocument htmlDocument

    /// Start batching multiple commands to skip drawing intermediate state.
    /// Note that commands from all threads are batched.
    static member BatchCommands () =
        UICommunication.sendCommand StartBatch
        Disposable.create (fun () -> UICommunication.sendCommand ApplyBatch)

    /// <summary>
    /// Pauses execution of the current thread for a given time.
    /// </summary>
    /// <param name="durationInMilliseconds">The length of the pause in milliseconds.</param>
    static member Sleep (durationInMilliseconds) =
        Thread.Sleep (TimeSpan.FromMilliseconds (durationInMilliseconds))

    /// <summary>
    /// Pauses execution until the mouse clicks at the scene.
    /// </summary>
    /// <returns>The position of the mouse click.</returns>
    static member WaitForMouseClick () =
        use signal = new ManualResetEventSlim()
        let mutable mouseClickEvent = Unchecked.defaultof<_>
        let fn mouseButton position =
            mouseClickEvent <- {
                MouseButton = mouseButton
                Position = position
            }
            signal.Set()
        use d = Model.onClickScene fn
        signal.Wait()
        mouseClickEvent

    /// <summary>
    /// Pauses execution until a specific keyboard key is pressed.
    /// </summary>
    /// <param name="key">The keyboard key to wait for.</param>
    static member WaitForKeyDown key =
        if obj.ReferenceEquals(key, null) then raise (ArgumentNullException "key")

        use signal = new ManualResetEventSlim()
        let fn () =
            signal.Set()
        use d = Model.onKeyDown key fn
        signal.Wait()

    /// <summary>
    /// Pauses execution until any keyboard key is pressed.
    /// </summary>
    /// <returns>The keyboard key that is pressed.</returns>
    static member WaitForAnyKeyDown () =
        use signal = new ManualResetEventSlim()
        let mutable keyboardKey = Unchecked.defaultof<_>
        let fn key =
            keyboardKey <- key
            signal.Set()
        use d = Model.onAnyKeyDown fn
        signal.Wait()
        keyboardKey

    /// <summary>
    /// Checks whether a given keyboard key is pressed.
    /// </summary>
    /// <param name="key">The keyboard key.</param>
    /// <returns>True, if the keyboard key is pressed, otherwise false.</returns>
    static member IsKeyDown key =
        if obj.ReferenceEquals(key, null) then raise (ArgumentNullException "key")

        Model.getCurrent().KeyboardState.KeysPressed
        |> Set.contains key

    /// <summary>
    /// Checks whether any keyboard key is pressed.
    /// </summary>
    /// <returns>True, if any keyboard key is pressed, otherwise false.</returns>
    static member IsAnyKeyDown
        with get () =
            Model.getCurrent().KeyboardState.KeysPressed
            |> Set.isEmpty
            |> not

    /// <summary>
    /// Registers an event handler that is called when any keyboard key is pressed.
    /// </summary>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnAnyKeyDown (action: Action<_>) =
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")

        Model.onAnyKeyDown action.Invoke

    /// <summary>
    /// Registers an event handler that is called when a specific keyboard key is pressed.
    /// </summary>
    /// <param name="key">The keyboard key that should be listened to.</param>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnKeyDown (key, action: Action) =
        if obj.ReferenceEquals(key, null) then raise (ArgumentNullException "key")
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")

        Model.onKeyDown key action.Invoke

    /// <summary>
    /// Registers an event handler that is called when the mouse is clicked anywhere on the scene.
    /// </summary>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnClickScene (action: Action<_, _>) =
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")

        Model.onClickScene (curry action.Invoke)

    /// The bounds of the scene.
    static member SceneBounds
        with get () = Model.getCurrent().SceneBounds

    /// The current position of the mouse.
    static member MousePosition
        with get () = Model.getCurrent().MouseState.Position
