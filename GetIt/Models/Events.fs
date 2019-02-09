namespace GetIt

type MouseState =
    { Position: Position }

module internal MouseState =
    let empty = { Position = Position.zero }

type KeyboardKey =
    | Space
    | Escape
    | Up
    | Down
    | Left
    | Right
    | A
    | B
    | C
    | D
    | E
    | F
    | G
    | H
    | I
    | J
    | K
    | L
    | M
    | N
    | O
    | P
    | Q
    | R
    | S
    | T
    | U
    | V
    | W
    | X
    | Y
    | Z
    | Digit0
    | Digit1
    | Digit2
    | Digit3
    | Digit4
    | Digit5
    | Digit6
    | Digit7
    | Digit8
    | Digit9

type KeyboardState =
    { KeysPressed: KeyboardKey list }

module internal KeyboardState =
    let empty = { KeysPressed = [] }

type MouseButton =
    | Left
    | Middle
    | Right

type EventHandler =
    | KeyDown of key: KeyboardKey option * handler: (KeyboardKey -> unit)
    | ClickScene of handler: (Position -> MouseButton -> unit)
    | ClickPlayer of playerId: PlayerId * handler: (unit -> unit)
    | MouseEnterPlayer of playerId: PlayerId * handler: (unit -> unit)

type Event =
    | KeyDown of KeyboardKey
    | KeyUp of KeyboardKey
    | ClickScene of Position * MouseButton
    | ClickPlayer of PlayerId
    | MouseEnterPlayer of PlayerId

