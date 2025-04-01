module internal GetIt.UI.Container.MessageProcessing

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Media
open Avalonia.Media.Imaging
open Avalonia.Svg.Skia
open DynamicData
open DynamicData.Binding
open FSharp.Control.Reactive
open GetIt
open GetIt.UIV2.ViewModels
open global.ReactiveUI
open System
open System.IO
open System.Reactive.Subjects
open System.Threading
open System.Threading.Tasks

type LoadedSvgImage = {
    Size: Size
    Image: SvgSource
}

type PlayerData = {
    SizeFactor: float
    Costumes: OneOfMany<LoadedSvgImage>
    Pen: GetIt.Pen
}

type Batching = {
    Level: int
    Messages: ControllerMsg list
}
module Batching =
    let zero = { Level = 0; Messages = [] }

type Model = {
    Players: Map<PlayerId, PlayerData>
    Batching: Batching
}
module Model =
    let zero =
        {
            Players = Map.empty
            Batching = Batching.zero
        }

let private convertSvgImage image =
    SvgSource.LoadFromSvg image.SvgData

let rec private processControllerMessageDirectly (mainViewModel: MainWindowViewModel) msg model =
    let updatePlayer playerId fn =
        mainViewModel.Players
        |> Seq.filter (fun p -> p.Id = playerId)
        |> Seq.iter fn
        model
    let updatePlayerPosition playerId fn =
        updatePlayer playerId (fun p ->
            let newPosition = fn p.Position
            let pen = model.Players |> Map.tryFind playerId |> Option.map (fun p -> p.Pen)
            match pen with
            | Some pen when pen.IsOn ->
                mainViewModel.AddPenLine(p.Position, newPosition, pen.Weight, pen.Color)
            | _ -> ()
            p.Position <- newPosition
        )
    let getSpeechBubbleViewModel speechBubbleData =
        match speechBubbleData with
        | Some (Say text) -> SaySpeechBubbleViewModel(Text = text) :> SpeechBubbleViewModel
        | Some (AskString text) -> AskTextSpeechBubbleViewModel(Text = text) :> SpeechBubbleViewModel
        | Some (AskBool text) -> AskBoolSpeechBubbleViewModel(Text = text) :> SpeechBubbleViewModel
        | None -> null
    let updatePen playerId fn =
        { model with
            Players =
                Map.tryFind playerId model.Players
                |> Option.map (fun player ->
                    Map.add playerId { player with Pen = fn player.Pen } model.Players
                )
                |> Option.defaultValue model.Players
        }
    let updateSizeFactor playerId fn =
        Map.tryFind playerId model.Players
        |> Option.map (fun playerData ->
            let sizeFactor = fn playerData.SizeFactor
            let costume = OneOfMany.current playerData.Costumes
            let model = updatePlayer playerId (fun player -> player.Size <- costume.Size * sizeFactor)
            { model with Players = Map.add playerId { playerData with SizeFactor = sizeFactor } model.Players }
        )
        |> Option.defaultValue model
    let setLayer getLayer playerId players =
        let playerLayer = players |> Seq.map (fun (p: PlayerViewModel) -> p.ZIndex) |> getLayer
        players
        |> Seq.tryFind (fun p -> p.Id = playerId)
        |> Option.iter (fun p -> p.ZIndex <- playerLayer)
        players
        |> Seq.sortBy (fun p -> p.ZIndex)
        |> Seq.iteri (fun idx p -> p.ZIndex <- idx + 1)
    let sendToBack playerId players =
        setLayer (Seq.min >> fun l -> l - 1) playerId players
    let bringToFront playerId players =
        setLayer (Seq.max >> fun l -> l + 1) playerId players
    let captureScene () =
        let appLifetime = Application.Current.ApplicationLifetime :?> IClassicDesktopStyleApplicationLifetime
        let mainWindow = appLifetime.MainWindow
        let captureSize = PixelSize.FromSize(mainWindow.Bounds.Size, scale = 1.)
        let captureDpi = new Vector(96, 96)
        use bitmap = new RenderTargetBitmap(captureSize, captureDpi)
        bitmap.Render(mainWindow)
        use targetStream = new MemoryStream()
        bitmap.Save(targetStream)
        targetStream.ToArray() |> PngImage
    match msg with
    | AddPlayer (playerId, playerData) ->
        mainViewModel.AddPlayer(playerId, fun p ->
            p.Image <- new Svg.Skia.SvgImage(Source = convertSvgImage playerData.Costume) :> IImage
            p.Size <- playerData.Size
            p.Position <- playerData.Position
            p.Angle <- Degrees.value playerData.Direction
            p.IsVisible <- playerData.IsVisible
            p.SpeechBubble <- getSpeechBubbleViewModel playerData.SpeechBubble
        )
        sendToBack playerId mainViewModel.Players
        let playerData = {
            SizeFactor = playerData.SizeFactor
            Costumes =
                playerData.Costumes
                |> OneOfMany.map (fun v -> { Size =  v.Size; Image = convertSvgImage v })
            Pen = playerData.Pen
        }
        { model with Players = Map.add playerId playerData model.Players }
        , None
    | RemovePlayer playerId ->
        mainViewModel.Players
        |> Seq.filter (fun p -> p.Id = playerId)
        |> mainViewModel.Players.RemoveMany
        { model with Players = Map.remove playerId model.Players }
        , None
    | SetWindowTitle None ->
        mainViewModel.Title <- "Get It"
        model
        , None
    | SetWindowTitle (Some title) ->
        mainViewModel.Title <- sprintf "Get It - %s" title
        model
        , None
    | SetBackground image ->
        mainViewModel.BackgroundImage <- convertSvgImage image
        model
        , None
    | ClearScene ->
        mainViewModel.PenLines.Clear()
        model
        , None
    | SetPosition (playerId, position) ->
        updatePlayerPosition playerId (fun _ -> position)
        , None
    | ChangePosition (playerId, position) ->
        updatePlayerPosition playerId (fun p -> p + position)
        , None
    | SetDirection (playerId, direction) ->
        updatePlayer playerId (fun p -> p.Angle <- Degrees.value direction)
        , None
    | ChangeDirection (playerId, direction) ->
        updatePlayer playerId (fun p -> p.Angle <- Degrees.op_Implicit p.Angle + direction |> Degrees.value)
        , None
    | SetSpeechBubble (playerId, speechBubble) ->
        updatePlayer playerId (fun p -> p.SpeechBubble <- getSpeechBubbleViewModel speechBubble)
        , None
    | SetPenState (playerId, isOn) ->
        updatePen playerId (fun p -> { p with IsOn = isOn })
        , None
    | TogglePenState playerId ->
        updatePen playerId (fun p -> { p with IsOn = not p.IsOn })
        , None
    | SetPenColor (playerId, color) ->
        updatePen playerId (fun p -> { p with Color = color })
        , None
    | ShiftPenColor (playerId, angle) ->
        updatePen playerId (fun p -> { p with Color = Color.hueShift angle p.Color })
        , None
    | SetPenWeight (playerId, weight) ->
        updatePen playerId (fun p -> { p with Weight = weight })
        , None
    | ChangePenWeight (playerId, weight) ->
        updatePen playerId (fun p -> { p with Weight = p.Weight + weight })
        , None
    | SetSizeFactor (playerId, sizeFactor) ->
        updateSizeFactor playerId (fun _ -> sizeFactor)
        , None
    | ChangeSizeFactor (playerId, value) ->
        updateSizeFactor playerId (fun sizeFactor -> sizeFactor + value)
        , None
    | SetNextCostume playerId ->
        Map.tryFind playerId model.Players
        |> Option.map (fun playerData ->
            let costumes = OneOfMany.next playerData.Costumes
            let costume = OneOfMany.current costumes
            let model = updatePlayer playerId (fun player ->
                player.Image <- new Svg.Skia.SvgImage(Source = costume.Image) :> IImage
                player.Size <- costume.Size * playerData.SizeFactor
            )
            { model with Players = Map.add playerId { playerData with Costumes = costumes } model.Players }
        )
        |> Option.defaultValue model
        , None
    | SendToBack playerId ->
        sendToBack playerId mainViewModel.Players
        model
        , None
    | BringToFront playerId ->
        bringToFront playerId mainViewModel.Players
        model
        , None
    | SetVisibility (playerId, isVisible) ->
        updatePlayer playerId (fun p -> p.IsVisible <- isVisible)
        , None
    | ToggleVisibility playerId ->
        updatePlayer playerId (fun p -> p.IsVisible <- not p.IsVisible)
        , None
    | CaptureScene ->
        Avalonia.Threading.Dispatcher.UIThread.RunJobs(Avalonia.Threading.DispatcherPriority.Render)
        let image = captureScene ()
        model, Some (CapturedScene image)
    | StartBatch ->
        { model with Batching = { model.Batching with Level = model.Batching.Level + 1 } }
        , None
    | ApplyBatch when model.Batching.Level > 1 ->
        { model with Batching = { model.Batching with Level = model.Batching.Level - 1 } }
        , None
    | ApplyBatch ->
        (model.Batching.Messages, { model with Batching = Batching.zero })
        ||> List.foldBack (fun msg model -> processControllerMessageDirectly mainViewModel msg model |> fst)
        , None

