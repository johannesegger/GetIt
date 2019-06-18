namespace GetIt

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

type ControllerMsg =
    | AddPlayer of PlayerId * PlayerData
    | RemovePlayer of PlayerId

type UIMsg =
    | SetSceneBounds of Rectangle
    | ApplyMouseClick of MouseClick

type ChannelMsg =
    | UIMsg of UIMsg
    | ControllerMsg of ControllerMsg

module Decode =
    let position : Decoder<_> =
        Decode.object (fun get ->
            {
                X = get.Required.Field "x" Decode.float
                Y = get.Required.Field "y" Decode.float
            }
        )

    let degrees : Decoder<_> =
        Decode.float
        |> Decode.map Degrees.op_Implicit

    let rgba : Decoder<_> =
        Decode.object (fun get ->
            {
                Red = get.Required.Field "red" Decode.int |> byte
                Green = get.Required.Field "green" Decode.int |> byte
                Blue = get.Required.Field "blue" Decode.int |> byte
                Alpha = get.Required.Field "alpha" Decode.int |> byte
            }
        )

    let pen : Decoder<_> =
        Decode.object (fun get ->
            {
                IsOn = get.Required.Field "isOn" Decode.bool
                Weight = get.Required.Field "weight" Decode.float
                Color = get.Required.Field "color" rgba
            }
        )

    let speechBubble : Decoder<_> =
        Decode.oneOf [
            Decode.field "say" Decode.string |> Decode.map Say
            Decode.field "askString" Decode.string |> Decode.map AskString
            Decode.field "askBool" Decode.string |> Decode.map AskBool
        ]

    let optionalSpeechBubble : Decoder<_> =
        Decode.option speechBubble

    let size : Decoder<_> =
        Decode.object (fun get ->
            {
                Width = get.Required.Field "width" Decode.float
                Height = get.Required.Field "height" Decode.float
            }
        )

    let windowSize : Decoder<_> =
        Decode.oneOf [
            Decode.field "specificSize" size |> Decode.map SpecificSize
            Decode.field "maximized" (Decode.nil Maximized)
        ]

    let svgImage : Decoder<_> =
        Decode.object (fun get ->
            {
                Size = get.Required.Field "size" size
                SvgData = get.Required.Field "svgData" Decode.string
            }
        )

    let playerData : Decoder<_> =
        Decode.object (fun get ->
            {
                SizeFactor = get.Required.Field "sizeFactor" Decode.float
                Position = get.Required.Field "position" position
                Direction = get.Required.Field "direction" degrees
                Pen = get.Required.Field "pen" pen
                SpeechBubble = get.Required.Field "speechBubble" optionalSpeechBubble
                Costumes = get.Required.Field "costumes" (Decode.list svgImage)
                CostumeIndex = get.Required.Field "costumeIndex" Decode.int
                Layer = get.Required.Field "layer" Decode.int
                IsVisible = get.Required.Field "isVisible" Decode.bool
            }
        )

    let playerId: Decoder<_> =
        Decode.guid
        |> Decode.map PlayerId

    let rectangle: Decoder<_> =
        Decode.object (fun get ->
            {
                Position = get.Required.Field "position" position
                Size = get.Required.Field "size" size
            }
        )

    let KeyboardKey: Decoder<_> =
        Decode.string
        |> Decode.andThen (fun key ->
            match key with
            | "space" -> Decode.succeed Space
            | "escape" -> Decode.succeed Escape
            | "enter" -> Decode.succeed Enter
            | "up" -> Decode.succeed Up
            | "down" -> Decode.succeed Down
            | "left" -> Decode.succeed Left
            | "right" -> Decode.succeed Right
            | "a" -> Decode.succeed A
            | "b" -> Decode.succeed B
            | "c" -> Decode.succeed C
            | "d" -> Decode.succeed D
            | "e" -> Decode.succeed E
            | "f" -> Decode.succeed F
            | "g" -> Decode.succeed G
            | "h" -> Decode.succeed H
            | "i" -> Decode.succeed I
            | "j" -> Decode.succeed J
            | "k" -> Decode.succeed K
            | "l" -> Decode.succeed L
            | "m" -> Decode.succeed M
            | "n" -> Decode.succeed N
            | "o" -> Decode.succeed O
            | "p" -> Decode.succeed P
            | "q" -> Decode.succeed Q
            | "r" -> Decode.succeed R
            | "s" -> Decode.succeed S
            | "t" -> Decode.succeed T
            | "u" -> Decode.succeed U
            | "v" -> Decode.succeed V
            | "w" -> Decode.succeed W
            | "x" -> Decode.succeed X
            | "y" -> Decode.succeed Y
            | "z" -> Decode.succeed Z
            | "0" -> Decode.succeed Digit0
            | "1" -> Decode.succeed Digit1
            | "2" -> Decode.succeed Digit2
            | "3" -> Decode.succeed Digit3
            | "4" -> Decode.succeed Digit4
            | "5" -> Decode.succeed Digit5
            | "6" -> Decode.succeed Digit6
            | "7" -> Decode.succeed Digit7
            | "8" -> Decode.succeed Digit8
            | "9" -> Decode.succeed Digit9
            | x -> Decode.fail (sprintf "Can't decode \"%s\" as keyboard key" x)
        )

    let mouseButton: Decoder<_> =
        Decode.string
        |> Decode.andThen (fun key ->
            match key with
            | "primary" -> Decode.succeed Primary
            | "secondary" -> Decode.succeed Secondary
            | x -> Decode.fail (sprintf "Can't decode \"%s\" as mouse button" x)
        )

    let mouseClick: Decoder<_> =
        Decode.object (fun get ->
            {
                Button = get.Required.Field "button" mouseButton
                Position = get.Required.Field "position" position
            }
        )

    let controllerMsg: Decoder<_> =
        let decoders =
            [
                ("addPlayer", Decode.tuple2 playerId playerData |> Decode.map AddPlayer)
                ("removePlayer", playerId |> Decode.map RemovePlayer)
                // ("setWindowTitle", Decode.option Decode.string |> Decode.map SetWindowTitle)
                // ("setBackground", svgImage |> Decode.map SetBackground)
                // ("clearScene", Decode.nil ClearScene)
                // ("makeScreenshot", Decode.nil MakeScreenshot)
                // ("setPosition", Decode.tuple2 playerId position |> Decode.map SetPosition)
                // ("setDirection", Decode.tuple2 playerId degrees |> Decode.map SetDirection)
                // ("setSpeechBubble", Decode.tuple2 playerId optionalSpeechBubble |> Decode.map SetSpeechBubble)
                // ("setPen", Decode.tuple2 playerId pen |> Decode.map SetPen)
                // ("setSizeFactor", Decode.tuple2 playerId Decode.float |> Decode.map SetSizeFactor)
                // ("setNextCostume", playerId |> Decode.map SetNextCostume)
                // ("sendToBack", playerId |> Decode.map SendToBack)
                // ("bringToFront", playerId |> Decode.map BringToFront)
                // ("keyDown", keyboardKey |> Decode.map (KeyDown >> ControllerEvent))
                // ("keyUp", keyboardKey |> Decode.map (KeyUp >> ControllerEvent))
                // ("mouseMove", position |> Decode.map (MouseMove >> ControllerEvent))
                // ("mouseClick", Decode.tuple2 mouseButton position |> Decode.map (MouseClick >> ControllerEvent))
                // ("startBatch", Decode.nil StartBatch)
                // ("applyBatch", Decode.nil ApplyBatch)
            ]
            |> List.map (fun (key, decoder) ->
                Decode.field key decoder
            )

        Decode.oneOf decoders

    let uiMsg: Decoder<_> =
        let decoders =
            [
                ("setSceneBounds", rectangle |> Decode.map SetSceneBounds)
                ("applyMouseClick", mouseClick |> Decode.map ApplyMouseClick)
                // ("setMousePosition", position |> Decode.map (SetMousePosition >> UIEvent))
                // ("answerQuestion", Decode.tuple2 playerId Decode.string |> Decode.map (AnswerQuestion >> UIEvent))
                // ("screenshot", Decode.string |> Decode.map (Convert.FromBase64String >> PngImage >> Screenshot >> UIEvent))
            ]
            |> List.map (fun (key, decoder) ->
                Decode.field key decoder
            )

        Decode.oneOf decoders

    let channelMsg: Decoder<_> =
        let decoders =
            [
                ("controllerMsg", controllerMsg |> Decode.map ControllerMsg)
                ("uiMsg", uiMsg |> Decode.map UIMsg)
            ]
            |> List.map (fun (key, decoder) ->
                Decode.field key decoder
            )

        Decode.oneOf decoders

