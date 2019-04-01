namespace GetIt

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
    { KeysPressed: Set<KeyboardKey> }

module KeyboardState =
    let empty = { KeysPressed = Set.empty }

type MouseButton =
    | Primary
    | Secondary

type MouseClickEvent =
    { Position: Position
      MouseButton: MouseButton }

type MouseState =
    { Position: Position }

module MouseState =
    let empty = { Position = Position.zero }

type ControllerEvent =
    | KeyDown of KeyboardKey
    | KeyUp of KeyboardKey
    | MouseMove of Position
    | MouseClick of MouseButton * Position

type UIEvent =
    | SetMousePosition of Position
    | ApplyMouseClick of MouseButton * Position
    | SetSceneBounds of Rectangle
    | AnswerQuestion of PlayerId * string
