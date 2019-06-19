module GetIt.UI

open Browser.Types
open Elmish
open Elmish.Debug
open Elmish.React
open Elmish.Streams
open Elmish.HMR // Must be last Elmish.* open declaration (see https://elmish.github.io/hmr/#Usage)
open Fable.Core.JsInterop
open Fable.Elmish.Nile
open Fable.React
open Fable.React.Props
open FSharp.Control
open Thoth.Json

importAll "../sass/main.sass"

// type Msg =
//     | SetSceneBounds of GetIt.Rectangle
//     | SetMousePosition of positionRelativeToSceneControl: Position
//     | ApplyMouseClick of MouseButton * positionRelativeToSceneControl: Position
//     | SetPosition of PlayerId * Position
//     | ChangePosition of PlayerId * Position
//     | SetDirection of PlayerId * Degrees
//     | ChangeDirection of PlayerId * Degrees
//     | SetSpeechBubble of PlayerId * SpeechBubble option
//     | UpdateAnswer of PlayerId * string
//     | ApplyStringAnswer of PlayerId * string
//     | ApplyBoolAnswer of PlayerId * bool
//     | SetPenState of PlayerId * isOn: bool
//     | TogglePenState of PlayerId
//     | SetPenColor of PlayerId * RGBAColor
//     | ShiftPenColor of PlayerId * Degrees
//     | SetPenWeight of PlayerId * float
//     | ChangePenWeight of PlayerId * float
//     | SetSizeFactor of PlayerId * float
//     | ChangeSizeFactor of PlayerId * float
//     | SetVisibility of PlayerId * isVisible: bool
//     | SetNextCostume of PlayerId
//     | SendToBack of PlayerId
//     | BringToFront of PlayerId
//     | AddPlayer of PlayerId * PlayerData
//     | RemovePlayer of PlayerId
//     | ClearScene
//     | SetBackground of SvgImage
//     | StartBatch
//     | ApplyBatch

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
        Players: Map<PlayerId, PlayerData>
        PlayerStringAnswers: Map<PlayerId, string>
        PenLines: PenLine list
        Background: SvgImage
        BatchMessages: (ChannelMsg list * int) option
    }

