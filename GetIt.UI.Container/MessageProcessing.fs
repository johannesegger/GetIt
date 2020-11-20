module internal MessageProcessing

open DynamicData
open DynamicData.Binding
open FSharp.Control.Reactive
open GetIt
open GetIt.UI
open ReactiveUI
open SharpVectors.Converters
open SharpVectors.Renderers.Wpf
open System.IO
open System.Reactive.Subjects
open System.Windows
open System.Windows.Media
open Thoth.Json.Net

type PlayerData = {
    SizeFactor: float
    Costumes: OneOfMany<SvgImage>
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
    let svgConverterSettings = WpfDrawingSettings(IncludeRuntime = true, TextAsGeometry = false)
    use converter = new FileSvgReader(svgConverterSettings)
    use svgReader = new StringReader(image.SvgData)
    let costume = converter.Read(svgReader)
    DrawingImage(costume)

let rec private processControllerMessageDirectly (mainViewModel: MainViewModel) msg model =
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
                mainViewModel.AddPenLine(p.Position, newPosition, pen.Weight, SolidColorBrush(Color.FromArgb(pen.Color.Alpha, pen.Color.Red, pen.Color.Green, pen.Color.Blue)))
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
            let image = OneOfMany.current playerData.Costumes
            let model = updatePlayer playerId (fun player -> player.Size <- image.Size * sizeFactor)
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
    match msg with
    | AddPlayer (playerId, playerData) ->
        mainViewModel.AddPlayer(playerId, fun p ->
            p.Image <- convertSvgImage playerData.Costume
            p.Size <- playerData.Size
            p.Position <- playerData.Position
            p.Angle <- Degrees.value playerData.Direction
            p.Visibility <- if playerData.IsVisible then Visibility.Visible else Visibility.Collapsed
            p.SpeechBubble <- getSpeechBubbleViewModel playerData.SpeechBubble
        )
        sendToBack playerId mainViewModel.Players
        let playerData = {
            SizeFactor = playerData.SizeFactor
            Costumes = playerData.Costumes
            Pen = playerData.Pen
        }
        { model with Players = Map.add playerId playerData model.Players }
    | RemovePlayer playerId ->
        mainViewModel.Players
        |> Seq.filter (fun p -> p.Id = playerId)
        |> mainViewModel.Players.RemoveMany
        { model with Players = Map.remove playerId model.Players }
    | SetWindowTitle None ->
        mainViewModel.Title <- "Get It"
        model
    | SetWindowTitle (Some title) ->
        mainViewModel.Title <- sprintf "Get It - %s" title
        model
    | SetBackground image ->
        mainViewModel.BackgroundImage <- convertSvgImage image
        model
    | ClearScene ->
        mainViewModel.PenLines.Clear()
        model
    | SetPosition (playerId, position) ->
        updatePlayerPosition playerId (fun _ -> position)
    | ChangePosition (playerId, position) ->
        updatePlayerPosition playerId (fun p -> p + position)
    | SetDirection (playerId, direction) ->
        updatePlayer playerId (fun p -> p.Angle <- Degrees.value direction)
    | ChangeDirection (playerId, direction) ->
        updatePlayer playerId (fun p -> p.Angle <- Degrees.op_Implicit p.Angle + direction |> Degrees.value)
    | SetSpeechBubble (playerId, speechBubble) ->
        updatePlayer playerId (fun p -> p.SpeechBubble <- getSpeechBubbleViewModel speechBubble)
    | SetPenState (playerId, isOn) ->
        updatePen playerId (fun p -> { p with IsOn = isOn })
    | TogglePenState playerId ->
        updatePen playerId (fun p -> { p with IsOn = not p.IsOn })
    | SetPenColor (playerId, color) ->
        updatePen playerId (fun p -> { p with Color = color })
    | ShiftPenColor (playerId, angle) ->
        updatePen playerId (fun p -> { p with Color = Color.hueShift angle p.Color })
    | SetPenWeight (playerId, weight) ->
        updatePen playerId (fun p -> { p with Weight = weight })
    | ChangePenWeight (playerId, weight) ->
        updatePen playerId (fun p -> { p with Weight = p.Weight + weight })
    | SetSizeFactor (playerId, sizeFactor) ->
        updateSizeFactor playerId (fun _ -> sizeFactor)
    | ChangeSizeFactor (playerId, value) ->
        updateSizeFactor playerId (fun sizeFactor -> sizeFactor + value)
    | SetNextCostume playerId ->
        Map.tryFind playerId model.Players
        |> Option.map (fun playerData ->
            let costumes = OneOfMany.next playerData.Costumes
            let costume = OneOfMany.current costumes
            let model = updatePlayer playerId (fun player ->
                player.Image <- convertSvgImage costume
                player.Size <- costume.Size * playerData.SizeFactor
            )
            { model with Players = Map.add playerId { playerData with Costumes = costumes } model.Players }
        )
        |> Option.defaultValue model
    | SendToBack playerId ->
        sendToBack playerId mainViewModel.Players
        model
    | BringToFront playerId ->
        bringToFront playerId mainViewModel.Players
        model
    | SetVisibility (playerId, isVisible) ->
        updatePlayer playerId (fun p -> p.Visibility <- if isVisible then Visibility.Visible else Visibility.Collapsed)
    | ToggleVisibility playerId ->
        updatePlayer playerId (fun p -> p.Visibility <- if p.Visibility = Visibility.Visible then Visibility.Collapsed else Visibility.Visible)
    | StartBatch ->
        { model with Batching = { model.Batching with Level = model.Batching.Level + 1 } }
    | ApplyBatch when model.Batching.Level > 1 ->
        { model with Batching = { model.Batching with Level = model.Batching.Level - 1 } }
    | ApplyBatch ->
        printfn "Applying messages: %A" model.Batching.Messages
        (model.Batching.Messages, { model with Batching = Batching.zero })
        ||> List.foldBack (processControllerMessageDirectly mainViewModel)

let private processControllerMessage (mainViewModel: MainViewModel) model msg =
    match msg with
    | StartBatch
    | ApplyBatch ->
        processControllerMessageDirectly mainViewModel msg model
    | msg ->
        if model.Batching.Level = 0 then
            processControllerMessageDirectly mainViewModel msg model
        else
            { model with Batching = { model.Batching with Messages = msg :: model.Batching.Messages } }

let run scheduler (mainViewModel: MainViewModel) (messageSubject: ISubject<_, _>) =
    let (encode, decoder) = Encode.Auto.generateEncoder(), Decode.Auto.generateDecoder()
    [
        mainViewModel.WhenAnyValue(fun p -> p.SceneSize)
        |> Observable.map(fun v -> UIMsg (SetSceneBounds({ Position = { X = -v.Width / 2.; Y = -v.Height / 2. }; Size = v })))

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

        messageSubject
        |> Observable.choose (fun message ->
            match Decode.fromString decoder message with
            | Ok message -> Some message
            | Error p ->
                #if DEBUG
                eprintfn "Deserializing message failed: %s, Message: %s" p message
                #endif
                None
        )
        |> Observable.observeOn scheduler
        |> Observable.scanInit (Model.zero, None) (fun (model, _) msg ->
            match msg with
            | ControllerMsg (msgId, controllerMessage) ->
                let model = processControllerMessage mainViewModel model controllerMessage
                model, Some (ControllerMsgConfirmation msgId)
        )
        |> Observable.choose snd
    ]
    |> Observable.mergeSeq
    |> Observable.map (encode >> Encode.toString 0)
    |> Observable.subscribeObserver messageSubject