module Encode =
    let playerId (PlayerId p) = Encode.guid p

    let position p =
        Encode.object [
            ("x", Encode.float p.X)
            ("y", Encode.float p.Y)
        ]

    let degrees (Degrees p) = Encode.float p

    let rgba p =
        Encode.object [
            ("red", Encode.int (int p.Red))
            ("green", Encode.int (int p.Green))
            ("blue", Encode.int (int p.Blue))
            ("alpha", Encode.int (int p.Alpha))
        ]

    let pen p =
        Encode.object [
            ("isOn", Encode.bool p.IsOn)
            ("weight", Encode.float p.Weight)
            ("color", rgba p.Color)
        ]

    let speechBubble p =
        match p with
        | Say text -> Encode.object [ ("say", Encode.string text) ]
        | AskString text -> Encode.object [ ("askString", Encode.string text) ]
        | AskBool text -> Encode.object [ ("askBool", Encode.string text) ]

    let optionalSpeechBubble p =
        match p with
        | Some data -> speechBubble data
        | None -> Encode.nil

    let size p =
        Encode.object [
            ("width", Encode.float p.Width)
            ("height", Encode.float p.Height)
        ]

    let windowSize p =
        match p with
        | SpecificSize p -> Encode.object [ ("specificSize", size p) ]
        | Maximized -> Encode.object [ ("maximized", Encode.nil) ]

    let svgImage p =
        Encode.object [
            ("size", size p.Size)
            ("svgData", Encode.string p.SvgData)
        ]

    let playerData p =
        Encode.object [
            ("sizeFactor", Encode.float p.SizeFactor)
            ("position", position p.Position)
            ("direction", degrees p.Direction)
            ("pen", pen p.Pen)
            ("speechBubble", optionalSpeechBubble p.SpeechBubble)
            ("costumes", Encode.list (List.map svgImage p.Costumes))
            ("costumeIndex", Encode.int p.CostumeIndex)
            ("layer", Encode.int p.Layer)
            ("isVisible", Encode.bool p.IsVisible)
        ]

    let rectangle (p: Rectangle) =
        Encode.object [
            ("position", position p.Position)
            ("size", size p.Size)
        ]

    let keyboardKey p =
        match p with
        | Space -> "space"
        | Escape -> "escape"
        | Enter -> "enter"
        | Up -> "up"
        | Down -> "down"
        | Left -> "left"
        | Right -> "right"
        | A -> "a"
        | B -> "b"
        | C -> "c"
        | D -> "d"
        | E -> "e"
        | F -> "f"
        | G -> "g"
        | H -> "h"
        | I -> "i"
        | J -> "j"
        | K -> "k"
        | L -> "l"
        | M -> "m"
        | N -> "n"
        | O -> "o"
        | P -> "p"
        | Q -> "q"
        | R -> "r"
        | S -> "s"
        | T -> "t"
        | U -> "u"
        | V -> "v"
        | W -> "w"
        | X -> "x"
        | Y -> "y"
        | Z -> "z"
        | Digit0 -> "0"
        | Digit1 -> "1"
        | Digit2 -> "2"
        | Digit3 -> "3"
        | Digit4 -> "4"
        | Digit5 -> "5"
        | Digit6 -> "6"
        | Digit7 -> "7"
        | Digit8 -> "8"
        | Digit9 -> "9"
        |> Encode.string

    let mouseButton p =
        match p with
        | Primary -> "primary"
        | Secondary -> "secondary"
        |> Encode.string

    let mouseClick (p: MouseClick) =
        Encode.object [
            ("button",  mouseButton p.Button)
            ("position",  position p.Position)
        ]

    let controllerMsg msg =
        match msg with
        | AddPlayer (pId, pData) ->
            Encode.object [ ("addPlayer", Encode.tuple2 playerId playerData (pId, pData)) ]
        | RemovePlayer pId ->
            Encode.object [ ("removePlayer", playerId pId) ]
        // | SetWindowTitle text ->
        //     Encode.object [ ("setWindowTitle", Encode.option Encode.string text) ]
        // | SetBackground background ->
        //     Encode.object [ ("setBackground", svgImage background) ]
        // | ClearScene ->
        //     Encode.object [ ("clearScene", Encode.nil) ]
        // | MakeScreenshot ->
        //     Encode.object [ ("makeScreenshot", Encode.nil) ]
        // | SetPosition (playerId, position) ->
        //     Encode.object [ ("setPosition", Encode.tuple2 playerId position (playerId, position)) ]
        // | SetDirection (playerId, direction) ->
        //     Encode.object [ ("setDirection", Encode.tuple2 playerId degrees (playerId, direction))]
        // | SetSpeechBubble (playerId, speechBubble) ->
        //     Encode.object [ ("setSpeechBubble", Encode.tuple2 playerId optionalSpeechBubble (playerId, speechBubble)) ]
        // | SetPen (playerId, pen) ->
        //     Encode.object [ ("setPen", Encode.tuple2 playerId pen (playerId, pen)) ]
        // | SetSizeFactor (playerId, sizeFactor) ->
        //     Encode.object [ ("setSizeFactor", Encode.tuple2 playerId Encode.float (playerId, sizeFactor)) ]
        // | SetNextCostume playerId ->
        //     Encode.object [ ("setNextCostume", playerId playerId) ]
        // | SendToBack playerId ->
        //     Encode.object [ ("sendToBack", playerId playerId) ]
        // | BringToFront playerId ->
        //     Encode.object [ ("bringToFront", playerId playerId) ]
        // | ControllerEvent (KeyDown keyboardKey) ->
        //     Encode.object [ ("keyDown", keyboardKey keyboardKey) ]
        // | ControllerEvent (KeyUp keyboardKey) ->
        //     Encode.object [ ("keyUp", keyboardKey keyboardKey) ]
        // | ControllerEvent (MouseMove position) ->
        //     Encode.object [ ("mouseMove", position position) ]
        // | ControllerEvent (MouseClick (mouseButton, position)) ->
        //     Encode.object [ ("mouseClick", Encode.tuple2 mouseButton position (mouseButton, position)) ]
        // | StartBatch ->
        //     Encode.object [ ("startBatch", Encode.nil) ]
        // | ApplyBatch ->
        //     Encode.object [ ("applyBatch", Encode.nil) ]

    let uIMsg msg =
        match msg with
        | SetSceneBounds p ->
            Encode.object [ ("setSceneBounds", rectangle p) ]
        | ApplyMouseClick p ->
            Encode.object [ ("applyMouseClick", mouseClick p) ]
        // | SetMousePosition position ->
        //     Encode.object [ ("setMousePosition", position position) ]
        // | AnswerQuestion (playerId, answer) ->
        //     Encode.object [ ("answerQuestion", Encode.tuple2 playerId Encode.string (playerId, answer)) ]
        // | Screenshot (PngImage data) ->
        //     Encode.object [ ("screenshot", Encode.string (Convert.ToBase64String data)) ]

    let channelMsg msg =
        match msg with
        | ControllerMsg msg ->
            Encode.object [ ("controllerMsg", controllerMsg msg) ]
        | UIMsg msg ->
            Encode.object [ ("uiMsg", uIMsg msg) ]
