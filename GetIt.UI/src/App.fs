module GetIt.UI

open Browser.Dom
open Browser.Types
open Elmish
open Elmish.Debug
open Elmish.React
open Elmish.HMR // Must be last Elmish.* open declaration (see https://elmish.github.io/hmr/#Usage)
open Fable.Core
open Fable.Core.JsInterop
open Fable.Elmish.Nile
open Fable.React
open Fable.React.Props
open Fable.Reaction
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
        PenLines: PenLine list
        Background: SvgImage
        BatchMessages: (ChannelMsg list * int) option
    }

let init () =
    {
        SceneBounds = GetIt.Rectangle.zero
        WindowTitle = None
        Players = Map.empty
        PenLines = []
        Background = Background.none
        BatchMessages = None
    }

let rec private updateDirectly msg model =
    let updatePlayer playerId fn =
        let player = Map.find playerId model.Players |> fn
        { model with Players = Map.add playerId player model.Players }

    match msg with
    | UIMsg (SetSceneBounds bounds) ->
        { model with SceneBounds = bounds }
    | UIMsg (AnswerStringQuestion (playerId, answer)) ->
        updatePlayer playerId (fun p ->
            match p.SpeechBubble with
            | Some (AskString _) -> { p with SpeechBubble = None }
            | Some (AskBool _)
            | Some (Say _)
            | None -> p
        )
    | UIMsg (AnswerBoolQuestion (playerId, answer)) ->
        updatePlayer playerId (fun p ->
            match p.SpeechBubble with
            | Some (AskBool _) -> { p with SpeechBubble = None }
            | Some (AskString _)
            | Some (Say _)
            | None -> p
        )
    | ControllerMsg (msgId, SetPosition (playerId, position)) ->
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
                    line :: model.PenLines
                else model.PenLines
        }
    | ControllerMsg (msgId, ChangePosition (playerId, relativePosition)) ->
        let player = Map.find playerId model.Players
        updateDirectly (ControllerMsg (System.Guid.NewGuid(), SetPosition (playerId, player.Position + relativePosition))) model
    | ControllerMsg (msgId, SetDirection (playerId, angle)) ->
        updatePlayer playerId (fun p -> { p with Direction = angle })
    | ControllerMsg (msgId, ChangeDirection (playerId, relativeDirection)) ->
        let player = Map.find playerId model.Players
        updateDirectly (ControllerMsg (System.Guid.NewGuid(), SetDirection (playerId, player.Direction + relativeDirection))) model
    | ControllerMsg (msgId, SetSpeechBubble (playerId, speechBubble)) ->
        updatePlayer playerId (fun p -> { p with SpeechBubble = speechBubble })
    | ControllerMsg (msgId, SetPenState (playerId, isOn)) ->
        updatePlayer playerId (fun p -> { p with Pen = { p.Pen with IsOn = isOn } })
    | ControllerMsg (msgId, TogglePenState playerId) ->
        updatePlayer playerId (fun p -> { p with Pen = { p.Pen with IsOn = not p.Pen.IsOn } })
    | ControllerMsg (msgId, SetPenColor (playerId, color)) ->
        updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Color = color } })
    | ControllerMsg (msgId, ShiftPenColor (playerId, angle)) ->
        updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Color = Color.hueShift angle p.Pen.Color } })
    | ControllerMsg (msgId, SetPenWeight (playerId, weight)) ->
        updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Weight = weight } })
    | ControllerMsg (msgId, ChangePenWeight (playerId, weight)) ->
        updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Weight = p.Pen.Weight + weight } })
    | ControllerMsg (msgId, SetSizeFactor (playerId, sizeFactor)) ->
        updatePlayer playerId (fun p -> { p with SizeFactor = sizeFactor })
    | ControllerMsg (msgId, ChangeSizeFactor (playerId, sizeFactor)) ->
        updatePlayer playerId (fun p -> { p with SizeFactor = p.SizeFactor + sizeFactor })
    | ControllerMsg (msgId, SetVisibility (playerId, isVisible)) ->
        updatePlayer playerId (fun p -> { p with IsVisible = isVisible })
    | ControllerMsg (msgId, ToggleVisibility playerId) ->
        updatePlayer playerId (fun p -> { p with IsVisible = not p.IsVisible })
    | ControllerMsg (msgId, SetNextCostume playerId) ->
        updatePlayer playerId Player.nextCostume
    | ControllerMsg (msgId, SendToBack playerId) ->
        { model with Players = Player.sendToBack playerId model.Players }
    | ControllerMsg (msgId, BringToFront playerId) ->
        { model with Players = Player.bringToFront playerId model.Players }
    | ControllerMsg (msgId, AddPlayer (playerId, player)) ->
        { model with
            Players =
                Map.add playerId player model.Players
                |> Player.sendToBack playerId
        }
    | ControllerMsg (msgId, RemovePlayer playerId) ->
        { model with
            Players = Map.remove playerId model.Players
        }
    | ControllerMsg (msgId, SetWindowTitle title) ->
        { model with WindowTitle = title }
    | ControllerMsg (msgId, ClearScene) ->
        { model with PenLines = [] }
    | ControllerMsg (msgId, SetBackground background) ->
        { model with Background = background }
    | ControllerMsg (msgId, StartBatch) ->
        { model with BatchMessages = Some ([], 1) }
    | ControllerMsg (msgId, ApplyBatch) ->
        eprintfn "Can't apply batch because no batch is running"
        model

