namespace GetIt

open System.Diagnostics
open Fabulous.Core
open Fabulous.DynamicViews
open SkiaSharp
open SkiaSharp.Views.Forms
open Xamarin.Forms

module App = 
    type Model =
        { SceneBounds: GetIt.Rectangle
          Players: Map<PlayerId, Player>
          PenLines: PenLine list
          MouseState: MouseState
          KeyboardState: KeyboardState
          EventHandlers: EventHandler list }

    type Msg = 
        | SetSceneSize of GetIt.Size
        | SetKeyboardKeyPressed of KeyboardKey
        | SetKeyboardKeyReleased of KeyboardKey
        | SetMousePosition of Position
        | SetPlayerPosition of PlayerId * Position
        | SetPlayerDirection of PlayerId * Degrees
        | SetSpeechBubble of PlayerId * SpeechBubble option
        | UpdateAnswer of PlayerId * answer: string
        | ApplyAnswer of PlayerId
        | SetPen of PlayerId * Pen
        | SetSizeFactor of PlayerId * sizeFactor: float
        | NextCostume of PlayerId
        | AddPlayer of Player
        | RemovePlayer of PlayerId
        | ClearScene
        | AddEventHandler of EventHandler
        | RemoveEventHandler of EventHandler
        | TriggerEvent of Event
        | ExecuteAction of (unit -> unit)

    let initModel =
        { SceneBounds = { Position = { X = -300.; Y = -200. }; Size = { Width = 600.; Height = 400. } }
          Players =
            [
                (
                    PlayerId (System.Guid.NewGuid()),
                    // Player.createWithCostumes [ Costume.createCircle RGBAColor.forestGreen 10. ]
                    // { Player.turtle with Direction = Degrees 45. }
                    // { Player.turtle with SizeFactor = 2. }
                    // { Player.turtle with Position = { X = 200.; Y = -200. } }
                    { Player.turtle with SpeechBubble = Some (Say { Text = "Hello,\r\nnice to meet you" }) }
                )
            ]
            |> Map.ofList
          PenLines = []
          MouseState = MouseState.empty
          KeyboardState = KeyboardState.empty
          EventHandlers = [] }

    let init () = (initModel, Cmd.none)

    let update msg model =
        match msg with
        | SetSceneSize size ->
            let bounds = { Position = { X = -size.Width / 2.; Y = -size.Height / 2. }; Size = size }
            let model' = { model with SceneBounds = bounds }
            (model', Cmd.none)
        | SetKeyboardKeyPressed key ->
            (model, Cmd.none)
        | SetKeyboardKeyReleased key ->
            (model, Cmd.none)
        | SetMousePosition position ->
            (model, Cmd.none)
        | SetPlayerPosition (playerId, position) ->
            (model, Cmd.none)
        | SetPlayerDirection (playerId, angle) ->
            (model, Cmd.none)
        | SetSpeechBubble (playerId, speechBubble) ->
            (model, Cmd.none)
        | UpdateAnswer (playerId, answer) ->
            (model, Cmd.none)
        | ApplyAnswer playerId ->
            (model, Cmd.none)
        | SetPen (playerId, pen) ->
            (model, Cmd.none)
        | SetSizeFactor (playerId, sizeFactor) ->
            (model, Cmd.none)
        | NextCostume playerId ->
            (model, Cmd.none)
        | AddPlayer player ->
            (model, Cmd.none)
        | RemovePlayer playerId ->
            (model, Cmd.none)
        | ClearScene ->
            (model, Cmd.none)
        | AddEventHandler eventHandler ->
            (model, Cmd.none)
        | RemoveEventHandler eventHandler ->
            (model, Cmd.none)
        | TriggerEvent event ->
            (model, Cmd.none)
        | ExecuteAction action ->
            (model, Cmd.none)

    [<System.Diagnostics.CodeAnalysis.SuppressMessage("Formatting", "TupleCommaSpacing") >]
    let view (model: Model) dispatch =
        let skColor color = SKColor(color.Red, color.Green, color.Blue, color.Alpha)

        let getPlayerView (player: Player) =
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

        let getFullPlayerView (playerId, player: Player) =
            View.AbsoluteLayout(
                backgroundColor = Color.DarkKhaki,
                children = [
                    yield
                        View.ContentView(
                            widthRequest = player.Size.Width,
                            heightRequest = player.Size.Height,
                            content = getPlayerView player,
                            rotation = 360. - Degrees.value player.Direction
                        )
                        |> layoutFlags AbsoluteLayoutFlags.PositionProportional
                        |> layoutBounds (Rectangle(0.5, 0.5, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize))
                        // |> layoutBounds (Rectangle(player.Bounds.Left - model.SceneBounds.Left, model.SceneBounds.Top - player.Bounds.Top, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize))

                    match player.SpeechBubble with
                    | Some (Say data) ->
                        yield
                            View.AbsoluteLayout(
                                translationX = 20.,
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
                                            text = data.Text
                                        ),
                                        padding = 0.,
                                        margin = Thickness(10., 10., 10., 25.)
                                    )
                                ]
                            )
                            |> layoutFlags AbsoluteLayoutFlags.PositionProportional
                            |> layoutBounds (Rectangle(0.5, 1., AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize))
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
                        verticalOptions = LayoutOptions.FillAndExpand,
                        children = List.map getFullPlayerView players)
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

    // Note, this declaration is needed if you enable LiveUpdate
    let program = Program.mkProgram init update view

type App () as app = 
    inherit Application ()

    let runner = 
        App.program
#if DEBUG
        |> Program.withConsoleTrace
#endif
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


