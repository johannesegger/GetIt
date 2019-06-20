module GetIt.UI

open Browser.Types
open Elmish
open Elmish.Debug
open Elmish.React
open Elmish.Streams
open Elmish.HMR // Must be last Elmish.* open declaration (see https://elmish.github.io/hmr/#Usage)
open Fable.Core
open Fable.Core.JsInterop
open Fable.Elmish.Nile
open Fable.React
open Fable.React.Props
open FSharp.Control
open Thoth.Json

importAll "../sass/main.sass"

type PenLine =
    {
        Start: Position
        End: Position
        Weight: float
        Color: RGBAColor
    }

type Model =
    {
        SceneBounds: GetIt.Rectangle
        WindowTitle: string option
        Players: Map<PlayerId, PlayerData>
        PlayerStringAnswers: Map<PlayerId, string>
        PenLines: PenLine list
        Background: SvgImage
        BatchMessages: (ChannelMsg list * int) option
    }

let init () =
    {
        SceneBounds = GetIt.Rectangle.zero
        WindowTitle = None
        Players = Map.empty
        PlayerStringAnswers = Map.empty
        PenLines = []
        Background = Background.none
        BatchMessages = None
    }

let rec update msg model =
    let updatePlayer playerId fn =
        let player = Map.find playerId model.Players |> fn
        { model with Players = Map.add playerId player model.Players }

    match model.BatchMessages, msg with
    | None, UIMsg (SetSceneBounds bounds) ->
        { model with SceneBounds = bounds }
    | None, UIMsg (SetMousePosition _) ->
        model
    | None, UIMsg (ApplyMouseClick _) ->
        model
    | None, UIMsg (UpdateStringAnswer (playerId, answer)) ->
        { model with PlayerStringAnswers = Map.add playerId answer model.PlayerStringAnswers }
    | None, UIMsg (AnswerStringQuestion (playerId, answer)) ->
        let model' =
            updatePlayer playerId (fun p ->
                match p.SpeechBubble with
                | Some (AskString _) -> { p with SpeechBubble = None }
                | Some (AskBool _)
                | Some (Say _)
                | None -> p
            )
        { model' with PlayerStringAnswers = Map.remove playerId model.PlayerStringAnswers }
    | None, UIMsg (AnswerBoolQuestion (playerId, answer)) ->
        updatePlayer playerId (fun p ->
            match p.SpeechBubble with
            | Some (AskBool _) -> { p with SpeechBubble = None }
            | Some (AskString _)
            | Some (Say _)
            | None -> p
        )
    | None, UIMsg (Screenshot _) ->
        model
    | None, ControllerMsg (SetPosition (playerId, position)) ->
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
    | None, ControllerMsg (ChangePosition (playerId, relativePosition)) ->
        let player = Map.find playerId model.Players
        update (ControllerMsg (SetPosition (playerId, player.Position + relativePosition))) model
    | None, ControllerMsg (SetDirection (playerId, angle)) ->
        updatePlayer playerId (fun p -> { p with Direction = angle })
    | None, ControllerMsg (ChangeDirection (playerId, relativeDirection)) ->
        let player = Map.find playerId model.Players
        update (ControllerMsg (SetDirection (playerId, player.Direction + relativeDirection))) model
    | None, ControllerMsg (SetSpeechBubble (playerId, speechBubble)) ->
        updatePlayer playerId (fun p -> { p with SpeechBubble = speechBubble })
    | None, ControllerMsg (SetPenState (playerId, isOn)) ->
        updatePlayer playerId (fun p -> { p with Pen = { p.Pen with IsOn = isOn } })
    | None, ControllerMsg (TogglePenState playerId) ->
        updatePlayer playerId (fun p -> { p with Pen = { p.Pen with IsOn = not p.Pen.IsOn } })
    | None, ControllerMsg (SetPenColor (playerId, color)) ->
        updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Color = color } })
    | None, ControllerMsg (ShiftPenColor (playerId, angle)) ->
        updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Color = Color.hueShift angle p.Pen.Color } })
    | None, ControllerMsg (SetPenWeight (playerId, weight)) ->
        updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Weight = weight } })
    | None, ControllerMsg (ChangePenWeight (playerId, weight)) ->
        updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Weight = p.Pen.Weight + weight } })
    | None, ControllerMsg (SetSizeFactor (playerId, sizeFactor)) ->
        updatePlayer playerId (fun p -> { p with SizeFactor = sizeFactor })
    | None, ControllerMsg (ChangeSizeFactor (playerId, sizeFactor)) ->
        updatePlayer playerId (fun p -> { p with SizeFactor = p.SizeFactor + sizeFactor })
    | None, ControllerMsg (SetVisibility (playerId, isVisible)) ->
        updatePlayer playerId (fun p -> { p with IsVisible = isVisible })
    | None, ControllerMsg (ToggleVisibility playerId) ->
        updatePlayer playerId (fun p -> { p with IsVisible = not p.IsVisible })
    | None, ControllerMsg (SetNextCostume playerId) ->
        updatePlayer playerId Player.nextCostume
    | None, ControllerMsg (SendToBack playerId) ->
        { model with Players = Player.sendToBack playerId model.Players }
    | None, ControllerMsg (BringToFront playerId) ->
        { model with Players = Player.bringToFront playerId model.Players }
    | None, ControllerMsg (AddPlayer (playerId, player)) ->
        { model with
            Players =
                Map.add playerId player model.Players
                |> Player.sendToBack playerId
        }
    | None, ControllerMsg (RemovePlayer playerId) ->
        { model with
            Players = Map.remove playerId model.Players
            PlayerStringAnswers = Map.remove playerId model.PlayerStringAnswers
        }
    | None, ControllerMsg (SetWindowTitle title) ->
        { model with WindowTitle = title }
    | None, ControllerMsg ClearScene ->
        { model with PenLines = [] }
    | None, ControllerMsg MakeScreenshot ->
        model
    | None, ControllerMsg (SetBackground background) ->
        { model with Background = background }
    | None, ControllerMsg (InputEvent (KeyDown key)) ->
        model
    | None, ControllerMsg (InputEvent (KeyUp key)) ->
        model
    | None, ControllerMsg (InputEvent (MouseMove virtualScreenPosition)) ->
        model
    | None, ControllerMsg (InputEvent (MouseClick virtualScreenMouseClick)) ->
        model
    | None, ControllerMsg StartBatch ->
        { model with BatchMessages = Some ([], 1) }
    | Some (messages, level), ControllerMsg StartBatch ->
        { model with BatchMessages = Some (messages, level + 1) }
    | None, ControllerMsg ApplyBatch ->
        model // TODO send error to controller?
    | Some (messages, level), ControllerMsg ApplyBatch when level > 1 ->
        { model with BatchMessages = Some (messages, level - 1) }
    | Some (messages, level), ControllerMsg ApplyBatch ->
        (messages, ({ model with BatchMessages = None }))
        ||> List.foldBack update
    | Some (messages, level), x ->
        { model with BatchMessages = Some (x :: messages, level) }

