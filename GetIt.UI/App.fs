namespace GetIt

open System
open System.IO
open System.Reactive.Subjects
open System.Text
open System.Threading
open Fabulous.Core
open Fabulous.DynamicViews
open SkiaSharp
open Xamarin.Forms

module App =
    type PenLine =
        {
            Start: Position
            End: Position
            Weight: float
            Color: RGBAColor
        }

    type Msg =
        | SetSceneBounds of GetIt.Rectangle
        | SetMousePosition of positionRelativeToSceneControl: Position
        | ApplyMouseClick of MouseButton * positionRelativeToSceneControl: Position
        | SetPosition of PlayerId * Position
        | ChangePosition of PlayerId * Position
        | SetDirection of PlayerId * Degrees
        | ChangeDirection of PlayerId * Degrees
        | SetSpeechBubble of PlayerId * SpeechBubble option
        | UpdateAnswer of PlayerId * string
        | ApplyStringAnswer of PlayerId * string
        | ApplyBoolAnswer of PlayerId * bool
        | SetPenState of PlayerId * isOn: bool
        | TogglePenState of PlayerId
        | SetPenColor of PlayerId * RGBAColor
        | ShiftPenColor of PlayerId * Degrees
        | SetPenWeight of PlayerId * float
        | ChangePenWeight of PlayerId * float
        | SetSizeFactor of PlayerId * float
        | ChangeSizeFactor of PlayerId * float
        | SetVisibility of PlayerId * isVisible: bool
        | SetNextCostume of PlayerId
        | SendToBack of PlayerId
        | BringToFront of PlayerId
        | AddPlayer of PlayerId * PlayerData
        | RemovePlayer of PlayerId
        | ClearScene
        | SetBackground of SvgImage
        | StartBatch
        | ApplyBatch

    type Model =
        {
            SceneBounds: GetIt.Rectangle
            Players: Map<PlayerId, PlayerData>
            PlayerStringAnswers: Map<PlayerId, string>
            PenLines: PenLine list
            Background: SvgImage
            BatchMessages: (Msg list * int) option
        }

    let initModel =
        {
            SceneBounds = GetIt.Rectangle.zero
            Players = Map.empty
            PlayerStringAnswers = Map.empty
            PenLines = []
            Background = Background.none
            BatchMessages = None
        }

    let init () = (initModel, Cmd.none)

    let rec update (msgs: IObserver<_>) msg model =
        let updatePlayer playerId fn =
            let player = Map.find playerId model.Players |> fn
            { model with Players = Map.add playerId player model.Players }

        match model.BatchMessages, msg with
        | None, SetSceneBounds bounds ->
            let model' = { model with SceneBounds = bounds }
            (model', Cmd.none)
        | None, SetMousePosition positionRelativeToSceneControl ->
            (model, Cmd.none)
        | None, ApplyMouseClick (mouseButton, positionRelativeToSceneControl) ->
            (model, Cmd.none)
        | None, SetPosition (playerId, position) ->
            let model' =
                let player = Map.find playerId model.Players
                let player' = { player with Position = position }
                { model with
                    Players = Map.add playerId player' model.Players
                    PenLines =
                        if player.Pen.IsOn
                        then
                            let line =
                                {
                                    Start = player.Position
                                    End = player'.Position
                                    Weight = player.Pen.Weight
                                    Color = player.Pen.Color
                                }
                            model.PenLines @ [ line ]
                        else model.PenLines
                }
            (model', Cmd.none)
        | None, ChangePosition (playerId, relativePosition) ->
            let player = Map.find playerId model.Players
            update msgs (SetPosition (playerId, player.Position + relativePosition)) model
        | None, SetDirection (playerId, angle) ->
            let model' = updatePlayer playerId (fun p -> { p with Direction = angle })
            (model', Cmd.none)
        | None, ChangeDirection (playerId, relativeDirection) ->
            let player = Map.find playerId model.Players
            update msgs (SetDirection (playerId, player.Direction + relativeDirection)) model
        | None, SetSpeechBubble (playerId, speechBubble) ->
            let model' = updatePlayer playerId (fun p -> { p with SpeechBubble = speechBubble })
            (model', Cmd.none)
        | None, UpdateAnswer (playerId, answer) ->
            let model' =
                { model with PlayerStringAnswers = Map.add playerId answer model.PlayerStringAnswers }
            (model', Cmd.none)
        | None, ApplyStringAnswer (playerId, answer) ->
            let model' =
                updatePlayer playerId (fun p ->
                    match p.SpeechBubble with
                    | Some (AskString _) -> { p with SpeechBubble = None }
                    | Some (AskBool _)
                    | Some (Say _)
                    | None -> p
                )
                |> fun m -> { m with PlayerStringAnswers = Map.remove playerId model.PlayerStringAnswers }
            (model', Cmd.none)
        | None, ApplyBoolAnswer (playerId, answer) ->
            let model' =
                updatePlayer playerId (fun p ->
                    match p.SpeechBubble with
                    | Some (AskBool _) -> { p with SpeechBubble = None }
                    | Some (AskString _)
                    | Some (Say _)
                    | None -> p
                )
            (model', Cmd.none)
        | None, SetPenState (playerId, isOn) ->
            let model' = updatePlayer playerId (fun p -> { p with Pen = { p.Pen with IsOn = isOn } })
            (model', Cmd.none)
        | None, TogglePenState playerId ->
            let model' = updatePlayer playerId (fun p -> { p with Pen = { p.Pen with IsOn = not p.Pen.IsOn } })
            (model', Cmd.none)
        | None, SetPenColor (playerId, color) ->
            let model' = updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Color = color } })
            (model', Cmd.none)
        | None, ShiftPenColor (playerId, angle) ->
            let model' = updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Color = Color.hueShift angle p.Pen.Color } })
            (model', Cmd.none)
        | None, SetPenWeight (playerId, weight) ->
            let model' = updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Weight = weight } })
            (model', Cmd.none)
        | None, ChangePenWeight (playerId, weight) ->
            let model' = updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Weight = p.Pen.Weight + weight } })
            (model', Cmd.none)
        | None, SetSizeFactor (playerId, sizeFactor) ->
            let model' = updatePlayer playerId (fun p -> { p with SizeFactor = sizeFactor })
            (model', Cmd.none)
        | None, ChangeSizeFactor (playerId, sizeFactor) ->
            let model' = updatePlayer playerId (fun p -> { p with SizeFactor = p.SizeFactor + sizeFactor })
            (model', Cmd.none)
        | None, SetVisibility (playerId, isVisible) ->
            let model' = updatePlayer playerId (fun p -> { p with IsVisible = isVisible })
            (model', Cmd.none)
        | None, SetNextCostume playerId ->
            let model' = updatePlayer playerId Player.nextCostume
            (model', Cmd.none)
        | None, SendToBack playerId ->
            let model' = { model with Players = Player.sendToBack playerId model.Players }
            (model', Cmd.none)
        | None, BringToFront playerId ->
            let model' = { model with Players = Player.bringToFront playerId model.Players }
            (model', Cmd.none)
        | None, AddPlayer (playerId, player) ->
            let model' =
                { model with
                    Players =
                        Map.add playerId player model.Players
                        |> Player.sendToBack playerId
                }
            (model', Cmd.none)
        | None, RemovePlayer playerId ->
            let model' =
                { model with
                    Players = Map.remove playerId model.Players
                    PlayerStringAnswers = Map.remove playerId model.PlayerStringAnswers
                }
            (model', Cmd.none)
        | None, ClearScene ->
            let model' = { model with PenLines = [] }
            (model', Cmd.none)
        | None, SetBackground background ->
            let model' = { model with Background = background }
            (model', Cmd.none)
        | None, StartBatch ->
            let model' = { model with BatchMessages = Some ([], 1) }
            (model', Cmd.none)
        | Some (messages, level), StartBatch ->
            let model' = { model with BatchMessages = Some (messages, level + 1) }
            (model', Cmd.none)
        | None, ApplyBatch ->
            (model, Cmd.none) // TODO send error to controller?
        | Some (messages, level), ApplyBatch when level > 1 ->
            let model' = { model with BatchMessages = Some (messages, level - 1) }
            (model', Cmd.none)
        | Some (messages, level), ApplyBatch ->
            let updateAndMerge msg (model, cmd) =
                let (model', cmd') = update msgs msg model
                model', Cmd.batch [ cmd; cmd' ]
            (messages, ({ model with BatchMessages = None }, Cmd.none))
            ||> List.foldBack updateAndMerge
        | Some (messages, level), x ->
            let model' = { model with BatchMessages = Some (x :: messages, level) }
            model', Cmd.none
        |> fun (model, cmd) ->
            msgs.OnNext (msg, model)
            (model, cmd)

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

                        let svgPicture =
                            use stream = new MemoryStream(Encoding.UTF8.GetBytes costume.SvgData)
                            let svg = SkiaSharp.Extended.Svg.SKSvg()
                            svg.Load(stream)

                        canvas.DrawPicture(svgPicture)
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
                        opacity = (if player.IsVisible then 1. else 0.5),
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

                            let markerRealSize = 15.f
                            let borderRadius = 15.f
                            let borderWidth = 2.f

                            use path = new SKPath()
                            path.MoveTo(borderRadius, 0.f)
                            path.RArcTo(borderRadius, borderRadius, 0.f, SKPathArcSize.Small, SKPathDirection.CounterClockwise, -borderRadius, borderRadius)
                            path.LineTo(path.LastPoint.X, float32 info.Height - markerRealSize - borderRadius - 2.f * borderWidth)
                            path.RArcTo(borderRadius, borderRadius, 0.f, SKPathArcSize.Small, SKPathDirection.CounterClockwise, borderRadius, borderRadius)
                            path.LineTo((float32 info.Width - markerRealSize) / 2.f - borderWidth, path.LastPoint.Y)
                            path.RLineTo(0.f, markerRealSize)
                            path.RLineTo(markerRealSize, -markerRealSize)
                            path.LineTo(float32 info.Width - borderRadius - 2.f * borderWidth, path.LastPoint.Y)
                            path.RArcTo(borderRadius, borderRadius, 0.f, SKPathArcSize.Small, SKPathDirection.CounterClockwise, borderRadius, -borderRadius)
                            path.LineTo(path.LastPoint.X, borderRadius)
                            path.RArcTo(borderRadius, borderRadius, 0.f, SKPathArcSize.Small, SKPathDirection.CounterClockwise, -borderRadius, -borderRadius)
                            // path.LineTo(borderRadius, path.LastPoint.Y)
                            path.Close()
                            path.Offset(float32 borderWidth, float32 borderWidth)

                            do
                                use paint =
                                    new SKPaint(
                                        Style = SKPaintStyle.Stroke,
                                        StrokeCap = SKStrokeCap.Square,
                                        StrokeWidth = float32 borderWidth,
                                        Color = SKColors.Black,
                                        IsAntialias = true
                                    )
                                canvas.DrawPath(path, paint)
                            do
                                use paint =
                                    new SKPaint(
                                        Style = SKPaintStyle.Fill,
                                        Color = SKColors.WhiteSmoke,
                                        IsAntialias = true
                                    )
                                canvas.DrawPath(path, paint)
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
                    | Some (AskString question) ->
                        let answer =
                            Map.tryFind playerId model.PlayerStringAnswers
                            |> Option.defaultValue ""
                        yield
                            View.StackLayout(
                                children = [
                                    View.Label(
                                        text = question,
                                        horizontalTextAlignment = TextAlignment.Center
                                    )
                                    View.Entry(
                                        text = answer,
                                        placeholder = "Answer",
                                        textChanged = (fun ev -> dispatch (UpdateAnswer (playerId, ev.NewTextValue))),
                                        completed = (fun text -> dispatch (ApplyStringAnswer (playerId, answer)))
                                    )
                                ]
                            )
                            |> speechBubble player
                    | Some (AskBool question) ->
                        yield
                            View.StackLayout(
                                children = [
                                    View.Label(
                                        text = question,
                                        horizontalTextAlignment = TextAlignment.Center
                                    )
                                    View.FlexLayout(
                                        direction = FlexDirection.Row,
                                        // alignContent = FlexAlignContent.SpaceBetween,
                                        // alignItems = FlexAlignItems.Stretch,
                                        children =
                                            [
                                                View.Button(
                                                    text = "✓",
                                                    command = (fun () -> dispatch (ApplyBoolAnswer (playerId, true))),
                                                    borderWidth = 1.,
                                                    borderColor = Color.ForestGreen,
                                                    textColor = Color.ForestGreen,
                                                    backgroundColor = Color.WhiteSmoke
                                                )
                                                |> flexGrow 1.

                                                View.Button(
                                                    text = "❌",
                                                    command = (fun () -> dispatch (ApplyBoolAnswer (playerId, false))),
                                                    borderWidth = 1.,
                                                    borderColor = Color.IndianRed,
                                                    textColor = Color.IndianRed,
                                                    backgroundColor = Color.WhiteSmoke,
                                                    margin = Thickness(5., 0., 0., 0.)
                                                )
                                                |> flexGrow 1.
                                            ]
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

        let backgroundView =
            dependsOn model.Background (fun model background ->
                View.SKCanvasView(
                    invalidate = true,
                    paintSurface = (fun args ->
                        let info = args.Info
                        let surface = args.Surface
                        let canvas = surface.Canvas

                        canvas.Clear()

                        let widthRatio = float32 info.Width / float32 background.Size.Width
                        let heightRatio = float32 info.Height / float32 background.Size.Height
                        canvas.Scale(System.Math.Max(widthRatio, heightRatio))

                        let svgPicture =
                            use stream = new MemoryStream(Encoding.UTF8.GetBytes background.SvgData)
                            let svg = SkiaSharp.Extended.Svg.SKSvg()
                            svg.Load(stream)

                        canvas.DrawPicture(svgPicture)
                    )
                )
            )

        let players =
            model.Players
            |> Map.toList
            |> List.sortBy (snd >> fun p -> p.Layer)

        let playersOnScene =
            players
            |> List.filter (snd >> fun p -> p.IsVisible)
            |> List.rev

        View.ContentPage(
            content = View.StackLayout(
                spacing = 0.,
                children = [
                    View.AbsoluteLayout(
                        automationId = "scene",
                        verticalOptions = LayoutOptions.FillAndExpand,
                        children =
                            [
                                backgroundView
                                |> layoutFlags AbsoluteLayoutFlags.All
                                |> layoutBounds (Rectangle(0., 0., 1., 1.))

                                View.AbsoluteLayout(children = List.map getPenLineView model.PenLines)
                                |> layoutFlags AbsoluteLayoutFlags.All
                                |> layoutBounds (Rectangle(0., 0., 1., 1.))

                                View.AbsoluteLayout(children = List.map getFullPlayerView playersOnScene)
                                |> layoutFlags AbsoluteLayoutFlags.All
                                |> layoutBounds (Rectangle(0., 0., 1., 1.))
                            ]
                    )
                    |> sizeChanged (fun e ->
                        let size = { Width = e.Width; Height = e.Height }
                        let bounds = { Position = { X = -size.Width / 2.; Y = -size.Height / 2. }; Size = size }
                        if bounds <> Rectangle.zero then
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
            ),
            created = (fun e -> NavigationPage.SetHasNavigationBar(e, false))
        )

    let subscription msgs =
        Cmd.ofSub (fun dispatch ->
            let d = msgs |> Observable.subscribe dispatch
            ()
        )

    // Note, this declaration is needed if you enable LiveUpdate
    let program msgs =
        Program.mkProgram init (update msgs) view
        |> Program.withSubscription (fun _ -> subscription msgs)

type App (msgs: ISubject<_, _>) as app = 
    inherit Application ()

    let runner = 
        App.program msgs
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
