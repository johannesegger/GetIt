namespace GetIt

open System
open System.IO
open System.Text
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
          Color: RGBAColor }

    type Msg =
        | SetSceneBounds of GetIt.Rectangle
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
        | SetBackground of SvgImage
        | StartBatch
        | ApplyBatch

    type Model =
        { SceneBounds: GetIt.Rectangle
          Players: Map<PlayerId, PlayerData>
          PlayerOrder: PlayerId list
          PenLines: PenLine list
          Background: SvgImage
          BatchMessages: (Msg list * int) option }

    let initModel =
        { SceneBounds = GetIt.Rectangle.zero
          Players = Map.empty
          PlayerOrder = []
          PenLines = []
          Background = Background.none
          BatchMessages = None }

    let init () = (initModel, Cmd.none)

    let rec update triggerEvent msg model =
        let updatePlayer playerId fn =
            let player = Map.find playerId model.Players |> fn
            { model with Players = Map.add playerId player model.Players }

        let triggerEventCmd event =
            Cmd.ofAsyncMsgOption (async { triggerEvent event; return None })

        match model.BatchMessages, msg with
        | None, SetSceneBounds bounds ->
            let model' = { model with SceneBounds = bounds }
            let cmd = triggerEventCmd (UIEvent.SetSceneBounds bounds)
            (model', cmd)
        | None, SetMousePosition positionRelativeToSceneControl ->
            let position =
                { X = model.SceneBounds.Left + positionRelativeToSceneControl.X
                  Y = model.SceneBounds.Top - positionRelativeToSceneControl.Y }
            let cmd = triggerEventCmd (UIEvent.SetMousePosition position)
            (model, cmd)
        | None, ApplyMouseClick (mouseButton, positionRelativeToSceneControl) ->
            let position =
                { X = model.SceneBounds.Left + positionRelativeToSceneControl.X
                  Y = model.SceneBounds.Top - positionRelativeToSceneControl.Y }
            let cmd = triggerEventCmd (UIEvent.ApplyMouseClick (mouseButton, position))
            (model, cmd)
        | None, SetPlayerPosition (playerId, position) ->
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
        | None, SetPlayerDirection (playerId, angle) ->
            let model' = updatePlayer playerId (fun p -> { p with Direction = angle })
            (model', Cmd.none)
        | None, SetSpeechBubble (playerId, speechBubble) ->
            let model' = updatePlayer playerId (fun p -> { p with SpeechBubble = speechBubble })
            (model', Cmd.none)
        | None, UpdateAnswer (playerId, answer) ->
            let model' = updatePlayer playerId (fun p ->
                match p.SpeechBubble with
                | Some (Ask askData) -> { p with SpeechBubble = Some (Ask { askData with Answer = Some answer }) }
                | Some (Say _)
                | None -> p
            )
            (model', Cmd.none)
        | None, ApplyAnswer playerId ->
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
        | None, SetPen (playerId, pen) ->
            let model' = updatePlayer playerId (fun p -> { p with Pen = pen })
            (model', Cmd.none)
        | None, SetSizeFactor (playerId, sizeFactor) ->
            let model' = updatePlayer playerId (fun p -> { p with SizeFactor = sizeFactor })
            (model', Cmd.none)
        | None, NextCostume playerId ->
            let model' = updatePlayer playerId Player.nextCostume
            (model', Cmd.none)
        | None, AddPlayer (playerId, player) ->
            let model' =
                { model with
                    Players = Map.add playerId player model.Players
                    PlayerOrder = model.PlayerOrder @ [ playerId ]
                }
            (model', Cmd.none)
        | None, RemovePlayer playerId ->
            let model' =
                { model with
                    Players = Map.remove playerId model.Players
                    PlayerOrder = model.PlayerOrder |> List.filter ((<>) playerId)
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
                let (model', cmd') = update triggerEvent msg model
                model', Cmd.batch [ cmd; cmd' ]
            (messages, ({ model with BatchMessages = None }, Cmd.none))
            ||> List.foldBack updateAndMerge
        | Some (messages, level), x ->
            let model' = { model with BatchMessages = Some (x :: messages, level) }
            model', Cmd.none

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

                            let markerRealSize = 15.
                            let borderRadius = 15.
                            let borderWidth = 2.

                            canvas.Translate(SKPoint(float32 borderWidth, float32 borderWidth))

                            let path =
                                [
                                    sprintf "M%f,0" borderRadius
                                    sprintf "a%f,%f 0 0,0 %f,%f" borderRadius borderRadius -borderRadius borderRadius
                                    sprintf "V%f" (float info.Height - markerRealSize - borderRadius - 2. * borderWidth)
                                    sprintf "a%f,%f 0 0,0 %f,%f" borderRadius borderRadius borderRadius borderRadius
                                    sprintf "H%f" ((float info.Width - markerRealSize) / 2.)
                                    sprintf "v%f" markerRealSize
                                    sprintf "l%f,%f" markerRealSize -markerRealSize
                                    sprintf "H%f" (float info.Width - borderRadius - 2. * borderWidth)
                                    sprintf "a%f,%f 0 0,0 %f,%f" borderRadius borderRadius borderRadius -borderRadius
                                    sprintf "V%f" borderRadius
                                    sprintf "a%f,%f 0 0,0 %f,%f" borderRadius borderRadius -borderRadius -borderRadius
                                    sprintf "H%f" borderRadius
                                ]
                                |> String.concat " "
                                |> SKPath.ParseSvgPathData
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
            model.PlayerOrder
            |> List.map (fun playerId -> playerId, Map.find playerId model.Players)
        View.NavigationPage(
            pages = [
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
                |> hasNavigationBar false
            ])

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
