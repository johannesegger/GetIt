namespace GetIt

open System
#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

type internal ControllerMsg =
    | AddPlayer of PlayerId * PlayerData
    | RemovePlayer of PlayerId
    | SetWindowTitle of string option
    | SetBackground of SvgImage
    | ClearScene
    | SetPosition of PlayerId * Position
    | ChangePosition of PlayerId * Position
    | SetDirection of PlayerId * Degrees
    | ChangeDirection of PlayerId * Degrees
    | SetSpeechBubble of PlayerId * SpeechBubble option
    | SetPenState of PlayerId * bool
    | TogglePenState of PlayerId
    | SetPenColor of PlayerId * RGBAColor
    | ShiftPenColor of PlayerId * Degrees
    | SetPenWeight of PlayerId * float
    | ChangePenWeight of PlayerId * float
    | SetSizeFactor of PlayerId * float
    | ChangeSizeFactor of PlayerId * float
    | SetNextCostume of PlayerId
    | SendToBack of PlayerId
    | BringToFront of PlayerId
    | SetVisibility of PlayerId * bool
    | ToggleVisibility of PlayerId
    | StartBatch
    | ApplyBatch

type internal UIMsg =
    | SetSceneBounds of Rectangle
    | AnswerStringQuestion of PlayerId * string
    | AnswerBoolQuestion of PlayerId * bool

type internal ChannelMsg =
    | UIMsg of UIMsg
    | ControllerMsg of Guid * ControllerMsg

module internal Decode =
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

    let oneOfMany itemDecoder : Decoder<_> =
        Decode.object (fun get ->
            OneOfMany.create
                (get.Required.Field "items" (Decode.array itemDecoder))
                (get.Required.Field "currentIndex" Decode.int)
        )

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
                Costumes = get.Required.Field "costumes" (oneOfMany svgImage)
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

    let controllerMsg: Decoder<_> =
        let decoders =
            [
                ("addPlayer", Decode.tuple2 playerId playerData |> Decode.map AddPlayer)
                ("removePlayer", playerId |> Decode.map RemovePlayer)
                ("setWindowTitle", Decode.option Decode.string |> Decode.map SetWindowTitle)
                ("setBackground", svgImage |> Decode.map SetBackground)
                ("clearScene", Decode.nil ClearScene)
                ("setPosition", Decode.tuple2 playerId position |> Decode.map SetPosition)
                ("changePosition", Decode.tuple2 playerId position |> Decode.map ChangePosition)
                ("setDirection", Decode.tuple2 playerId degrees |> Decode.map SetDirection)
                ("changeDirection", Decode.tuple2 playerId degrees |> Decode.map ChangeDirection)
                ("setSpeechBubble", Decode.tuple2 playerId optionalSpeechBubble |> Decode.map SetSpeechBubble)
                ("setPenState", Decode.tuple2 playerId Decode.bool |> Decode.map SetPenState)
                ("togglePenState", playerId |> Decode.map TogglePenState)
                ("setPenColor", Decode.tuple2 playerId rgba |> Decode.map SetPenColor)
                ("shiftPenColor", Decode.tuple2 playerId degrees |> Decode.map ShiftPenColor)
                ("setPenWeight", Decode.tuple2 playerId Decode.float |> Decode.map SetPenWeight)
                ("changePenWeight", Decode.tuple2 playerId Decode.float |> Decode.map ChangePenWeight)
                ("setSizeFactor", Decode.tuple2 playerId Decode.float |> Decode.map SetSizeFactor)
                ("changeSizeFactor", Decode.tuple2 playerId Decode.float |> Decode.map ChangeSizeFactor)
                ("setNextCostume", playerId |> Decode.map SetNextCostume)
                ("sendToBack", playerId |> Decode.map SendToBack)
                ("bringToFront", playerId |> Decode.map BringToFront)
                ("setVisibility", Decode.tuple2 playerId Decode.bool |> Decode.map SetVisibility)
                ("toggleVisibility", playerId |> Decode.map ToggleVisibility)
                ("startBatch", Decode.nil StartBatch)
                ("applyBatch", Decode.nil ApplyBatch)
            ]
            |> List.map (fun (key, decoder) ->
                Decode.field key decoder
            )

        Decode.oneOf decoders

    let uiMsg: Decoder<_> =
        let decoders =
            [
                ("setSceneBounds", rectangle |> Decode.map SetSceneBounds)
                ("answerStringQuestion", Decode.tuple2 playerId Decode.string |> Decode.map AnswerStringQuestion)
                ("answerBoolQuestion", Decode.tuple2 playerId Decode.bool |> Decode.map AnswerBoolQuestion)
            ]
            |> List.map (fun (key, decoder) ->
                Decode.field key decoder
            )

        Decode.oneOf decoders

    let channelMsg: Decoder<_> =
        let decoders =
            [
                ("controllerMsg", Decode.tuple2 Decode.guid controllerMsg |> Decode.map ControllerMsg)
                ("uiMsg", uiMsg |> Decode.map UIMsg)
            ]
            |> List.map (fun (key, decoder) ->
                Decode.field key decoder
            )

        Decode.oneOf decoders