let init () =
    {
        SceneBounds = GetIt.Rectangle.zero
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
    // | None, SetMousePosition positionRelativeToSceneControl ->
    //     model
    | None, UIMsg (ApplyMouseClick mouseClick) ->
        model
    // | None, SetPosition (playerId, position) ->
    //     let player = Map.find playerId model.Players
    //     let player' = { player with Position = position }
    //     { model with
    //         Players = Map.add playerId player' model.Players
    //         PenLines =
    //             if player.Pen.IsOn
    //             then
    //                 let line =
    //                     {
    //                         Start = player.Position
    //                         End = player'.Position
    //                         Weight = player.Pen.Weight
    //                         Color = player.Pen.Color
    //                     }
    //                 model.PenLines @ [ line ]
    //             else model.PenLines
    //     }
    // | None, ChangePosition (playerId, relativePosition) ->
    //     let player = Map.find playerId model.Players
    //     update (SetPosition (playerId, player.Position + relativePosition)) model
    // | None, SetDirection (playerId, angle) ->
    //     updatePlayer playerId (fun p -> { p with Direction = angle })
    // | None, ChangeDirection (playerId, relativeDirection) ->
    //     let player = Map.find playerId model.Players
    //     update (SetDirection (playerId, player.Direction + relativeDirection)) model
    // | None, SetSpeechBubble (playerId, speechBubble) ->
    //     updatePlayer playerId (fun p -> { p with SpeechBubble = speechBubble })
    // | None, UpdateAnswer (playerId, answer) ->
    //     { model with PlayerStringAnswers = Map.add playerId answer model.PlayerStringAnswers }
    // | None, ApplyStringAnswer (playerId, answer) ->
    //     let model' =
    //         updatePlayer playerId (fun p ->
    //             match p.SpeechBubble with
    //             | Some (AskString _) -> { p with SpeechBubble = None }
    //             | Some (AskBool _)
    //             | Some (Say _)
    //             | None -> p
    //         )
    //     { model' with PlayerStringAnswers = Map.remove playerId model.PlayerStringAnswers }
    // | None, ApplyBoolAnswer (playerId, answer) ->
    //     updatePlayer playerId (fun p ->
    //         match p.SpeechBubble with
    //         | Some (AskBool _) -> { p with SpeechBubble = None }
    //         | Some (AskString _)
    //         | Some (Say _)
    //         | None -> p
    //     )
    // | None, SetPenState (playerId, isOn) ->
    //     updatePlayer playerId (fun p -> { p with Pen = { p.Pen with IsOn = isOn } })
    // | None, TogglePenState playerId ->
    //     updatePlayer playerId (fun p -> { p with Pen = { p.Pen with IsOn = not p.Pen.IsOn } })
    // | None, SetPenColor (playerId, color) ->
    //     updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Color = color } })
    // | None, ShiftPenColor (playerId, angle) ->
    //     updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Color = Color.hueShift angle p.Pen.Color } })
    // | None, SetPenWeight (playerId, weight) ->
    //     updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Weight = weight } })
    // | None, ChangePenWeight (playerId, weight) ->
    //     updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Weight = p.Pen.Weight + weight } })
    // | None, SetSizeFactor (playerId, sizeFactor) ->
    //     updatePlayer playerId (fun p -> { p with SizeFactor = sizeFactor })
    // | None, ChangeSizeFactor (playerId, sizeFactor) ->
    //     updatePlayer playerId (fun p -> { p with SizeFactor = p.SizeFactor + sizeFactor })
    // | None, SetVisibility (playerId, isVisible) ->
    //     updatePlayer playerId (fun p -> { p with IsVisible = isVisible })
    // | None, SetNextCostume playerId ->
    //     updatePlayer playerId Player.nextCostume
    // | None, SendToBack playerId ->
    //     { model with Players = Player.sendToBack playerId model.Players }
    // | None, BringToFront playerId ->
    //     { model with Players = Player.bringToFront playerId model.Players }
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
    // | None, ClearScene ->
    //     { model with PenLines = [] }
    // | None, SetBackground background ->
    //     { model with Background = background }
    // | None, StartBatch ->
    //     { model with BatchMessages = Some ([], 1) }
    // | Some (messages, level), StartBatch ->
    //     { model with BatchMessages = Some (messages, level + 1) }
    // | None, ApplyBatch ->
    //     model // TODO send error to controller?
    // | Some (messages, level), ApplyBatch when level > 1 ->
    //     { model with BatchMessages = Some (messages, level - 1) }
    // | Some (messages, level), ApplyBatch ->
    //     (messages, ({ model with BatchMessages = None }))
    //     ||> List.foldBack update
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

    let drawPlayer (player: PlayerData) position =
        player.Costume.SvgData
        |> Browser.Dom.window.btoa
        |> sprintf "data:image/svg+xml;base64,%s"
        |> Canvas.drawImage position (player.Size.Width, player.Size.Height)

    let drawScenePlayers =
        playersOnScene
        |> List.map (fun (PlayerId playerId, player) ->
            Canvas.Batch [
                Canvas.Save
                Canvas.Translate (-model.SceneBounds.Left + player.Position.X, model.SceneBounds.Top - player.Position.Y)
                Canvas.Rotate (2. * System.Math.PI - Degrees.toRadians player.Direction)
                Canvas.Scale (player.SizeFactor, player.SizeFactor)
                drawPlayer player (-player.Size.Width / 2., -player.Size.Height / 2.)
                Canvas.Restore
            ]
        )
        |> Canvas.Batch
    
    div [ Id "main" ] [
        canvasSize model.SceneBounds.Size
        |> Canvas.initialize
        |> Canvas.withId "scene"
        |> Canvas.draw (Canvas.ClearReact (0., 0., model.SceneBounds.Size.Width, model.SceneBounds.Size.Height))
        |> Canvas.draw drawScenePlayers
        |> Canvas.render

        div [ Id "info" ] [
            yield!
                players
                |> List.map (fun (playerId, player) ->
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
                                drawPlayer player (-player.Size.Width / 2., -player.Size.Height / 2.)
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
    ]

let observeSubTreeAdditions (parent: Node) : IAsyncObservable<Node> =
    AsyncRx.create (fun obs -> async {
        let onMutate mutations =
            mutations
            |> Seq.collect (fun m -> m?addedNodes)
            |> Seq.iter (obs.OnNextAsync >> Async.StartImmediate)
        let mutationObserver = createNew Browser.Dom.window?MutationObserver (onMutate)
        let mutationObserverConfig = createObj [
            "childList" ==> true
            "subtree" ==> true
        ]
        mutationObserver?observe(parent, mutationObserverConfig)
        return AsyncDisposable.Create (fun () -> async {
            mutationObserver?disconnect()
        })
    })

let observeResize (element: HTMLElement) : IAsyncObservable<float * float> =
    AsyncRx.create (fun obs -> async {
        let resizeObserver = createNew Browser.Dom.window?ResizeObserver (fun entries ->
            entries
            |> Seq.exactlyOne
            |> fun e -> (e?contentRect?width, e?contentRect?height)
            |> obs.OnNextAsync
            |> Async.StartImmediate
        )
        resizeObserver?observe(element)
        return AsyncDisposable.Create (fun () -> async {
            resizeObserver?disconnect()
        })
    })

let observeSceneSizeFromWindowResize =
    AsyncRx.create (fun obs -> async {
        let resizeCanvas evt =
            Browser.Dom.console.log (evt)
            obs.OnNextAsync (Browser.Dom.window.innerWidth, Browser.Dom.window.innerHeight)
            |> Async.StartImmediate
            ()
        Browser.Dom.window.addEventListener("resize", resizeCanvas, false)
        return AsyncDisposable.Create (fun () -> async {
            Browser.Dom.window.removeEventListener("resize", resizeCanvas, false)
        })
    })
    |> AsyncRx.choose (fun (windowWidth, windowHeight) ->
        match Browser.Dom.document.querySelector "#info" :?> HTMLElement |> Option.ofObj with
        | Some info -> Some (windowWidth, windowHeight - info.offsetHeight)
        | None -> None
    )

let stream states msgs =
    [
        msgs

        Browser.Dom.document.querySelector "#elmish-app"
        |> observeSubTreeAdditions
        |> AsyncRx.choose (fun (n: Node) ->
            if n.nodeType = n.ELEMENT_NODE then Some (n :?> HTMLElement) else None
        )
        |> AsyncRx.startWith [ Browser.Dom.document.body ]
        |> AsyncRx.choose (fun n -> n.querySelector("#scene") :?> HTMLElement |> Option.ofObj)
        // |> AsyncRx.take 1
        |> AsyncRx.map (fun n -> n.offsetWidth, n.offsetHeight)
        // |> AsyncRx.flatMapLatest observeResize
        // |> AsyncRx.debounce 100
        // |> AsyncRx.distinctUntilChanged
        |> AsyncRx.merge observeSceneSizeFromWindowResize
        |> AsyncRx.map(fun (width, height) ->
            {
                Position = { X = -width / 2.; Y = -height / 2. }
                Size = { Width = width; Height = height }
            }
            |> SetSceneBounds
            |> UIMsg
        )
        |> AsyncRx.msgChannel (sprintf "ws://%s%s" Browser.Dom.window.location.host MessageChannel.endpoint) (Encode.channelMsg >> Encode.toString 0) (Decode.fromString Decode.channelMsg >> Result.toOption)
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
