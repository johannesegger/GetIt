namespace GetIt

open System
open System.Reactive.Concurrency
open System.Reactive.Linq
open System.Reactive.Subjects
open FSharp.Control.Reactive

type internal Model =
    {
        SceneBounds: Rectangle
        Players: Map<PlayerId, PlayerData>
        MouseState: MouseState
        KeyboardState: KeyboardState
    }

module internal Model =
    let private gate = Object()

    let mutable private subject =
        let initial =
            {
                SceneBounds = Rectangle.zero
                Players = Map.empty
                MouseState = MouseState.empty
                KeyboardState = KeyboardState.empty
            }
        new BehaviorSubject<_>(initial)

    let observable = subject.AsObservable()

    let getCurrent () = subject.Value

    let updateCurrent fn =
        lock gate (fun () -> subject.OnNext(fn subject.Value))

    let updatePlayer playerId fn =
        updateCurrent (fun m ->
            let player = Map.find playerId m.Players |> fn
            { m with Players = Map.add playerId player m.Players }
        )

    let private keyDownFilter filter =
        observable
        |> Observable.map (fun model ->
            let hasActiveTextInput =
                model.Players
                |> Map.exists (fun playerId player ->
                    match player.SpeechBubble with
                    | Some (AskString _) -> true
                    | Some (AskBool _)
                    | Some (Say _)
                    | None -> false
                )
            hasActiveTextInput, model.KeyboardState.KeysPressed
        )
        |> Observable.pairwise
        |> Observable.choose (fun ((_, keysPressedOld), (hasActiveTextInput, keysPressedNew)) ->
            match hasActiveTextInput, Set.difference keysPressedNew keysPressedOld |> filter with
            | true, _ -> None
            | false, result -> result
        )

    let private onKeyDownFilter filter handler =
        keyDownFilter filter
        |> Observable.observeOn ThreadPoolScheduler.Instance
        |> Observable.subscribe handler

    let onKeyDown key =
        onKeyDownFilter (Set.contains key >> function true -> Some () | false -> None)

    let onAnyKeyDown =
        onKeyDownFilter (Set.toSeq >> Seq.tryHead)

    let private whileKeyDownFilter filter interval handler =
        keyDownFilter filter
        |> Observable.switchMap (fun key ->
            let keyUpObservable =
                observable
                |> Observable.choose (fun model ->
                    if Set.contains key model.KeyboardState.KeysPressed then None
                    else Some ()
                )
                |> Observable.take 1
            Observable.interval interval
            |> Observable.map (int >> (+) 2)
            |> Observable.startWith [ 1 ]
            |> Observable.map (fun i -> key, i)
            |> Observable.takeUntilOther keyUpObservable
        )
        |> Observable.repeat
        |> Observable.observeOn ThreadPoolScheduler.Instance
        |> Observable.subscribe handler

    let whileKeyDown key interval handler =
        whileKeyDownFilter (Set.contains key >> function true -> Some key | false -> None) interval (snd >> handler)

    let whileAnyKeyDown interval handler =
        whileKeyDownFilter (Set.toSeq >> Seq.tryHead) interval (uncurry handler)

    let onClickScene fn =
        observable
        |> Observable.choose (fun model ->
            match model.MouseState.LastClick with
            | Some (eventId, mouseClickEvent) when Rectangle.contains mouseClickEvent.Position model.SceneBounds ->
                Some (eventId, mouseClickEvent)
            | _ -> None
        )
        |> Observable.distinctUntilChangedKey fst
        |> Observable.map snd
        |> Observable.observeOn ThreadPoolScheduler.Instance
        |> Observable.subscribe fn

    let onClickPlayer playerId fn =
        observable
        |> Observable.choose (fun model ->
            match model.MouseState.LastClick, Map.tryFind playerId model.Players with
            | Some (eventId, mouseClickEvent), Some player when Rectangle.contains mouseClickEvent.Position player.Bounds ->
                Some (eventId, mouseClickEvent)
            | _ -> None
        )
        |> Observable.distinctUntilChanged
        |> Observable.map snd
        |> Observable.observeOn ThreadPoolScheduler.Instance
        |> Observable.subscribe fn

    let onEnterPlayer playerId fn =
        observable
        |> Observable.map (fun model -> Map.tryFind playerId model.Players, model.MouseState.Position)
        |> Observable.pairwise
        |> Observable.choose (function
            | (Some p1, mousePosition1), (Some p2, mousePosition2) ->
                let hasBeenEntered =
                    not (Rectangle.contains mousePosition1 p1.Bounds) &&
                    Rectangle.contains mousePosition2 p2.Bounds
                if hasBeenEntered then Some ()
                else None
            | _ -> None
        )
        |> Observable.observeOn ThreadPoolScheduler.Instance
        |> Observable.subscribe fn
