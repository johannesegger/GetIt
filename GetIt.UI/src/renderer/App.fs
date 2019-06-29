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

importAll "../../sass/main.sass"

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
    | None, ControllerMsg (msgId, SetPosition (playerId, position)) ->
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
    | None, ControllerMsg (msgId, ChangePosition (playerId, relativePosition)) ->
        let player = Map.find playerId model.Players
        update (ControllerMsg (System.Guid.NewGuid(), SetPosition (playerId, player.Position + relativePosition))) model
    | None, ControllerMsg (msgId, SetDirection (playerId, angle)) ->
        updatePlayer playerId (fun p -> { p with Direction = angle })
    | None, ControllerMsg (msgId, ChangeDirection (playerId, relativeDirection)) ->
        let player = Map.find playerId model.Players
        update (ControllerMsg (System.Guid.NewGuid(), SetDirection (playerId, player.Direction + relativeDirection))) model
    | None, ControllerMsg (msgId, SetSpeechBubble (playerId, speechBubble)) ->
        updatePlayer playerId (fun p -> { p with SpeechBubble = speechBubble })
    | None, ControllerMsg (msgId, SetPenState (playerId, isOn)) ->
        updatePlayer playerId (fun p -> { p with Pen = { p.Pen with IsOn = isOn } })
    | None, ControllerMsg (msgId, TogglePenState playerId) ->
        updatePlayer playerId (fun p -> { p with Pen = { p.Pen with IsOn = not p.Pen.IsOn } })
    | None, ControllerMsg (msgId, SetPenColor (playerId, color)) ->
        updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Color = color } })
    | None, ControllerMsg (msgId, ShiftPenColor (playerId, angle)) ->
        updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Color = Color.hueShift angle p.Pen.Color } })
    | None, ControllerMsg (msgId, SetPenWeight (playerId, weight)) ->
        updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Weight = weight } })
    | None, ControllerMsg (msgId, ChangePenWeight (playerId, weight)) ->
        updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Weight = p.Pen.Weight + weight } })
    | None, ControllerMsg (msgId, SetSizeFactor (playerId, sizeFactor)) ->
        updatePlayer playerId (fun p -> { p with SizeFactor = sizeFactor })
    | None, ControllerMsg (msgId, ChangeSizeFactor (playerId, sizeFactor)) ->
        updatePlayer playerId (fun p -> { p with SizeFactor = p.SizeFactor + sizeFactor })
    | None, ControllerMsg (msgId, SetVisibility (playerId, isVisible)) ->
        updatePlayer playerId (fun p -> { p with IsVisible = isVisible })
    | None, ControllerMsg (msgId, ToggleVisibility playerId) ->
        updatePlayer playerId (fun p -> { p with IsVisible = not p.IsVisible })
    | None, ControllerMsg (msgId, SetNextCostume playerId) ->
        updatePlayer playerId Player.nextCostume
    | None, ControllerMsg (msgId, SendToBack playerId) ->
        { model with Players = Player.sendToBack playerId model.Players }
    | None, ControllerMsg (msgId, BringToFront playerId) ->
        { model with Players = Player.bringToFront playerId model.Players }
    | None, ControllerMsg (msgId, AddPlayer (playerId, player)) ->
        { model with
            Players =
                Map.add playerId player model.Players
                |> Player.sendToBack playerId
        }
    | None, ControllerMsg (msgId, RemovePlayer playerId) ->
        { model with
            Players = Map.remove playerId model.Players
            PlayerStringAnswers = Map.remove playerId model.PlayerStringAnswers
        }
    | None, ControllerMsg (msgId, SetWindowTitle title) ->
        { model with WindowTitle = title }
    | None, ControllerMsg (msgId, ClearScene) ->
        { model with PenLines = [] }
    | None, ControllerMsg (msgId, SetBackground background) ->
        { model with Background = background }
    | None, ControllerMsg (msgId, InputEvent (KeyDown key)) ->
        model
    | None, ControllerMsg (msgId, InputEvent (KeyUp key)) ->
        model
    | None, ControllerMsg (msgId, InputEvent (MouseMove virtualScreenPosition)) ->
        model
    | None, ControllerMsg (msgId, InputEvent (MouseClick virtualScreenMouseClick)) ->
        model
    | None, ControllerMsg (msgId, StartBatch) ->
        { model with BatchMessages = Some ([], 1) }
    | Some (messages, level), ControllerMsg (msgId, StartBatch) ->
        { model with BatchMessages = Some (messages, level + 1) }
    | None, ControllerMsg (msgId, ApplyBatch) ->
        eprintfn "Can't apply batch because no batch is running"
        model
    | Some (messages, level), ControllerMsg (msgId, ApplyBatch) when level > 1 ->
        { model with BatchMessages = Some (messages, level - 1) }
    | Some (messages, level), ControllerMsg (msgId, ApplyBatch) ->
        (messages, ({ model with BatchMessages = None }))
        ||> List.foldBack update
    | Some (messages, level), x ->
        { model with BatchMessages = Some (x :: messages, level) }

