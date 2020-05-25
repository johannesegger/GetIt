module GetIt.UI

open Browser
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

type SpeechBubblePosition = Left | Right

type Model =
    {
        SceneBounds: GetIt.Rectangle
        WindowTitle: string option
        Players: Map<PlayerId, PlayerData>
        SpeechBubblePositions: Map<PlayerId, SpeechBubblePosition>
        PenLines: PenLine list
        Background: SvgImage
        BatchMessages: (ChannelMsg list * int) option
    }

type LocalMsg = SetSpeechBubblePosition of PlayerId * SpeechBubblePosition

type Msg =
    | ChannelMsg of ChannelMsg
    | LocalMsg of LocalMsg

let init () =
    {
        SceneBounds = GetIt.Rectangle.zero
        WindowTitle = None
        Players = Map.empty
        SpeechBubblePositions = Map.empty
        PenLines = []
        Background = Background.none
        BatchMessages = None
    }

let private updateLocally msg model =
    match msg with
    | SetSpeechBubblePosition (playerId, position) ->
        { model with SpeechBubblePositions = Map.add playerId position model.SpeechBubblePositions }

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
            SpeechBubblePositions = Map.remove playerId model.SpeechBubblePositions
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
    match msg, model.BatchMessages with
    | LocalMsg msg, _ -> updateLocally msg model
    | ChannelMsg msg, None ->
        updateDirectly msg model
    | ChannelMsg msg, Some (messages, level) ->
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

let private initSpeechBubble (sceneBounds: Rectangle) (playerBounds: Rectangle) position (element: HTMLElement) =
    let border = element.querySelector(":scope > .border") :?> HTMLCanvasElement
    let content = element.querySelector(":scope > .content") :?> HTMLElement

    let ctx = border.getContext_2d()

    let width = content.offsetWidth
    let height = content.offsetHeight
    let scaleFactor = window.devicePixelRatio
    border.style.width <- sprintf "%fpx" width
    border.style.height <- sprintf "%fpx" height
    border.width <- width * scaleFactor
    border.height <- height * scaleFactor
    ctx.scale(scaleFactor, scaleFactor)

    ctx.strokeStyle <- U3.Case1 "#00000033"
    ctx.fillStyle <- U3.Case1 "#8B451310"
    ctx.lineWidth <- 2.
    ctx.beginPath()
    ctx.moveTo(10., 5.)
    ctx.lineTo(width - 10., 5.)
    ctx.bezierCurveTo(width, 5., width, height - 20., width - 10., height - 20.)
    ctx.lineTo(40., height - 20.)
    ctx.bezierCurveTo(40. - 20., height, 40. - 20. - 15., height, 40. - 15., height - 20.)
    ctx.lineTo(10., height - 20.)
    ctx.bezierCurveTo(0., height - 20., 0., 5., 10., 5.)
    ctx.stroke()
    ctx.fill()

    element.style.transform <-
        match position with
        | Left -> sprintf "translate(%fpx, %fpx) translate(-100%%, -100%%)" (-sceneBounds.Left + playerBounds.Left) (sceneBounds.Top - playerBounds.Top)
        | Right -> sprintf "translate(%fpx, %fpx) translate(0,-100%%)" (-sceneBounds.Left + playerBounds.Right) (sceneBounds.Top - playerBounds.Top)

    match element.getBoundingClientRect().top with
    | top when top < 0. -> element.style.transform <- sprintf "%s translate(0, %fpx)" element.style.transform -top
    | _ -> ()

    match position, element.getBoundingClientRect() with
    | Right, bounds when bounds.right > sceneBounds.Size.Width -> Some Left
    | Right, _ -> None
    | Left, bounds when bounds.left < 0. -> Some Right
    | Left, _ -> None

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

    let speechBubbleView playerId (player: PlayerData) position =
        let speechBubble content =
            div
                [
                    Class "speech-bubble"
                    Ref (fun e ->
                        if not <| isNull e then
                            match initSpeechBubble model.SceneBounds player.Bounds position (e :?> HTMLElement) with
                            | Some newPosition -> dispatch (LocalMsg (SetSpeechBubblePosition (playerId, newPosition)))
                            | None -> ()
                    )
                ]
                [
                    canvas
                        [
                            yield Class "border"
                            match position with
                            | Left -> yield Style [ Transform "Scale(-1, 1)" ]
                            | Right -> ()
                        ]
                        []
                    div [ Class "content" ] [ PerfectScrollbar.perfectScrollbar [] content ]
                ]
            |> Some

        match player.SpeechBubble with
        | None
        | Some (Say "") -> None
        | Some (Say text) ->
            speechBubble [ span [] [ str text ] ]
        | Some (AskString text) ->
            speechBubble [
                span [] [ str text ]
                input [
                    AutoFocus true
                    OnKeyPress (fun ev -> if ev.charCode = 13. then dispatch (ChannelMsg (UIMsg (AnswerStringQuestion (playerId, ev.Value)))))
                    Style [ Width "100%"; MarginTop "5px"; BoxSizing BoxSizingOptions.BorderBox ]
                ]
            ]
        | Some (AskBool text) ->
            speechBubble [
                span [] [ str text ]
                div [ Class "askbool-answers" ] [
                    button [ OnClick (fun ev -> dispatch (ChannelMsg (UIMsg (AnswerBoolQuestion (playerId, true))))) ] [ str "✔️" ]
                    button [ OnClick (fun ev -> dispatch (ChannelMsg (UIMsg (AnswerBoolQuestion (playerId, false))))) ] [ str "❌" ]
                ]
            ]

    let scenePlayersView =
        playersOnScene
        |> List.collect (fun (playerId, player) ->
            [
                yield scenePlayerView player
                let speechBubbleSettings =
                    Map.tryFind playerId model.SpeechBubblePositions
                    |> Option.defaultValue Right
                yield! speechBubbleView playerId player speechBubbleSettings |> Option.toList
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

let stream (states: IAsyncObservable<Msg option * Model> ) (msgs: IAsyncObservable<Msg>) =
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
        >> AsyncRx.takeUntil AsyncRx.beforeWindowUnloadObservable
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

    let sceneSizeChanged =
        nodeCreated "#scene"
        |> AsyncRx.take 1
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
                | ChannelMsg (UIMsg (AnswerStringQuestion _) as msg)
                | ChannelMsg (UIMsg (AnswerBoolQuestion _) as msg) -> Some msg
                | _ -> None
            )
        ]
        |> AsyncRx.mergeSeq

    let controllerMsgs =
        states |> AsyncRx.choose (fst >> function | Some (ChannelMsg (ControllerMsg _ as msg)) -> Some msg | _ -> None)

    [
        msgs
        |> AsyncRx.filter (function LocalMsg _ -> true | _ -> false)

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
        |> AsyncRx.map ChannelMsg
    ]
    |> AsyncRx.mergeSeq

Program.mkSimple init update view
|> Program.withStream stream
// #if DEBUG
// |> Program.withDebugger
// |> Program.withConsoleTrace
// #endif
|> Program.withReactBatched "elmish-app"
|> Program.run
