namespace GetIt

open System

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

type KeyboardState =
    {
        KeysPressed: Set<KeyboardKey>
    }

module KeyboardState =
    let empty = { KeysPressed = Set.empty }

type MouseButton =
    | Primary
    | Secondary

type MouseClick =
    {
        Button: MouseButton
        Position: Position
    }

type VirtualScreenMouseClick =
    {
        Button: MouseButton
        VirtualScreenPosition: Position
    }

type MouseState =
    {
        Position: Position
        LastClick: (Guid * MouseClick) option
    }

module MouseState =
    let empty =
        {
            Position = Position.zero
            LastClick = None
        }

type ControllerEvent =
    | KeyDown of KeyboardKey
    | KeyUp of KeyboardKey
    | MouseMove of virtualScreenPosition: Position
    | MouseClick of VirtualScreenMouseClick

type PngImage = PngImage of byte[]

module PngImage =
    let toBase64String (PngImage data) =
        Convert.ToBase64String data
        |> sprintf "data:image/png;base64, %s"