let view model dispatch =
    let players =
        model.Players
        |> Map.toList
        |> List.sortBy (snd >> fun p -> p.Layer)
    let playersOnScene =
        players
        |> List.filter (snd >> fun p -> p.IsVisible)
        |> List.rev

    let drawBackground =
        let ratio = System.Math.Max(model.SceneBounds.Size.Width / model.Background.Size.Width, model.SceneBounds.Size.Height / model.Background.Size.Height)
        let backgroundWidth = ratio * model.Background.Size.Width
        let backgroundHeight = ratio * model.Background.Size.Height
        g [
            [
                sprintf "translate(%f %f)" ((model.SceneBounds.Size.Width - backgroundWidth) / 2.) ((model.SceneBounds.Size.Height - backgroundHeight) / 2.)
                sprintf "scale(%f %f)" ratio ratio
            ]
            |> String.concat " "
            |> SVGAttr.Transform
            DangerouslySetInnerHTML { __html = model.Background.SvgData }
        ] []

    let drawPlayerOnScene (player: PlayerData) =
        let left = -model.SceneBounds.Left + player.Bounds.Left
        let top = model.SceneBounds.Top - player.Bounds.Top
        g
            [
                [
                    sprintf "translate(%f %f)" left top
                    sprintf "rotate(%f %f %f)" (360. - Degrees.value player.Direction) (player.Size.Width / 2.) (player.Size.Height / 2.)
                    sprintf "scale(%f %f)" player.SizeFactor player.SizeFactor
                ]
                |> String.concat " "
                |> SVGAttr.Transform
                DangerouslySetInnerHTML { __html = player.Costume.SvgData }
            ]
            []

    let drawSpeechBubble playerId (player: PlayerData) =
        let speechBubble content =
            svgEl "foreignObject"
                [
                    X (-model.SceneBounds.Left + player.Bounds.Right - 30.)
                    Y (model.SceneBounds.Top - player.Bounds.Top - 20.)
                    SVGAttr.Width "1"
                    SVGAttr.Height "1"
                    Style [ Overflow "visible" ]
                ]
                [
                    div [ Class "speech-bubble" ] content
                ]
            |> Some

        match player.SpeechBubble with
        | None -> None
        | Some (Say text) ->
            speechBubble [ span [] [ str text ] ]
        | Some (AskString text) ->
            speechBubble [
                span [] [ str text ]
                input [
                    OnChange (fun ev -> dispatch (UpdateStringAnswer (playerId, ev.Value)))
                    OnKeyPress (fun ev -> if ev.charCode = 13. then dispatch (AnswerStringQuestion (playerId, ev.Value)))
                    Value (model.PlayerStringAnswers |> Map.tryFind playerId |> Option.defaultValue "")
                    Style [ Width "100%"; MarginTop "5px" ]
                ]
            ]
        | Some (AskBool text) ->
            speechBubble [
                span [] [ str text ]
                div [ Class "askbool-answers" ] [
                    button [ OnClick (fun ev -> dispatch (AnswerBoolQuestion (playerId, true))) ] [ str "✔️" ]
                    button [ OnClick (fun ev -> dispatch (AnswerBoolQuestion (playerId, false))) ] [ str "❌" ]
                ]
            ]

    let drawScenePlayers =
        playersOnScene
        |> List.map (fun (playerId, player) ->
            g [] [
                yield drawPlayerOnScene player
                yield! drawSpeechBubble playerId player |> Option.toList
            ]
        )

    let drawPenLines =
        g [] [
            yield!
                model.PenLines
                |> List.map (fun penLine ->
                    line [
                            X1 (penLine.Start.X - model.SceneBounds.Left)
                            Y1 (model.SceneBounds.Top - penLine.Start.Y)
                            X2 (penLine.End.X - model.SceneBounds.Left)
                            Y2 (model.SceneBounds.Top - penLine.End.Y)
                            Style [
                                Stroke (RGBAColor.rgbaHexNotation penLine.Color)
                                StrokeWidth penLine.Weight
                            ]
                        ]
                        []
                )
        ]

    div [ Id "main" ] [
        svg [ Id "scene" ] [
            yield drawBackground
            yield drawPenLines
            yield! drawScenePlayers
        ]

        div [ Id "info" ] [
            div [ Id "inner-info" ] [
                yield!
                    players
                    |> List.map (fun (PlayerId playerId, player) ->
                        let (width, height) = (30., 30.)
                        let ratio = System.Math.Min(width / player.Costume.Size.Width, height / player.Costume.Size.Height)
                        let playerWidth = ratio * player.Costume.Size.Width
                        let playerHeight = ratio * player.Costume.Size.Height
                        div [ Class "player" ] [
                            svg [
                                    Class "view"
                                    SVGAttr.Width width
                                    SVGAttr.Height height
                                    sprintf "rotate(%f 0 0)" (360. - Degrees.value player.Direction)
                                    |> SVGAttr.Transform
                                    SVGAttr.Opacity (if player.IsVisible then 1. else 0.5)
                                ] [
                                    g
                                        [
                                            [
                                                sprintf "translate(%f %f)" ((width - playerWidth) / 2.) ((height - playerHeight) / 2.)
                                                sprintf "scale(%f %f)" ratio ratio
                                            ]
                                            |> String.concat " "
                                            |> SVGAttr.Transform
                                            DangerouslySetInnerHTML { __html = player.Costume.SvgData }
                                        ]
                                        []
                                ]

                            span [ Class "info" ] [
                                str (sprintf "X: %.2f | Y: %.2f | ∠ %.2f°" player.Position.X player.Position.Y (Degrees.value player.Direction))
                            ]
                        ]
                    )
            ]
        ]
    ]

