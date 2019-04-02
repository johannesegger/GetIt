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
          PenLines: PenLine list }

    let initModel =
        { SceneBounds = GetIt.Rectangle.zero
          Players = Map.empty
          PenLines = [] }

    type Msg =
        | SetSceneBounds of GetIt.Rectangle
        | SetKeyboardKeyPressed of KeyboardKey
        | SetKeyboardKeyReleased of KeyboardKey
        | SetMousePosition of positionRelativeToSceneControl: Position
        | ApplyMouseClick of MouseButton * positionRelativeToSceneControl: Position
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
        | ExecuteAction of (unit -> unit)

    let init () = (initModel, Cmd.none)

    let update triggerEvent msg model =
        let updatePlayer playerId fn =
            let player = Map.find playerId model.Players |> fn
            { model with Players = Map.add playerId player model.Players }

        let triggerEventCmd event =
            Cmd.ofAsyncMsgOption (async { triggerEvent event; return None })

        match msg with
        | SetSceneBounds bounds ->
            let model' = { model with SceneBounds = bounds }
            let cmd = triggerEventCmd (UIEvent.SetSceneBounds bounds)
            (model', cmd)
        | SetKeyboardKeyPressed key ->
            (model, Cmd.none)
        | SetKeyboardKeyReleased key ->
            (model, Cmd.none)
        | SetMousePosition positionRelativeToSceneControl ->
            let position =
                { X = model.SceneBounds.Left + positionRelativeToSceneControl.X
                  Y = model.SceneBounds.Top - positionRelativeToSceneControl.Y }
            let cmd = triggerEventCmd (UIEvent.SetMousePosition position)
            (model, cmd)
        | ApplyMouseClick (mouseButton, positionRelativeToSceneControl) ->
            let position =
                { X = model.SceneBounds.Left + positionRelativeToSceneControl.X
                  Y = model.SceneBounds.Top - positionRelativeToSceneControl.Y }
            let cmd = triggerEventCmd (UIEvent.ApplyMouseClick (mouseButton, position))
            (model, cmd)
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
                            model.PenLines @ [ line ]
                        else model.PenLines }
            (model', Cmd.none)
        | SetPlayerDirection (playerId, angle) ->
            let model' = updatePlayer playerId (fun p -> { p with Direction = angle })
            (model', Cmd.none)
        | SetSpeechBubble (playerId, speechBubble) ->
            let model' = updatePlayer playerId (fun p -> { p with SpeechBubble = speechBubble })
            (model', Cmd.none)
        | UpdateAnswer (playerId, answer) ->
            let model' = updatePlayer playerId (fun p ->
                match p.SpeechBubble with
                | Some (Ask askData) -> { p with SpeechBubble = Some (Ask { askData with Answer = Some answer }) }
                | Some (Say _)
                | None -> p
            )
            (model', Cmd.none)
        | ApplyAnswer playerId ->
            let model' = updatePlayer playerId (fun p ->
                match p.SpeechBubble with
                | Some (Ask askData) -> { p with SpeechBubble = None }
                | Some (Say _)
                | None -> p
            )

            let cmd =
                model.Players
                |> Map.tryFind playerId
                |> Option.bind (fun p ->
                    match p.SpeechBubble with
                    | Some (Ask askData) -> Option.defaultValue "" askData.Answer |> Some
                    | Some (Say _)
                    | None -> None
                )
                |> Option.map (fun answer -> triggerEventCmd (UIEvent.AnswerQuestion (playerId, answer)))
                |> Option.defaultValue Cmd.none
            (model', cmd)
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
            let model' = { model with PenLines = [] }
            (model', Cmd.none)
        | AddEventHandler eventHandler ->
            (model, Cmd.none)
        | RemoveEventHandler eventHandler ->
            (model, Cmd.none)
        | ExecuteAction action ->
            (model, Cmd.none)

    [<System.Diagnostics.CodeAnalysis.SuppressMessage("Formatting", "TupleCommaSpacing") >]
    let view (model: Model) dispatch =
        let skColor color = SKColor(color.Red, color.Green, color.Blue, color.Alpha)
        let xfColor color = Color(float color.Red / 255., float color.Green / 255., float color.Blue / 255., float color.Alpha / 255.)

        let getPlayerView (player: PlayerData) =
            // see https://github.com/fsprojects/Fabulous/issues/261
            dependsOn player.Costume (fun model costume ->
                View.SKCanvasView(
                    invalidate = true,
                    paintSurface = (fun args ->
                        let info = args.Info
                        let surface = args.Surface
                        let canvas = surface.Canvas

                        canvas.Clear()

                        // see https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/curves/path-data
                        canvas.Translate(float32 info.Width / 2.f, float32 info.Height / 2.f)

                        let widthRatio = float32 info.Width / float32 costume.Size.Width
                        let heightRatio = float32 info.Height / float32 costume.Size.Height
                        canvas.Scale(System.Math.Min(widthRatio, heightRatio))

                        canvas.Translate(float32 costume.Size.Width / -2.f, float32 costume.Size.Height / -2.f)

                        costume.Paths
                        |> List.iter (fun p ->
                            let path = SKPath.ParseSvgPathData(p.Data)
                            use paint = new SKPaint(Style = SKPaintStyle.Fill, Color = skColor p.FillColor)
                            canvas.DrawPath(path, paint)
                        )
                    )
                )
            )

        let getPlayerInfoView (playerId, player) =
            let boxSize = { Width = 30.; Height = 30. }
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

        let speechBubble (player: PlayerData) content =
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
                    |> layoutFlags AbsoluteLayoutFlags.All
                    |> layoutBounds (Rectangle(0., 0., 1., 1.))

                    View.Frame(
                        widthRequest = 150.,
                        content = content,
                        padding = 0.,
                        margin = Thickness(10., 10., 10., 25.)
                    )
                ]
            )
            |> layoutBounds (Rectangle(player.Bounds.Right - model.SceneBounds.Left - 75., 1., AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize))
            |> layoutFlags AbsoluteLayoutFlags.YProportional

        let getFullPlayerView (playerId, player: PlayerData) =
            View.AbsoluteLayout(
                children = [
                    yield
                        View.ContentView(
                            widthRequest = player.Size.Width,
                            heightRequest = player.Size.Height,
                            content = getPlayerView player,
                            rotation = 360. - Degrees.value player.Direction
                        )
                        |> layoutBounds (Rectangle(player.Bounds.Left - model.SceneBounds.Left, model.SceneBounds.Top - player.Bounds.Top, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize))

                    match player.SpeechBubble with
                    | Some (Say text) ->
                        yield
                            View.Label(
                                text = text,
                                horizontalTextAlignment = TextAlignment.Center
                            )
                            |> speechBubble player
                    | Some (Ask data) ->
                        yield
                            View.StackLayout(
                                children = [
                                    View.Label(
                                        text = data.Question,
                                        horizontalTextAlignment = TextAlignment.Center
                                    )
                                    View.Entry(
                                        text = Option.defaultValue "" data.Answer,
                                        placeholder = "Answer",
                                        textChanged = (fun ev -> dispatch (UpdateAnswer (playerId, ev.NewTextValue))),
                                        completed = (fun text -> dispatch (ApplyAnswer playerId))
                                    )
                                ]
                            )
                            |> speechBubble player
                    | None -> ()
                ]
            )
            |> layoutFlags AbsoluteLayoutFlags.All
            |> layoutBounds (Rectangle(0., 0., 1., 1.))

        let getPenLineView penLine =
            dependsOn (penLine, model.SceneBounds) (fun model (penLine, sceneBounds) ->
                let dx = penLine.End.X - penLine.Start.X
                let dy = penLine.End.Y - penLine.Start.Y
                View.BoxView(
                    color = xfColor penLine.Color,
                    widthRequest = Math.Sqrt(dx * dx + dy * dy),
                    heightRequest = penLine.Weight,
                    translationX = penLine.Start.X - sceneBounds.Left,
                    translationY = sceneBounds.Top - penLine.Start.Y - penLine.Weight / 2.,
                    rotation = 360. - Math.Atan2(dy, dx) * 180. / Math.PI,
                    anchorX = 0.,
                    anchorY = 0.5
                )
            )

        let players = Map.toList model.Players

        View.ContentPage(
            title = "GetIt",
            content = View.StackLayout(
                spacing = 0.,
                children = [
                    View.AbsoluteLayout(
                        isClippedToBounds = true,
                        automationId = "scene",
                        verticalOptions = LayoutOptions.FillAndExpand,
                        children =
                            [
                                View.AbsoluteLayout(children = List.map getPenLineView model.PenLines)
                                |> layoutFlags AbsoluteLayoutFlags.All
                                |> layoutBounds (Rectangle(0., 0., 1., 1.))

                                View.AbsoluteLayout(children = List.map getFullPlayerView players)
                                |> layoutFlags AbsoluteLayoutFlags.All
                                |> layoutBounds (Rectangle(0., 0., 1., 1.))
                            ]
                    )
                    |> sizeChanged (fun e ->
                        let size = { Width = e.Width; Height = e.Height }
                        let bounds = { Position = { X = -size.Width / 2.; Y = -size.Height / 2. }; Size = size }
                        dispatch (SetSceneBounds bounds)
                    )
                    View.ScrollView(
                        verticalOptions = LayoutOptions.End,
                        orientation = ScrollOrientation.Horizontal,
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

    let clearScene () = dispatchMessage ClearScene
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
    let applyMouseClick mouseButton position = dispatchMessage (ApplyMouseClick (mouseButton, position))

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