let private processControllerMessage (mainViewModel: MainWindowViewModel) model msg =
    match msg with
    | CaptureScene
    | StartBatch
    | ApplyBatch ->
        processControllerMessageDirectly mainViewModel msg model
    | msg ->
        if model.Batching.Level = 0 then
            processControllerMessageDirectly mainViewModel msg model
        else
            { model with Batching = { model.Batching with Messages = msg :: model.Batching.Messages } }
            , None

let private tryGetKeyboardKey keyCode =
    match keyCode with
    | SharpHook.Native.KeyCode.VcSpace -> Some Space
    | SharpHook.Native.KeyCode.VcEscape -> Some Escape
    | SharpHook.Native.KeyCode.VcEnter -> Some Enter
    | SharpHook.Native.KeyCode.VcUp -> Some Up
    | SharpHook.Native.KeyCode.VcDown -> Some Down
    | SharpHook.Native.KeyCode.VcLeft -> Some Left
    | SharpHook.Native.KeyCode.VcRight -> Some Right
    | SharpHook.Native.KeyCode.VcA -> Some A
    | SharpHook.Native.KeyCode.VcB -> Some B
    | SharpHook.Native.KeyCode.VcC -> Some C
    | SharpHook.Native.KeyCode.VcD -> Some D
    | SharpHook.Native.KeyCode.VcE -> Some E
    | SharpHook.Native.KeyCode.VcF -> Some F
    | SharpHook.Native.KeyCode.VcG -> Some G
    | SharpHook.Native.KeyCode.VcH -> Some H
    | SharpHook.Native.KeyCode.VcI -> Some I
    | SharpHook.Native.KeyCode.VcJ -> Some J
    | SharpHook.Native.KeyCode.VcK -> Some K
    | SharpHook.Native.KeyCode.VcL -> Some L
    | SharpHook.Native.KeyCode.VcM -> Some M
    | SharpHook.Native.KeyCode.VcN -> Some N
    | SharpHook.Native.KeyCode.VcO -> Some O
    | SharpHook.Native.KeyCode.VcP -> Some P
    | SharpHook.Native.KeyCode.VcQ -> Some Q
    | SharpHook.Native.KeyCode.VcR -> Some R
    | SharpHook.Native.KeyCode.VcS -> Some S
    | SharpHook.Native.KeyCode.VcT -> Some T
    | SharpHook.Native.KeyCode.VcU -> Some U
    | SharpHook.Native.KeyCode.VcV -> Some V
    | SharpHook.Native.KeyCode.VcW -> Some W
    | SharpHook.Native.KeyCode.VcX -> Some X
    | SharpHook.Native.KeyCode.VcY -> Some Y
    | SharpHook.Native.KeyCode.VcZ -> Some Z
    | SharpHook.Native.KeyCode.Vc0 -> Some Digit0
    | SharpHook.Native.KeyCode.Vc1 -> Some Digit1
    | SharpHook.Native.KeyCode.Vc2 -> Some Digit2
    | SharpHook.Native.KeyCode.Vc3 -> Some Digit3
    | SharpHook.Native.KeyCode.Vc4 -> Some Digit4
    | SharpHook.Native.KeyCode.Vc5 -> Some Digit5
    | SharpHook.Native.KeyCode.Vc6 -> Some Digit6
    | SharpHook.Native.KeyCode.Vc7 -> Some Digit7
    | SharpHook.Native.KeyCode.Vc8 -> Some Digit8
    | SharpHook.Native.KeyCode.Vc9 -> Some Digit9
    | _ -> None