let stream states msgs =
    let msgChannel =
        let url = sprintf "ws://%s%s" Server.host MessageChannel.endpoint
        let encode = Encode.channelMsg >> Encode.toString 0
        let decode =
            Decode.fromString Decode.channelMsg
            >> (function
                | Ok p -> Some p
                | Error p ->
                    eprintfn "Deserializing message failed: %O" p
                    None
            )
        AsyncRx.msgChannel url encode decode
    [
        msgs
        |> AsyncRx.choose (function
            | UIMsg (UpdateStringAnswer _) as x -> Some x
            | _ -> None
        )

        states
        |> AsyncRx.map (snd >> fun model -> model.WindowTitle)
        |> AsyncRx.distinctUntilChanged
        |> AsyncRx.tapOnNext (fun title ->
            Browser.Dom.document.title <-
                match title with
                | Some text -> sprintf "Get It - %s" text
                | None -> "Get It"
        )
        |> AsyncRx.flatMapLatest (ignore >> AsyncRx.empty)

        [
            AsyncRx.defer (fun () ->
                Browser.Dom.document.querySelector "#elmish-app"
                |> AsyncRx.observeSubTreeAdditions
                |> AsyncRx.choose (fun (n: Node) ->
                    if n.nodeType = n.ELEMENT_NODE then Some (n :?> HTMLElement) else None
                )
                |> AsyncRx.startWith [ Browser.Dom.document.body ]
            )
            |> AsyncRx.choose (fun n -> n.querySelector("#scene") :?> HTMLElement |> Option.ofObj)
            // |> AsyncRx.take 1
            |> AsyncRx.map (fun n -> let bounds = n.getBoundingClientRect() in (bounds.width, bounds.height))
            |> AsyncRx.merge AsyncRx.observeSceneSizeFromWindowResize
            |> AsyncRx.map(fun (width, height) ->
                {
                    Position = { X = -width / 2.; Y = -height / 2. }
                    Size = { Width = width; Height = height }
                }
                |> SetSceneBounds
            )

            states
            |> AsyncRx.choose (function | Some (ControllerMsg (msgId, InputEvent (MouseMove virtualScreenPosition))), model -> Some (virtualScreenPosition, model.SceneBounds) | _ -> None)
            |> AsyncRx.map (fun (virtualScreenPosition, sceneBounds) ->
                {
                    X = sceneBounds.Left + virtualScreenPosition.X * Browser.Dom.window.screen.availWidth - Browser.Dom.window.screenLeft - 5.
                    Y = sceneBounds.Top - virtualScreenPosition.Y * Browser.Dom.window.screen.availHeight + Browser.Dom.window.screenTop + 19.
                }
                |> SetMousePosition
            )

            states
            |> AsyncRx.choose (function | Some (ControllerMsg (msgId, InputEvent (MouseClick mouseClick))), model -> Some (mouseClick, model.SceneBounds) | _ -> None)
            |> AsyncRx.map (fun (mouseClick, sceneBounds) ->
                {
                    Button = mouseClick.Button
                    Position = {
                        X = sceneBounds.Left + mouseClick.VirtualScreenPosition.X * Browser.Dom.window.screen.availWidth - Browser.Dom.window.screenLeft - 7.
                        Y = sceneBounds.Top - mouseClick.VirtualScreenPosition.Y * Browser.Dom.window.screen.availHeight + Browser.Dom.window.screenTop + 24.
                    }
                }
                |> ApplyMouseClick
            )

            msgs
            |> AsyncRx.choose (function
                | UIMsg (AnswerStringQuestion _ as msg)
                | UIMsg (AnswerBoolQuestion _ as msg) -> Some msg
                | _ -> None
            )
        ]
        |> AsyncRx.mergeSeq
        |> AsyncRx.map UIMsg
        |> AsyncRx.merge (
            states
            |> AsyncRx.choose (fst >> function | Some (ControllerMsg _ as msg) -> Some msg | _ -> None)
        )
        |> msgChannel
    ]
    |> AsyncRx.mergeSeq

Program.mkSimple init update (fun model dispatch -> view model (UIMsg >> dispatch))
|> Program.withStream stream
#if DEBUG
|> Program.withDebugger
|> Program.withConsoleTrace
#endif
|> Program.withReactBatched "elmish-app"
|> Program.run
