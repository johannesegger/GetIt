namespace GetIt

open System
open System.Threading
open Fabulous.Core
open Fabulous.DynamicViews
open SkiaSharp
open Xamarin.Forms

module App =
    type PenLine =
        { Start: Position
          End: Position
          Weight: float
          Color: RGBA }

    type Model =
        { SceneBounds: GetIt.Rectangle
          Players: Map<PlayerId, PlayerData>
          PenLines: PenLine list
          MouseState: MouseState }

    let initModel =
        { SceneBounds = GetIt.Rectangle.zero
          Players = Map.empty
          PenLines = []
          MouseState = MouseState.empty }

    type Msg =
        | SetSceneBounds of GetIt.Rectangle
        | SetKeyboardKeyPressed of KeyboardKey
        | SetKeyboardKeyReleased of KeyboardKey
        | SetMousePosition of positionRelativeToSceneControl: Position
        | SetPlayerPosition of PlayerId * Position
        | SetPlayerDirection of PlayerId * Degrees
        | SetSpeechBubble of PlayerId * SpeechBubble option
        | UpdateAnswer of PlayerId * answer: string
        | ApplyAnswer of PlayerId
        | SetPen of PlayerId * Pen
        | SetSizeFactor of PlayerId * sizeFactor: float
        | NextCostume of PlayerId
        | AddPlayer of PlayerId * PlayerData
        | RemovePlayer of PlayerId
        | ClearScene
        | AddEventHandler of EventHandler
        | RemoveEventHandler of EventHandler
        | TriggerEvent of UIEvent
        | ExecuteAction of (unit -> unit)

    let init () = (initModel, Cmd.none)

    let update triggerEvent msg model =
        let updatePlayer playerId fn =
            let player = Map.find playerId model.Players |> fn
            { model with Players = Map.add playerId player model.Players }

        match msg with
        | SetSceneBounds bounds ->
            let model' = { model with SceneBounds = bounds }
            (model', Cmd.none)
        | SetKeyboardKeyPressed key ->
            (model, Cmd.none)
        | SetKeyboardKeyReleased key ->
            (model, Cmd.none)
        | SetMousePosition positionRelativeToSceneControl ->
            let position =
                { X = model.SceneBounds.Left + positionRelativeToSceneControl.X
                  Y = model.SceneBounds.Top - positionRelativeToSceneControl.Y }
            let model' = { model with MouseState = { model.MouseState with Position = position } }
            let cmd = Cmd.ofMsg (TriggerEvent (UIEvent.SetMousePosition position))
            (model', cmd)
        | SetPlayerPosition (playerId, position) ->
            let model' =
                let player = Map.find playerId model.Players
                let player' = { player with Position = position }
                { model with
                    Players = Map.add playerId player' model.Players
                    PenLines =
                        if player.Pen.IsOn
                        then
                            let line =
                                { Start = player.Position
                                  End = player'.Position
                                  Weight = player.Pen.Weight
                                  Color = player.Pen.Color }
                            line :: model.PenLines
                        else model.PenLines }
            (model', Cmd.none)
        | SetPlayerDirection (playerId, angle) ->
            let model' = updatePlayer playerId (fun p -> { p with Direction = angle })
            (model', Cmd.none)
        | SetSpeechBubble (playerId, speechBubble) ->
            let model' = updatePlayer playerId (fun p -> { p with SpeechBubble = speechBubble })
            (model', Cmd.none)
        | UpdateAnswer (playerId, answer) ->
            (model, Cmd.none)
        | ApplyAnswer playerId ->
            (model, Cmd.none)
        | SetPen (playerId, pen) ->
            let model' = updatePlayer playerId (fun p -> { p with Pen = pen })
            (model', Cmd.none)
        | SetSizeFactor (playerId, sizeFactor) ->
            let model' = updatePlayer playerId (fun p -> { p with SizeFactor = sizeFactor })
            (model', Cmd.none)
        | NextCostume playerId ->
            let model' = updatePlayer playerId Player.nextCostume
            (model', Cmd.none)
        | AddPlayer (playerId, player) ->
            let model' = { model with Players = Map.add playerId player model.Players }
            (model', Cmd.none)
        | RemovePlayer playerId ->
            (model, Cmd.none)
        | ClearScene ->
            (model, Cmd.none)
        | AddEventHandler eventHandler ->
            (model, Cmd.none)
        | RemoveEventHandler eventHandler ->
            (model, Cmd.none)
        | TriggerEvent event ->
            (model, Cmd.ofAsyncMsgOption (async { triggerEvent event; return None }))
        | ExecuteAction action ->
            (model, Cmd.none)

    [<System.Diagnostics.CodeAnalysis.SuppressMessage("Formatting", "TupleCommaSpacing") >]
    let view (model: Model) dispatch =
        let skColor color = SKColor(color.Red, color.Green, color.Blue, color.Alpha)
        let xfColor color = Color(float color.Red / 255., float color.Green / 255., float color.Blue / 255., float color.Alpha / 255.)

        let getPlayerView (player: PlayerData) =
            View.SKCanvasView(
                enableTouchEvents = true,
                paintSurface = (fun args ->
                    let info = args.Info
                    let surface = args.Surface
                    let canvas = surface.Canvas

                    canvas.Clear()

                    // see https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/curves/path-data
                    canvas.Translate(float32 info.Width / 2.f, float32 info.Height / 2.f)

                    let widthRatio = float32 info.Width / float32 player.Costume.Size.Width
                    let heightRatio = float32 info.Height / float32 player.Costume.Size.Height
                    canvas.Scale(System.Math.Min(widthRatio, heightRatio))

                    canvas.Translate(float32 player.Costume.Size.Width / -2.f, float32 player.Costume.Size.Height / -2.f)

                    player.Costume.Paths
                    |> List.iter (fun p ->
                        let path = SKPath.ParseSvgPathData(p.Data)
                        use paint = new SKPaint(Style = SKPaintStyle.Fill, Color = skColor p.FillColor)
                        canvas.DrawPath(path, paint)
                    )
                ),
                touch = (fun args ->
                    printfn "touch event at (%f, %f)" args.Location.X args.Location.Y
                )
            )

        let getPlayerInfoView (playerId, player) =
            let boxSize = { Width = 30.; Height = 30. }
            // let size = Size.scale boxSize player.Costume.Size
            View.StackLayout(
                orientation = StackOrientation.Horizontal,
                children = [
                    View.ContentView(
                        widthRequest = boxSize.Width,
                        heightRequest = boxSize.Height,
                        content = getPlayerView player
                    )
                    View.Label(
                        verticalOptions = LayoutOptions.Center,
                        text = sprintf "X: %.2f | Y: %.2f | ∠ %.2f°" player.Position.X player.Position.Y (Degrees.value player.Direction)
                    )
                    |> margin 10.
                ]
            )

        let getFullPlayerView (playerId, player: PlayerData) =
            View.AbsoluteLayout(
                backgroundColor = Color.DarkKhaki,
                children = [
                    // TODO put in separate container to minimize view diffs?
                    yield!
                        model.PenLines
                        |> List.map (fun line ->
                            let dx = line.End.X - line.Start.X
                            let dy = line.End.Y - line.Start.Y
                            View.BoxView(
                                color = xfColor line.Color,
                                widthRequest = Math.Sqrt(dx * dx + dy * dy),
                                heightRequest = line.Weight,
                                translationX = line.Start.X - model.SceneBounds.Left,
                                translationY = model.SceneBounds.Top - line.Start.Y,
                                rotation = 360. - Math.Atan2(dy, dx) * 180. / Math.PI,
                                anchorX = 0.,
                                anchorY = 0.
                            )
                        )

                    yield
                        View.ContentView(
                            gestureRecognizers = [
                                View.ClickGestureRecognizer(
                                    command = (fun () -> dispatch (TriggerEvent (ClickPlayer (playerId, Primary)))),
                                    buttons = ButtonsMask.Primary
                                )
                                View.ClickGestureRecognizer(
                                    command = (fun () -> dispatch (TriggerEvent (ClickPlayer (playerId, Secondary)))),
                                    buttons = ButtonsMask.Secondary
                                )
                            ],
                            widthRequest = player.Size.Width,
                            heightRequest = player.Size.Height,
                            content = getPlayerView player,
                            rotation = 360. - Degrees.value player.Direction
                        )
                        |> layoutBounds (Rectangle(player.Bounds.Left - model.SceneBounds.Left, model.SceneBounds.Top - player.Bounds.Top, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize))

                    match player.SpeechBubble with
                    | Some (Say text) ->
                        yield
                            View.AbsoluteLayout(
                                translationY = model.SceneBounds.Bottom - player.Bounds.Top,
                                children = [
                                    View.SKCanvasView(
                                        paintSurface = (fun args ->
                                            let info = args.Info
                                            let surface = args.Surface
                                            let canvas = surface.Canvas

                                            canvas.Clear()

                                            let markerDrawHeight = 15.f
                                            let markerRealHeight = 20.f

                                            let borderRadius = 15.f
                                            use outerBubble = new SKRoundRect(SKRect(0.f, 0.f, float32 info.Width, float32 info.Height - markerRealHeight), borderRadius, borderRadius)
                                            use bubbleBorderPaint = new SKPaint(Style = SKPaintStyle.Fill, Color = SKColors.Black)
                                            canvas.DrawRoundRect(outerBubble, bubbleBorderPaint)

                                            let borderWidth = 5.f
                                            use innerBubble = new SKRoundRect(SKRect(borderWidth, borderWidth, float32 info.Width - borderWidth, float32 info.Height - borderWidth - markerRealHeight), borderRadius - borderWidth, borderRadius - borderWidth)
                                            use bubbleFillPaint = new SKPaint(Style = SKPaintStyle.Fill, Color = SKColors.WhiteSmoke)
                                            canvas.DrawRoundRect(innerBubble, bubbleFillPaint)

                                            canvas.Translate(SKPoint((float32 info.Width - markerRealHeight) / 2.f, float32 info.Height - markerRealHeight))

                                            use markerBorderPaint = new SKPaint(Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Square, StrokeWidth = 5.f, Color = SKColors.Black)
                                            let path = SKPath.ParseSvgPathData(sprintf "M0,0 L0,%f %f,0" markerDrawHeight markerDrawHeight)
                                            canvas.DrawPath(path, markerBorderPaint)

                                            canvas.Translate(SKPoint(2.f, -borderWidth))

                                            use markerFillPaint = new SKPaint(Style = SKPaintStyle.Fill, Color = SKColors.WhiteSmoke)
                                            canvas.DrawPath(path, markerFillPaint)
                                        )
                                    )
                                    |> layoutBounds (Rectangle(0., 0., 1., 1.))
                                    |> layoutFlags AbsoluteLayoutFlags.All

                                    View.Frame(
                                        content = View.Label(
                                            text = text
                                        ),
                                        padding = 0.,
                                        margin = Thickness(10., 10., 10., 25.)
                                    )
                                ]
                            )
                            |> layoutFlags AbsoluteLayoutFlags.YProportional
                            |> layoutBounds (Rectangle(player.Bounds.Right - model.SceneBounds.Left + 20., (* TODO *)1., AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize))
                    | Some (Ask data) -> ()
                    | None -> ()
                ]
            )
            |> layoutFlags AbsoluteLayoutFlags.All
            |> layoutBounds (Rectangle(0., 0., 1., 1.))

        let players = Map.toList model.Players

        View.ContentPage(
            title = "GetIt",
        //   content = View.StackLayout(padding = 20.0, verticalOptions = LayoutOptions.Center,
        //     children = [ 
        //         View.Label(text = sprintf "%d" model.Count, horizontalOptions = LayoutOptions.Center, widthRequest=200.0, horizontalTextAlignment=TextAlignment.Center)
        //         View.Button(text = "Increment", command = (fun () -> dispatch Increment), horizontalOptions = LayoutOptions.Center)
        //         View.Button(text = "Decrement", command = (fun () -> dispatch Decrement), horizontalOptions = LayoutOptions.Center)
        //         View.Label(text = "Timer", horizontalOptions = LayoutOptions.Center)
        //         View.Switch(isToggled = model.TimerOn, toggled = (fun on -> dispatch (TimerToggled on.Value)), horizontalOptions = LayoutOptions.Center)
        //         View.Slider(minimumMaximum = (0.0, 10.0), value = double model.Step, valueChanged = (fun args -> dispatch (SetStep (int (args.NewValue + 0.5)))), horizontalOptions = LayoutOptions.FillAndExpand)
        //         View.Label(text = sprintf "Step size: %d" model.Step, horizontalOptions = LayoutOptions.Center) 
        //         View.Button(text = "Reset", horizontalOptions = LayoutOptions.Center, command = (fun () -> dispatch Reset), canExecute = (model <> initModel))
        //     ])
            content = View.StackLayout(
                children = [
                    View.AbsoluteLayout(
                        automationId = "scene",
                        gestureRecognizers = [
                            // TODO fix click position
                            View.ClickGestureRecognizer(
                                command = (fun () -> dispatch (TriggerEvent (ClickScene (Position.zero, Primary)))),
                                buttons = ButtonsMask.Primary
                            )
                            View.ClickGestureRecognizer(
                                command = (fun () -> dispatch (TriggerEvent (ClickScene (Position.zero, Secondary)))),
                                buttons = ButtonsMask.Secondary
                            )
                        ],
                        widthRequest = model.SceneBounds.Size.Width,
                        heightRequest = model.SceneBounds.Size.Height,
                        verticalOptions = LayoutOptions.FillAndExpand,
                        children = List.map getFullPlayerView players)
                    |> sizeChanged (fun e ->
                        let size = { Width = e.Width; Height = e.Height }
                        let bounds = { Position = { X = -size.Width / 2.; Y = -size.Height / 2. }; Size = size }
                        dispatch (SetSceneBounds bounds)
                    )
                    View.ScrollView(
                        verticalOptions = LayoutOptions.End,
                        //orientation = ScrollOrientation.Horizontal,
                        padding = Thickness(20., 10.),
                        backgroundColor = Color.LightGray,
                        content = View.StackLayout(
                            spacing = 0.,
                            orientation = StackOrientation.Horizontal,
                            children = List.map getPlayerInfoView players
                        )
                    )
                ]
            )
        )

    let dispatchSubject = new System.Reactive.Subjects.Subject<Msg>()
    let dispatchMessage = dispatchSubject.OnNext

    let subscription =
        Cmd.ofSub (fun dispatch ->
            let d = dispatchSubject.Subscribe(dispatch)
            ()
        )

    // Note, this declaration is needed if you enable LiveUpdate
    let program triggerEvent =
        Program.mkProgram init (update triggerEvent) view
        |> Program.withSubscription (fun _ -> subscription)

    let showScene start =
        use signal = new ManualResetEventSlim()
        let uiThread = Thread(fun () ->
            let cts = new CancellationTokenSource()
            let exitCode = start signal.Set (fun () -> cts.Cancel())
            Environment.Exit exitCode // shut everything down when the UI thread exits
        )

        uiThread.Name <- "Fabulous UI"
        uiThread.SetApartmentState ApartmentState.STA
        uiThread.IsBackground <- false
        uiThread.Start()
        signal.Wait()

    let setSceneBounds sceneBounds = dispatchMessage (SetSceneBounds sceneBounds)
    let addPlayer playerId player = dispatchMessage (AddPlayer (playerId, player))
    let removePlayer playerId = dispatchMessage (RemovePlayer playerId)
    let setPosition playerId position = dispatchMessage (SetPlayerPosition (playerId, position))
    let setDirection playerId angle = dispatchMessage (SetPlayerDirection (playerId, angle))
    let setSpeechBubble playerId speechBubble = dispatchMessage (SetSpeechBubble (playerId, speechBubble))
    let setPen playerId pen = dispatchMessage (SetPen (playerId, pen))
    let setSizeFactor playerId sizeFactor = dispatchMessage (SetSizeFactor (playerId, sizeFactor))
    let setNextCostume playerId = dispatchMessage (NextCostume playerId)
    let setMousePosition position = dispatchMessage (SetMousePosition position)

type App (triggerEvent) as app = 
    inherit Application ()

    let runner = 
        App.program triggerEvent
        // |> Program.withConsoleTrace // this slows down execution by a lot, so uncomment with caution
        |> Program.runWithDynamicView app

#if DEBUG
    // Uncomment this line to enable live update in debug mode. 
    // See https://fsprojects.github.io/Fabulous/tools.html for further  instructions.
    //
    //do runner.EnableLiveUpdate()
#endif

    // Uncomment this code to save the application state to app.Properties using Newtonsoft.Json
    // See https://fsprojects.github.io/Fabulous/models.html for further  instructions.
#if APPSAVE
    let modelId = "model"
    override __.OnSleep() = 

        let json = Newtonsoft.Json.JsonConvert.SerializeObject(runner.CurrentModel)
        Console.WriteLine("OnSleep: saving model into app.Properties, json = {0}", json)

        app.Properties.[modelId] <- json

    override __.OnResume() = 
        Console.WriteLine "OnResume: checking for model in app.Properties"
        try 
            match app.Properties.TryGetValue modelId with
            | true, (:? string as json) -> 

                Console.WriteLine("OnResume: restoring model from app.Properties, json = {0}", json)
                let model = Newtonsoft.Json.JsonConvert.DeserializeObject<App.Model>(json)

                Console.WriteLine("OnResume: restoring model from app.Properties, model = {0}", (sprintf "%0A" model))
                runner.SetCurrentModel (model, Cmd.none)

            | _ -> ()
        with ex -> 
            App.program.onError("Error while restoring model found in app.Properties", ex)

    override this.OnStart() = 
        Console.WriteLine "OnStart: using same logic as OnResume()"
        this.OnResume()
#endif