let canvasSize size =
    { Canvas.Width = size.Width; Canvas.Height = size.Height }

let view model dispatch =
    let players =
        model.Players
        |> Map.toList
        |> List.sortBy (snd >> fun p -> p.Layer)
    let playersOnScene =
        players
        |> List.filter (snd >> fun p -> p.IsVisible)
        |> List.rev

    let drawScenePlayers =
        playersOnScene
        |> List.map (fun (PlayerId playerId, player) ->
            Canvas.Batch [
                Canvas.Save
                Canvas.Translate (-model.SceneBounds.Left + player.Position.X, model.SceneBounds.Top - player.Position.Y)
                Canvas.Rotate (2. * System.Math.PI - Degrees.toRadians player.Direction)
                Canvas.Scale (player.SizeFactor, player.SizeFactor)
                Canvas.DrawLoadedImage ((sprintf "#player-%O" playerId), (-player.Size.Width / 2., -player.Size.Height / 2.), (player.Size.Width, player.Size.Height))
                Canvas.Restore
            ]
        )
        |> Canvas.Batch

    let drawPenLines =
        model.PenLines
        |> List.collect (fun penLine ->
            [
                Canvas.BeginPath
                Canvas.StrokeStyle (U3.Case1 <| RGBAColor.rgbaHexNotation penLine.Color)
                Canvas.MoveTo (penLine.Start.X - model.SceneBounds.Left, model.SceneBounds.Top - penLine.Start.Y)
                Canvas.LineTo (penLine.End.X - model.SceneBounds.Left, model.SceneBounds.Top - penLine.End.Y)
                Canvas.Stroke
            ]
        )
        |> Canvas.Batch

    div [ Id "main" ] [
        canvasSize model.SceneBounds.Size
        |> Canvas.initialize
        |> Canvas.withId "scene"
        |> Canvas.draw (Canvas.ClearReact (0., 0., model.SceneBounds.Size.Width, model.SceneBounds.Size.Height))
        |> Canvas.draw drawPenLines
        |> Canvas.draw drawScenePlayers
        |> Canvas.render

        div [ Id "info" ] [
            yield!
                players
                |> List.map (fun (PlayerId playerId, player) ->
                    let size = { Canvas.Width = 30.; Canvas.Height = 30. }
                    let ratio = System.Math.Min(size.Width / player.Size.Width, size.Height / player.Size.Height)
                    div [ Class "player" ] [
                        Canvas.initialize size
                        |> Canvas.draw (Canvas.ClearReact (0., 0., size.Width, size.Height))
                        |> Canvas.draw (
                            Canvas.Batch [
                                Canvas.Save
                                Canvas.Translate (size.Width / 2., size.Height / 2.)
                                Canvas.Rotate (2. * System.Math.PI - Degrees.toRadians player.Direction)
                                Canvas.Scale (ratio, ratio)
                                Canvas.DrawLoadedImage ((sprintf "#player-%O" playerId), (-player.Size.Width / 2., -player.Size.Height / 2.), (player.Size.Width, player.Size.Height))
                                Canvas.Restore
                            ]
                        )
                        |> Canvas.render

                        span [] [
                            str (sprintf "X: %.2f | Y: %.2f | ∠ %.2f°" player.Position.X player.Position.Y (Degrees.value player.Direction))
                        ]
                    ]
                )
        ]

        div [ Style [ Display DisplayOptions.None ] ] [
            yield!
                players
                |> List.map (fun (PlayerId playerId, player) ->
                    img [
                        Id (sprintf "player-%O" playerId)
                        player.Costume.SvgData
                        |> Browser.Dom.window.btoa
                        |> sprintf "data:image/svg+xml;base64,%s"
                        |> Src
                    ]
                )
        ]
    ]