let run scheduler (mainWindow: Window) (mainViewModel: MainWindowViewModel) (serverMessages: ISubject<_, _>) =
    [
        mainViewModel.WhenAnyValue(fun p -> p.IsLoaded)
        |> Observable.firstIf id
        |> Observable.switchMap (fun _ -> mainViewModel.WhenAnyValue(fun p -> p.SceneBounds))
        |> Observable.map(fun v -> UIMsg (SetSceneBounds v))

        mainViewModel.Players.ToObservableChangeSet<_>().ToCollection()
        |> Observable.switchMap (fun players ->
            players
            |> Seq.map (fun player ->
                player.WhenAnyValue(fun p -> p.SpeechBubble)
                |> Observable.filter (not << isNull)
                |> Observable.map (function
                    | :? AskTextSpeechBubbleViewModel as v -> v.ConfirmCommand |> Observable.first |> Observable.map (fun answer -> UIMsg (AnswerStringQuestion(player.Id, answer)))
                    | :? AskBoolSpeechBubbleViewModel as v -> v.ConfirmCommand |> Observable.first |> Observable.map (fun answer -> UIMsg (AnswerBoolQuestion(player.Id, answer)))
                    | _ -> Observable.empty
                )
                |> Observable.mergeInner
            )
            |> Observable.mergeSeq
        )

        Observable.fromEventHandler mainWindow.Loaded.AddHandler mainWindow.Loaded.RemoveHandler
        |> Observable.map (fun _e ->
            let x, y = InputEvents.getCurrentMousePosition ()
            let clientPoint = mainWindow.PointToClient(PixelPoint(int x, int y))
            let position = { X = mainViewModel.SceneBounds.Left + clientPoint.X; Y = mainViewModel.SceneBounds.Top - clientPoint.Y }
            UIMsg (MouseMove position)
        )

        Observable.fromEventHandler mainWindow.Loaded.AddHandler mainWindow.Loaded.RemoveHandler
        |> Observable.first
        |> Observable.bind (fun e ->
            System.Reactive.Linq.Observable.Create(fun (obs: IObserver<_>) (ct: CancellationToken) ->
                task {
                    use hook = new SharpHook.SimpleGlobalHook()
                    use _ = ct.Register (fun () -> hook.Dispose())
                    use _ = hook.MouseClicked.Subscribe (fun e ->
                        let button =
                            if e.Data.Button = SharpHook.Native.MouseButton.Button1 then Some Primary
                            elif e.Data.Button = SharpHook.Native.MouseButton.Button2 then Some Secondary
                            else None
                        match button with
                        | Some button ->
                            let clientPoint = mainWindow.PointToClient(PixelPoint(int e.Data.X, int e.Data.Y))
                            let position = { X = mainViewModel.SceneBounds.Left + clientPoint.X; Y = mainViewModel.SceneBounds.Top - clientPoint.Y }
                            obs.OnNext (UIMsg (MouseClick { Button = button; Position = position }))
                        | None -> ()
                    )
                    use _ = hook.MouseMoved.Subscribe (fun e ->
                        let screenPoint = PixelPoint(int e.Data.X, int e.Data.Y)
                        let clientPoint = mainWindow.PointToClient screenPoint
                        let position = { X = mainViewModel.SceneBounds.Left + clientPoint.X; Y = mainViewModel.SceneBounds.Top - clientPoint.Y }
                        obs.OnNext (UIMsg (MouseMove position))
                    )
                    use _ = hook.KeyPressed.Subscribe (fun v ->
                        match tryGetKeyboardKey v.Data.KeyCode with
                        | Some key -> obs.OnNext (UIMsg (KeyDown key))
                        | None -> ()
                    )
                    use _ = hook.KeyReleased.Subscribe (fun v ->
                        match tryGetKeyboardKey v.Data.KeyCode with
                        | Some key -> obs.OnNext (UIMsg (KeyUp key))
                        | None -> ()
                    )
                    do! hook.RunAsync()
                } :> Task
            )
        )

        serverMessages
        |> Observable.choose Result.toOption
        |> Observable.observeOn scheduler
        |> Observable.scanInit (Model.zero, None) (fun (model, _) msg ->
            match msg with
            | ControllerMsg controllerMessage ->
                let model, responseMessage = processControllerMessage mainViewModel model controllerMessage
                model, (responseMessage |> Option.map UIMsg)
        )
        |> Observable.choose snd
    ]
    |> Observable.mergeSeq
    |> Observable.subscribeObserver serverMessages