module internal Encode =
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

    let oneOfMany encodeItem p =
        Encode.object [
            "currentIndex", Encode.int p.CurrentIndex
            "items", Encode.array (Array.map encodeItem p.Items)
        ]

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
            ("costumes", oneOfMany svgImage p.Costumes)
            ("layer", Encode.int p.Layer)
            ("isVisible", Encode.bool p.IsVisible)
        ]

    let rectangle (p: Rectangle) =
        Encode.object [
            ("position", position p.Position)
            ("size", size p.Size)
        ]

    let controllerMsg msg =
        match msg with
        | AddPlayer (pId, pData) ->
            Encode.object [ ("addPlayer", Encode.tuple2 playerId playerData (pId, pData)) ]
        | RemovePlayer pId ->
            Encode.object [ ("removePlayer", playerId pId) ]
        | SetWindowTitle text ->
            Encode.object [ ("setWindowTitle", Encode.option Encode.string text) ]
        | SetBackground background ->
            Encode.object [ ("setBackground", svgImage background) ]
        | ClearScene ->
            Encode.object [ ("clearScene", Encode.nil) ]
        | SetPosition (pId, pos) ->
            Encode.object [ ("setPosition", Encode.tuple2 playerId position (pId, pos)) ]
        | ChangePosition (pId, pos) ->
            Encode.object [ ("changePosition", Encode.tuple2 playerId position (pId, pos)) ]
        | SetDirection (pId, dir) ->
            Encode.object [ ("setDirection", Encode.tuple2 playerId degrees (pId, dir))]
        | ChangeDirection (pId, dir) ->
            Encode.object [ ("changeDirection", Encode.tuple2 playerId degrees (pId, dir))]
        | SetSpeechBubble (pId, data) ->
            Encode.object [ ("setSpeechBubble", Encode.tuple2 playerId optionalSpeechBubble (pId, data)) ]
        | SetPenState (pId, isOn) ->
            Encode.object [ ("setPenState", Encode.tuple2 playerId Encode.bool (pId, isOn)) ]
        | TogglePenState pId ->
            Encode.object [ ("togglePenState", playerId pId) ]
        | SetPenColor (pId, color) ->
            Encode.object [ ("setPenColor", Encode.tuple2 playerId rgba (pId, color)) ]
        | ShiftPenColor (pId, angle) ->
            Encode.object [ ("shiftPenColor", Encode.tuple2 playerId degrees (pId, angle)) ]
        | SetPenWeight (pId, weight) ->
            Encode.object [ ("setPenWeight", Encode.tuple2 playerId Encode.float (pId, weight)) ]
        | ChangePenWeight (pId, weight) ->
            Encode.object [ ("changePenWeight", Encode.tuple2 playerId Encode.float (pId, weight)) ]
        | SetSizeFactor (pId, value) ->
            Encode.object [ ("setSizeFactor", Encode.tuple2 playerId Encode.float (pId, value)) ]
        | ChangeSizeFactor (pId, value) ->
            Encode.object [ ("changeSizeFactor", Encode.tuple2 playerId Encode.float (pId, value)) ]
        | SetNextCostume pId ->
            Encode.object [ ("setNextCostume", playerId pId) ]
        | SendToBack pId ->
            Encode.object [ ("sendToBack", playerId pId) ]
        | BringToFront pId ->
            Encode.object [ ("bringToFront", playerId pId) ]
        | SetVisibility (pId, isVisible) ->
            Encode.object [ ("setVisibility", Encode.tuple2 playerId Encode.bool (pId, isVisible)) ]
        | ToggleVisibility pId ->
            Encode.object [ ("toggleVisibility", playerId pId) ]
        | StartBatch ->
            Encode.object [ ("startBatch", Encode.nil) ]
        | ApplyBatch ->
            Encode.object [ ("applyBatch", Encode.nil) ]

    let uiMsg msg =
        match msg with
        | SetSceneBounds p ->
            Encode.object [ ("setSceneBounds", rectangle p) ]
        | AnswerStringQuestion (pId, answer) ->
            Encode.object [ ("answerStringQuestion", Encode.tuple2 playerId Encode.string (pId, answer)) ]
        | AnswerBoolQuestion (pId, answer) ->
            Encode.object [ ("answerBoolQuestion", Encode.tuple2 playerId Encode.bool (pId, answer)) ]

    let channelMsg msg =
        match msg with
        | ControllerMsg (msgId, msg) ->
            Encode.object [ ("controllerMsg", Encode.tuple2 Encode.guid controllerMsg (msgId, msg)) ]
        | UIMsg msg ->
            Encode.object [ ("uiMsg", uiMsg msg) ]
