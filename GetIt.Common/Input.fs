namespace GetIt

/// Defines some common keys on a keyboard.
type KeyboardKey =
    | Space
    | Escape
    | Enter
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

type internal KeyboardState =
    {
        KeysPressed: Set<KeyboardKey>
    }

module internal KeyboardState =
    let empty = { KeysPressed = Set.empty }

/// Defines some common mouse button.
type MouseButton =
    | Primary
    | Secondary

/// Defines data of a mouse click event.
type MouseClick =
    {
        Button: MouseButton
        Position: Position
    }

type internal MouseState =
    {
        Position: Position
    }

module internal MouseState =
    let empty =
        {
            Position = Position.zero
        }