let update msg model =
    match model.BatchMessages with
    | None ->
        updateDirectly msg model
    | Some (messages, level) ->
        match msg with
        | ControllerMsg (msgId, StartBatch) ->
            { model with BatchMessages = Some (messages, level + 1) }
        | ControllerMsg (msgId, ApplyBatch) when level > 1 ->
            { model with BatchMessages = Some (messages, level - 1) }
        | ControllerMsg (msgId, ApplyBatch) ->
            (messages, ({ model with BatchMessages = None }))
            ||> List.foldBack updateDirectly
        | x ->
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

    let background =
        img [
            Src (
                model.Background.SvgData
                |> window.btoa
                |> sprintf "data:image/svg+xml;base64,%s"
            )
            Style [
                Width "100%"
                Height "100%"
                ObjectFit "cover"
            ]
        ]

    let scenePlayerView (player: PlayerData) =
        let left = -model.SceneBounds.Left + player.Bounds.Left
        let top = model.SceneBounds.Top - player.Bounds.Top
        img [
            Src (
                player.Costume.SvgData
                |> window.btoa
                |> sprintf "data:image/svg+xml;base64,%s"
            )
            Style [
                Width player.Size.Width
                Height player.Size.Height
                Transform (
                    [
                        sprintf "translate(%fpx,%fpx)" left top
                        sprintf "rotate(%fdeg)" (360. - Degrees.value player.Direction)
                    ]
                    |> String.concat " "
                )
            ]
        ]

    let speechBubbleView playerId (player: PlayerData) =
        let speechBubble content =
            div
                [
                    Class "speech-bubble"
                    Style [
                        let offsetLeft = -model.SceneBounds.Left + player.Bounds.Right - 30.
                        let offsetTop = model.SceneBounds.Top - player.Bounds.Top - 20.
                        yield Transform (sprintf "translate(%fpx, %fpx) translate(0,-100%%)" offsetLeft offsetTop)
                    ]
                ]
                content
            |> Some

        match player.SpeechBubble with
        | None -> None
        | Some (Say text) ->
            speechBubble [ span [] [ str text ] ]
        | Some (AskString text) ->
            speechBubble [
                span [] [ str text ]
                input [
                    AutoFocus true
                    OnKeyPress (fun ev -> if ev.charCode = 13. then dispatch (AnswerStringQuestion (playerId, ev.Value)))
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

    let scenePlayersView =
        playersOnScene
        |> List.collect (fun (playerId, player) ->
            [
                yield scenePlayerView player
                yield! speechBubbleView playerId player |> Option.toList
            ]
        )

    div [ Id "main" ] [
        div [ Id "scene" ] [
            yield div [ Style [ Position PositionOptions.Absolute; Width "100%"; Height "100%" ] ] [ background ]
            yield div [ Style [ Position PositionOptions.Absolute ] ] [ canvas [ Id "scene-pen-lines" ] [] ]
            yield! scenePlayersView |> List.map (List.singleton >> div [ Style [ Position PositionOptions.Absolute ] ])
        ]
        PerfectScrollbar.perfectScrollbar [ PerfectScrollbar.Id "info" ] [
            div [ Id "inner-info" ] [
                yield!
                    players
                    |> List.map (fun (_playerId, player) ->
                        div [ Class "player" ] [
                            img [
                                Class "view"
                                Src (
                                    player.Costume.SvgData
                                    |> window.btoa
                                    |> sprintf "data:image/svg+xml;base64,%s"
                                )
                                Style [
                                    Width "30px"
                                    Height "30px"
                                    ObjectFit "contain"
                                    Transform (sprintf "rotate(%fdeg)" (360. - Degrees.value player.Direction))
                                    Opacity (if player.IsVisible then 1. else 0.5)
                                ]
                            ]

                            span [ Class "info" ] [
                                str (sprintf "X: %.2f | Y: %.2f | ↻ %.2f°" player.Position.X player.Position.Y (Degrees.value player.Direction))
                            ]
                        ]
                    )
            ]
        ]
    ]

let getSceneBounds width height =
    {
        Position = { X = -width / 2.; Y = -height / 2. }
        Size = { Width = width; Height = height }
    }

let drawLine (canvas: HTMLCanvasElement) line =
    let sceneBounds = getSceneBounds canvas.width canvas.height
    let ctx = canvas.getContext_2d()
    ctx.beginPath()
    ctx.strokeStyle <- U3.Case1 (RGBAColor.rgbaHexNotation line.Color)
    ctx.lineWidth <- line.Weight
    ctx.moveTo(line.Start.X - sceneBounds.Left, sceneBounds.Top - line.Start.Y)
    ctx.lineTo(line.End.X - sceneBounds.Left, sceneBounds.Top - line.End.Y)
    ctx.stroke ()

let drawLines canvas = List.iter (drawLine canvas)

let clearScene (canvas: HTMLCanvasElement) =
    canvas.getContext_2d().clearRect(0., 0., canvas.width, canvas.height)

let stream (states: IAsyncObservable<ChannelMsg option * Model> ) (msgs: IAsyncObservable<ChannelMsg>) =
    let msgChannel =
        let socketUrl =
            let urlParams = createNew window?URLSearchParams window.location.search
            urlParams?get("socketUrl") |> Option.ofObj
            |> Option.map window?decodeURIComponent
            |> Option.defaultValue "ws://localhost/socket"
        let encode = Encode.channelMsg >> Encode.toString 0
        let decode =
            Decode.fromString Decode.channelMsg
            >> (function
                | Ok p -> Some p
                | Error p ->
                    eprintfn "Deserializing message failed: %O" p
                    None
            )
        AsyncRx.msgChannel socketUrl encode decode
        >> AsyncRx.``finally`` (fun () -> async { window.close() })

    let nodeCreated selector =
        AsyncRx.defer (fun () ->
            document.querySelector "#elmish-app"
            |> AsyncRx.observeSubTreeAdditions
            |> AsyncRx.choose (fun (n: Node) ->
                if n.nodeType = n.ELEMENT_NODE then Some (n :?> HTMLElement) else None
            )
            |> AsyncRx.startWith [ document.body ]
        )
        |> AsyncRx.choose (fun n -> n.querySelector(selector) :?> HTMLElement |> Option.ofObj)
        |> AsyncRx.take 1

    let sceneSizeChanged =
        nodeCreated "#scene"
        |> AsyncRx.flatMapLatest (fun sceneElement ->
            AsyncRx.observeElementSizeFromWindowResize "#scene"
            |> AsyncRx.map (fun sceneSize -> (sceneElement, sceneSize))
        )

    let resizePenLineCanvas =
        sceneSizeChanged
        |> AsyncRx.map (fun (scene, size) -> scene.querySelector("#scene-pen-lines") :?> HTMLCanvasElement, size)
        |> AsyncRx.tapOnNext (fun (canvas, (width, height)) ->
            canvas.width <- width
            canvas.height <- height
        )
        |> AsyncRx.map fst

    let penLines =
        let source =
            states
            |> AsyncRx.map (snd >> fun s -> s.PenLines)
        resizePenLineCanvas
        |> AsyncRx.flatMapLatest (fun canvas ->
            source
            |> AsyncRx.sampleWith AsyncRx.requestAnimationFrameObservable
            |> AsyncRx.startWith [[]] // redraw all lines after scene size changed
            |> AsyncRx.pairwise
            |> AsyncRx.map (fun penLines -> canvas, penLines)
        )
        |> AsyncRx.tapOnNext (fun (canvas, (previousPenLines, nextPenLines)) ->
            match nextPenLines.Length - previousPenLines.Length with
            | x when x >= 0 ->
                nextPenLines
                |> List.take x
                |> drawLines canvas
            | _ -> clearScene canvas

        )
        |> AsyncRx.ignore

    let uiMsgs =
        [
            sceneSizeChanged
            |> AsyncRx.map(snd >> fun (width, height) ->
                getSceneBounds width height
                |> SetSceneBounds
                |> UIMsg
            )

            msgs
            |> AsyncRx.choose (function
                | UIMsg (AnswerStringQuestion _)
                | UIMsg (AnswerBoolQuestion _) as msg -> Some msg
                | _ -> None
            )
        ]
        |> AsyncRx.mergeSeq

    let controllerMsgs =
        states |> AsyncRx.choose (fst >> function | Some (ControllerMsg _ as msg) -> Some msg | _ -> None)

    [
        states
        |> AsyncRx.map (snd >> fun model -> model.WindowTitle)
        |> AsyncRx.distinctUntilChanged
        |> AsyncRx.tapOnNext (fun title ->
            document.title <-
                match title with
                | Some text -> sprintf "Get It - %s" text
                | None -> "Get It"
        )
        |> AsyncRx.ignore

        penLines

        [
            uiMsgs
            controllerMsgs
        ]
        |> AsyncRx.mergeSeq
        |> msgChannel
    ]
    |> AsyncRx.mergeSeq
    |> AsyncRx.takeUntil AsyncRx.beforeWindowUnloadObservable

Program.mkSimple init update (fun model dispatch -> view model (UIMsg >> dispatch))
|> Program.withStream stream
// #if DEBUG
// |> Program.withDebugger
// |> Program.withConsoleTrace
// #endif
|> Program.withReactBatched "elmish-app"
|> Program.run