let stream states msgs =
    [
        msgs

        states
        |> AsyncRx.map (fun model -> model.WindowTitle)
        |> AsyncRx.distinctUntilChanged
        |> AsyncRx.tapOnNext (fun title ->
            Browser.Dom.document.title <-
                match title with
                | Some text -> sprintf "Get It - %s" text
                | None -> "Get It"
        )
        |> AsyncRx.flatMapLatest (ignore >> AsyncRx.empty)

        [
            Browser.Dom.document.querySelector "#elmish-app"
            |> AsyncRx.observeSubTreeAdditions
            |> AsyncRx.choose (fun (n: Node) ->
                if n.nodeType = n.ELEMENT_NODE then Some (n :?> HTMLElement) else None
            )
            |> AsyncRx.startWith [ Browser.Dom.document.body ]
            |> AsyncRx.choose (fun n -> n.querySelector("#scene") :?> HTMLElement |> Option.ofObj)
            // |> AsyncRx.take 1
            |> AsyncRx.map (fun n -> n.offsetWidth, n.offsetHeight)
            |> AsyncRx.merge AsyncRx.observeSceneSizeFromWindowResize
            |> AsyncRx.map(fun (width, height) ->
                {
                    Position = { X = -width / 2.; Y = -height / 2. }
                    Size = { Width = width; Height = height }
                }
                |> SetSceneBounds
            )
        ]
        |> AsyncRx.mergeSeq
        |> AsyncRx.map UIMsg
        |> AsyncRx.msgChannel (sprintf "ws://%s%s" Browser.Dom.window.location.host MessageChannel.endpoint) (Encode.channelMsg >> Encode.toString 0) (Decode.fromString Decode.channelMsg >> Result.toOption)
        |> AsyncRx.requestAnimationFrame
    ]
    |> AsyncRx.mergeSeq

Program.mkSimple init update view
|> Program.withStream stream
#if DEBUG
|> Program.withDebugger
|> Program.withConsoleTrace
#endif
|> Program.withReactBatched "elmish-app"
|> Program.run
