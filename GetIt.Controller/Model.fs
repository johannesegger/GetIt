namespace GetIt

open System
open System.Reactive.Linq
open System.Reactive.Subjects
open FSharp.Control.Reactive

type internal EventHandler =
    | OnAnyKeyDown of handler: (KeyboardKey -> unit)
    | OnKeyDown of key: KeyboardKey * handler: (unit -> unit)
    | OnClickScene of handler: (Position -> MouseButton -> unit)
    | OnClickPlayer of playerId: PlayerId * handler: (MouseButton -> unit)
    | OnMouseEnterPlayer of playerId: PlayerId * handler: (unit -> unit)

type internal Model =
    { SceneBounds: Rectangle
      Players: Map<PlayerId, PlayerData>
      MouseState: MouseState
      KeyboardState: KeyboardState
      EventHandlers: (Guid * EventHandler) list }

module internal Model =
    let private gate = Object()

    let mutable private subject =
        let initial =
            { SceneBounds = Rectangle.zero
              Players = Map.empty
              MouseState = MouseState.empty
              KeyboardState = KeyboardState.empty
              EventHandlers = [] }
        new BehaviorSubject<_>(initial)

    let observable = subject.AsObservable()

    let getCurrent () = subject.Value

    let updateCurrent fn =
        lock gate (fun () -> subject.OnNext(fn subject.Value))

    let addEventHandler eventHandler =
        let eventHandlerId = Guid.NewGuid()
        updateCurrent (fun model -> { model with EventHandlers = (eventHandlerId, eventHandler) :: model.EventHandlers })
        Disposable.create (fun () ->
            updateCurrent (fun model ->
                { model with EventHandlers = model.EventHandlers |> List.filter (fst >> (<>) eventHandlerId) }
            )
        )