namespace GetIt

open ColorCode
open FSharp.Control.Reactive
open Fue.Compiler
open Fue.Data
open System
open System.Diagnostics
open System.IO
open System.Reflection
open System.Runtime.InteropServices
open System.Text
open System.Threading

module internal Game =
    let mutable defaultTurtle = None

    let mutable private showSceneCalled = 0
    let mutable private communicationState : UICommunication.CommunicationState option = None

    let disposeCommunicationState () =
        match communicationState with
        | Some state ->
            (state :> IDisposable).Dispose()
            showSceneCalled <- 0
            communicationState <- None
        | None -> ()

    let doWithCommunicationState fn =
        match communicationState with
        | Some state ->
            try
                fn state
            with :? OperationCanceledException -> exit 0
        | None ->
            raise (GetItException "Connection to UI not set up. Consider calling `Game.ShowScene()` at the beginning.")

    let doWithMutableModel fn =
        doWithCommunicationState (fun s -> fn s.MutableModel)

    let showScene sceneSize =
        match Environment.GetEnvironmentVariable "GET_IT_SOCKET_URL" |> Option.ofObj with
        | Some socketUrl ->
            // TODO this feels ugly
            GetIt.UI.Container.Program.main [||]
            |> Environment.Exit
            failwith "Unreachable"
        | None ->
            if Interlocked.CompareExchange(&showSceneCalled, 1, 0) <> 0 then
                raise (GetItException "Connection to UI already set up. Do you call `Game.ShowScene()` multiple times?")

            let state = UICommunication.showScene sceneSize
            // Ensure controller process is stopped, even if we have e.g. an endless loop
            state.CancellationToken.Register(fun () ->
                printfn "Shutting down controller process"
                Environment.Exit 0
            ) |> ignore
            communicationState <- Some state
            Disposable.create disposeCommunicationState

    let addPlayer playerData = doWithCommunicationState (UICommunication.addPlayer playerData)
    let removePlayer playerId = doWithCommunicationState (UICommunication.removePlayer playerId)
    let setWindowTitle title = doWithCommunicationState (UICommunication.setWindowTitle title)
    let setBackground background = doWithCommunicationState (UICommunication.setBackground background)
    let clearScene () =  doWithCommunicationState UICommunication.clearScene
    let startBatch () = doWithCommunicationState UICommunication.startBatch
    let applyBatch () = doWithCommunicationState UICommunication.applyBatch
    let makeScreenshot () = doWithCommunicationState UICommunication.makeScreenshot
    let setPosition playerId position = doWithCommunicationState (UICommunication.setPosition playerId position)
    let changePosition playerId position = doWithCommunicationState (UICommunication.changePosition playerId position)
    let setDirection playerId angle = doWithCommunicationState (UICommunication.setDirection playerId angle)
    let changeDirection playerId angle = doWithCommunicationState (UICommunication.changeDirection playerId angle)
    let say playerId text = doWithCommunicationState (UICommunication.say playerId text)
    let shutUp playerId = doWithCommunicationState (UICommunication.shutUp playerId)
    let askString playerId question = doWithCommunicationState (UICommunication.askString playerId question)
    let askBool playerId question = doWithCommunicationState (UICommunication.askBool playerId question)
    let setPenState playerId isOn = doWithCommunicationState (UICommunication.setPenState playerId isOn)
    let togglePenState playerId = doWithCommunicationState (UICommunication.togglePenState playerId)
    let setPenColor playerId color = doWithCommunicationState (UICommunication.setPenColor playerId color)
    let shiftPenColor playerId angle = doWithCommunicationState (UICommunication.shiftPenColor playerId angle)
    let setPenWeight playerId weight = doWithCommunicationState (UICommunication.setPenWeight playerId weight)
    let changePenWeight playerId weight = doWithCommunicationState (UICommunication.changePenWeight playerId weight)
    let setSizeFactor playerId sizeFactor = doWithCommunicationState (UICommunication.setSizeFactor playerId sizeFactor)
    let changeSizeFactor playerId sizeFactor = doWithCommunicationState (UICommunication.changeSizeFactor playerId sizeFactor)
    let setNextCostume playerId = doWithCommunicationState (UICommunication.setNextCostume playerId)
    let sendToBack playerId = doWithCommunicationState (UICommunication.sendToBack playerId)
    let bringToFront playerId = doWithCommunicationState (UICommunication.bringToFront playerId)
    let setVisibility playerId isVisible = doWithCommunicationState (UICommunication.setVisibility playerId isVisible)
    let toggleVisibility playerId = doWithCommunicationState (UICommunication.toggleVisibility playerId)

    let onClickScene fn = doWithMutableModel (MutableModel.onClickScene fn)
    let onKeyDown key fn = doWithMutableModel (MutableModel.onKeyDown key fn)
    let onAnyKeyDown fn = doWithMutableModel (MutableModel.onAnyKeyDown fn)
    let onEnterPlayer playerId fn = doWithMutableModel (MutableModel.onEnterPlayer playerId fn)
    let onClickPlayer playerId fn = doWithMutableModel (MutableModel.onClickPlayer playerId fn)
    let whileKeyDown key interval fn = doWithMutableModel (MutableModel.whileKeyDown key interval fn)
    let whileAnyKeyDown interval fn = doWithMutableModel (MutableModel.whileAnyKeyDown interval fn)
    let getCurrentModel () = doWithMutableModel MutableModel.getCurrent

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
    /// <param name="playerData">The definition of the player that should be added.</param>
    /// <returns>The added player.</returns>
    static member AddPlayer (playerData: PlayerData) =
        if obj.ReferenceEquals(playerData, null) then raise (ArgumentNullException "playerData")

        let playerId = Game.addPlayer playerData
        new Player(playerId, (fun () -> Map.find playerId (Game.getCurrentModel().Players)), (fun () -> Game.removePlayer playerId))

    /// <summary>
    /// Adds a player to the scene and calls a method to control the player.
    /// The method runs on a thread pool thread so that multiple players can be controlled in parallel.
    /// </summary>
    /// <param name="playerData">The definition of the player that should be added.</param>
    /// <param name="run">The method that is used to control the player.</param>
    /// <returns>The added player.</returns>
    static member AddPlayer (playerData, run: Action<_>) =
        if obj.ReferenceEquals(playerData, null) then raise (ArgumentNullException "playerData")
        if obj.ReferenceEquals(run, null) then raise (ArgumentNullException "run")

        let player = Game.AddPlayer playerData
        async { run.Invoke player } |> Async.Start
        player

    static member private AddTurtle() =
        Game.defaultTurtle <- Some <| Game.AddPlayer PlayerData.Turtle

    /// Initializes and shows an empty scene and adds the default player to it.
    static member ShowSceneAndAddTurtle () =
        let d = Game.ShowScene ()
        Game.AddTurtle ()
        d

    /// Initializes and shows an empty scene with a specific size and adds the default player to it.
    static member ShowSceneAndAddTurtle (windowWidth, windowHeight) =
        let d = Game.ShowScene (windowWidth, windowHeight)
        Game.AddTurtle ()
        d

    /// Initializes and shows an empty scene with maximized size and adds the default player to it.
    static member ShowMaximizedSceneAndAddTurtle () =
        let d = Game.ShowMaximizedScene ()
        Game.AddTurtle ()
        d

    /// Sets the title of the window.
    static member SetWindowTitle text =
        let textOpt = if String.IsNullOrWhiteSpace text then None else Some text
        Game.setWindowTitle textOpt

    /// Sets the scene background.
    static member SetBackground background =
        if obj.ReferenceEquals(background, null) then raise (ArgumentNullException "background")
        Game.setBackground background

    /// Clears all drawings from the scene.
    static member ClearScene () =
        Game.clearScene ()

    /// <summary>
    /// Prints the scene. Note that `wkhtmltopdf` and `SumatraPDF` must be installed.
    /// </summary>
    /// <param name="printConfig">The configuration used for printing.</param>
    static member Print printConfig =
        if obj.ReferenceEquals(printConfig, null) then raise (ArgumentNullException "printConfig")

        if not <| RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            raise (GetItException (sprintf "Printing is not supported on operating system \"%s\"." RuntimeInformation.OSDescription))

        let assemblyDir =
            Assembly.GetCallingAssembly().Location
            |> Path.GetDirectoryName

        let base64ImageData =
            Game.makeScreenshot ()
            |> PngImage.toBase64String

        let documentTemplate =
            try
                File.ReadAllText printConfig.TemplatePath
            with e -> raise (GetItException ("Error while printing scene: Can't read print template.", e))

        let documentContent =
            let configParams =
                printConfig.TemplateParams
                |> Map.map (fun key value -> value :> obj)
                |> Map.toList
            let sourceFiles =
                let sourceFilesDir = Path.Combine(assemblyDir, "src")
                Directory.GetFiles(sourceFilesDir, "*.source", SearchOption.AllDirectories)
                |> Seq.map (fun f ->
                    let relativePath =
                        f.Substring(sourceFilesDir.Length).TrimStart('/', '\\')
                        |> fun p -> Path.ChangeExtension(p, null)
                    let content = File.ReadAllText f
                    let formattedContent = HtmlFormatter().GetHtmlString(content, Languages.CSharp)
                    relativePath, formattedContent
                )
                |> Seq.toList
            init
            |> addMany configParams
            |> add "sourceFiles" sourceFiles
            |> add "screenshot" base64ImageData
            |> fromText documentTemplate

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

            let sumatraStartInfo = ProcessStartInfo("sumatrapdf", sprintf "-print-to \"%s\" -print-settings \"duplex,color\" -silent -exit-when-done \"%s\"" printConfig.PrinterName pdfPath)
            let exitCode =
                try
                    use sumatraProcess = Process.Start(sumatraStartInfo)
                    sumatraProcess.WaitForExit()
                    sumatraProcess.ExitCode
                with e -> raise (GetItException ("Error while printing scene: Ensure `sumatrapdf` is installed.", e))
            if exitCode <> 0 then
                raise (GetItException (sprintf "SumatraPDF exited with non-zero exit code (%d). Ensure that printer \"%s\" is connected." exitCode printConfig.PrinterName))

    /// Start batching multiple commands to skip drawing intermediate state.
    /// Note that commands from all threads are batched.
    static member BatchCommands () =
        Game.startBatch ()
        Disposable.create Game.applyBatch

    /// <summary>
    /// Pauses execution of the current thread for a given time.
    /// </summary>
    /// <param name="duration">The length of the pause.</param>
    static member Sleep (duration: TimeSpan) =
        Thread.Sleep duration

    /// <summary>
    /// Pauses execution of the current thread for a given time.
    /// </summary>
    /// <param name="durationInMilliseconds">The length of the pause in milliseconds.</param>
    static member Sleep durationInMilliseconds =
        Game.Sleep (TimeSpan.FromMilliseconds durationInMilliseconds)

    /// <summary>
    /// Pauses execution until the mouse clicks at the scene.
    /// </summary>
    /// <returns>The position of the mouse click.</returns>
    static member WaitForMouseClick () =
        use signal = new ManualResetEventSlim()
        let mutable mouseClickEvent = None
        let fn ev =
            mouseClickEvent <- Some ev
            signal.Set()
        use d = Game.onClickScene fn
        signal.Wait()
        Option.get mouseClickEvent

    /// <summary>
    /// Pauses execution until a specific keyboard key is pressed.
    /// </summary>
    /// <param name="key">The keyboard key to wait for.</param>
    static member WaitForKeyDown key =
        if obj.ReferenceEquals(key, null) then raise (ArgumentNullException "key")

        use signal = new ManualResetEventSlim()
        use d = Game.onKeyDown key signal.Set
        signal.Wait()

    /// <summary>
    /// Pauses execution until any keyboard key is pressed.
    /// </summary>
    /// <returns>The keyboard key that is pressed.</returns>
    static member WaitForAnyKeyDown () =
        use signal = new ManualResetEventSlim()
        let mutable keyboardKey = None
        let fn key =
            keyboardKey <- Some key
            signal.Set()
        use d = Game.onAnyKeyDown fn
        signal.Wait()
        Option.get keyboardKey

    /// <summary>
    /// Checks whether a given keyboard key is pressed.
    /// </summary>
    /// <param name="key">The keyboard key.</param>
    /// <returns>True, if the keyboard key is pressed, otherwise false.</returns>
    static member IsKeyDown key =
        if obj.ReferenceEquals(key, null) then raise (ArgumentNullException "key")

        Game.getCurrentModel().KeyboardState.KeysPressed
        |> Set.contains key

    /// <summary>
    /// Checks whether any keyboard key is pressed.
    /// </summary>
    /// <returns>True, if any keyboard key is pressed, otherwise false.</returns>
    static member IsAnyKeyDown
        with get () =
            Game.getCurrentModel().KeyboardState.KeysPressed
            |> Set.isEmpty
            |> not

    /// <summary>
    /// Registers an event handler that is called once when any keyboard key is pressed.
    /// </summary>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnAnyKeyDown (action: Action<_>) =
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")

        Game.onAnyKeyDown action.Invoke

    /// <summary>
    /// Registers an event handler that is called once when a specific keyboard key is pressed.
    /// </summary>
    /// <param name="key">The keyboard key that should be listened to.</param>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnKeyDown (key, action: Action) =
        if obj.ReferenceEquals(key, null) then raise (ArgumentNullException "key")
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")

        Game.onKeyDown key action.Invoke

    /// <summary>
    /// Registers an event handler that is called continuously when any keyboard key is pressed.
    /// </summary>
    /// <param name="interval">How often the event handler should be called.</param>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnAnyKeyDown (interval, action: Action<_, _>) =
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")

        Game.whileAnyKeyDown interval (curry action.Invoke)

    /// <summary>
    /// Registers an event handler that is called continuously when a specific keyboard key is pressed.
    /// </summary>
    /// <param name="key">The keyboard key that should be listened to.</param>
    /// <param name="interval">How often the event handler should be called.</param>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnKeyDown (key, interval, action: Action<_>) =
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")

        Game.whileKeyDown key interval action.Invoke

    /// <summary>
    /// Registers an event handler that is called when the mouse is clicked anywhere on the scene.
    /// </summary>
    /// <param name="action">The event handler that should be called.</param>
    /// <returns>The disposable subscription.</returns>
    static member OnClickScene (action: Action<_>) =
        if obj.ReferenceEquals(action, null) then raise (ArgumentNullException "action")

        Game.onClickScene action.Invoke

    /// The bounds of the scene.
    static member SceneBounds
        with get () = Game.getCurrentModel().SceneBounds

    /// The current position of the mouse.
    static member MousePosition
        with get () = Game.getCurrentModel().MouseState.Position

