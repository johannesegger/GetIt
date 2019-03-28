namespace GetIt

open System
open System.IO
open System.Reactive
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Threading
open FSharp.Control.Reactive
open Thoth.Json.Net

type ControllerToUIMsg =
    | UIMsgProcessed
    | ShowScene of bounds: Rectangle
    | AddPlayer of PlayerId * PlayerData
    | RemovePlayer of PlayerId
    | SetPosition of PlayerId * Position
    | SetDirection of PlayerId * Degrees
    | SetSpeechBubble of PlayerId * SpeechBubble option
    | SetPen of PlayerId * Pen
    | SetSizeFactor of PlayerId * float
    | SetNextCostume of PlayerId
    | ControllerEvent of ControllerEvent

type UIToControllerMsg =
    | ControllerMsgProcessed
    | UIEvent of UIEvent

type IdentifiableMsg<'a> = IdentifiableMsg of Guid * 'a

module private Serialization =
    let decodePosition =
        Decode.object (fun get ->
            { X = get.Required.Field "x" Decode.float
              Y = get.Required.Field "y" Decode.float }
        )

    let decodeDegrees = Decode.float |> Decode.map Degrees.op_Implicit

    let decodeRgba =
        Decode.object (fun get ->
            { Red = get.Required.Field "red" Decode.int |> byte
              Green = get.Required.Field "green" Decode.int |> byte
              Blue = get.Required.Field "blue" Decode.int |> byte
              Alpha = get.Required.Field "alpha" Decode.int |> byte }
        )

    let decodePen =
        Decode.object (fun get ->
            { IsOn = get.Required.Field "isOn" Decode.bool
              Weight = get.Required.Field "weight" Decode.float
              Color = get.Required.Field "color" decodeRgba }
        )

    let decodeAskData =
        Decode.object (fun get ->
            { Question = get.Required.Field "question" Decode.string
              Answer = get.Required.Field "answer" Decode.string }
        )

    let decodeSpeechBubble =
        Decode.oneOf [
            Decode.field "say" Decode.string |> Decode.map Say
            Decode.field "ask" decodeAskData |> Decode.map Ask
        ]

    let decodeOptionalSpeechBubble = Decode.option decodeSpeechBubble

    let decodeSize =
        Decode.object (fun get ->
            { Width = get.Required.Field "width" Decode.float
              Height = get.Required.Field "height" Decode.float }
        )

    let decodeGeometryPath =
        Decode.object (fun get ->
            { FillColor = get.Required.Field "fillColor" decodeRgba
              Data = get.Required.Field "data" Decode.string }
        )

    let decodeCostume =
        Decode.object (fun get ->
            { Size = get.Required.Field "size" decodeSize
              Paths = get.Required.Field "paths" (Decode.list decodeGeometryPath) }
        )

    let decodePlayerData =
        Decode.object (fun get ->
            { SizeFactor = get.Required.Field "sizeFactor" Decode.float
              Position = get.Required.Field "position" decodePosition
              Direction = get.Required.Field "direction" decodeDegrees
              Pen = get.Required.Field "pen" decodePen
              SpeechBubble = get.Required.Field "speechBubble" decodeOptionalSpeechBubble
              Costumes = get.Required.Field "costumes" (Decode.list decodeCostume)
              CostumeIndex = get.Required.Field "costumeIndex" Decode.int }
        )

    let decodePlayerId = Decode.guid |> Decode.map PlayerId

    let decodeRectangle =
        Decode.object (fun get ->
            { Position = get.Required.Field "position" decodePosition
              Size = get.Required.Field "size" decodeSize }
        )

    let decodeKeyboardKey =
        Decode.string
        |> Decode.andThen (fun key ->
            match key with
            | "space" -> Decode.succeed Space
            | "escape" -> Decode.succeed Escape
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

    let decodeMouseButton =
        Decode.string
        |> Decode.andThen (fun key ->
            match key with
            | "primary" -> Decode.succeed Primary
            | "secondary" -> Decode.succeed Secondary
            | x -> Decode.fail (sprintf "Can't decode \"%s\" as mouse button" x)
        )

    let encodePlayerId (PlayerId playerId) = Encode.guid playerId

    let encodePosition position =
        Encode.object [
            ("x", Encode.float position.X)
            ("y", Encode.float position.Y)
        ]

    let encodeDegrees (Degrees value) = Encode.float value

    let encodeRgba rgba =
        Encode.object [
            ("red", Encode.int (int rgba.Red))
            ("green", Encode.int (int rgba.Green))
            ("blue", Encode.int (int rgba.Blue))
            ("alpha", Encode.int (int rgba.Alpha))
        ]

    let encodePen pen =
        Encode.object [
            ("isOn", Encode.bool pen.IsOn)
            ("weight", Encode.float pen.Weight)
            ("color", encodeRgba pen.Color)
        ]

    let encodeAskData askData =
        Encode.object [
            ("question", Encode.string askData.Question)
            ("answer", Encode.string askData.Answer)
        ]

    let encodeSpeechBubble speechBubble =
        match speechBubble with
        | Say text -> Encode.object [ ("say", Encode.string text) ]
        | Ask askData -> Encode.object [ ("ask", encodeAskData askData) ]

    let encodeOptionalSpeechBubble optionalSpeechBubble =
        match optionalSpeechBubble with
        | Some speechBubble -> encodeSpeechBubble speechBubble
        | None -> Encode.nil

    let encodeSize size =
        Encode.object [
            ("width", Encode.float size.Width)
            ("height", Encode.float size.Height)
        ]

    let encodeGeometryPath geometryPath =
        Encode.object [
            ("fillColor", encodeRgba geometryPath.FillColor)
            ("data", Encode.string geometryPath.Data)
        ]

    let encodeCostume costume =
        Encode.object [
            ("size", encodeSize costume.Size)
            ("paths", Encode.list (List.map encodeGeometryPath costume.Paths))
        ]

    let encodePlayerData playerData =
        Encode.object [
            ("sizeFactor", Encode.float playerData.SizeFactor)
            ("position", encodePosition playerData.Position)
            ("direction", encodeDegrees playerData.Direction)
            ("pen", encodePen playerData.Pen)
            ("speechBubble", encodeOptionalSpeechBubble playerData.SpeechBubble)
            ("costumes", Encode.list (List.map encodeCostume playerData.Costumes))
            ("costumeIndex", Encode.int playerData.CostumeIndex)
        ]

    let encodeRectangle (rectangle: Rectangle) =
        Encode.object [
            ("position", encodePosition rectangle.Position)
            ("size", encodeSize rectangle.Size)
        ]

    let encodeKeyboardKey key =
        match key with
        | Space -> "space"
        | Escape -> "escape"
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

    let encodeMouseButton mouseButton =
        match mouseButton with
        | Primary -> "primary"
        | Secondary -> "secondary"
        |> Encode.string

module ControllerToUIMsg =
    open Serialization

    let decode: Decode.Decoder<IdentifiableMsg<_>> =
        let decoders =
            [
                ("messageProcessed", Decode.nil UIMsgProcessed)
                ("showScene", decodeRectangle |> Decode.map ShowScene)
                ("addPlayer", Decode.tuple2 decodePlayerId decodePlayerData |> Decode.map AddPlayer)
                ("removePlayer", decodePlayerId |> Decode.map RemovePlayer)
                ("setPosition", Decode.tuple2 decodePlayerId decodePosition |> Decode.map SetPosition)
                ("setDirection", Decode.tuple2 decodePlayerId decodeDegrees |> Decode.map SetDirection)
                ("setSpeechBubble", Decode.tuple2 decodePlayerId decodeOptionalSpeechBubble |> Decode.map SetSpeechBubble)
                ("setPen", Decode.tuple2 decodePlayerId decodePen |> Decode.map SetPen)
                ("setSizeFactor", Decode.tuple2 decodePlayerId Decode.float |> Decode.map SetSizeFactor)
                ("setNextCostume", decodePlayerId |> Decode.map SetNextCostume)
                ("keyDown", decodeKeyboardKey |> Decode.map (KeyDown >> ControllerEvent))
                ("keyUp", decodeKeyboardKey |> Decode.map (KeyUp >> ControllerEvent))
                ("mouseMove", decodePosition |> Decode.map (MouseMove >> ControllerEvent))
                ("mouseClick", Decode.tuple2 decodeMouseButton decodePosition |> Decode.map (MouseClick >> ControllerEvent))
            ]
            |> List.map (fun (key, decoder) ->
                Decode.field key decoder
            )

        Decode.tuple2 Decode.guid (Decode.oneOf decoders)
        |> Decode.map IdentifiableMsg

    let encode (IdentifiableMsg (msgId, msg)) =
        let encodeMsg msg =
            match msg with
            | UIMsgProcessed ->
                Encode.object [ ("messageProcessed", Encode.nil) ]
            | ShowScene bounds ->
                Encode.object [ ("showScene", encodeRectangle bounds) ]
            | AddPlayer (playerId, playerData) ->
                Encode.object [ ("addPlayer", Encode.tuple2 encodePlayerId encodePlayerData (playerId, playerData)) ]
            | RemovePlayer playerId ->
                Encode.object [ ("removePlayer", encodePlayerId playerId) ]
            | SetPosition (playerId, position) ->
                Encode.object [ ("setPosition", Encode.tuple2 encodePlayerId encodePosition (playerId, position)) ]
            | SetDirection (playerId, direction) ->
                Encode.object [ ("setDirection", Encode.tuple2 encodePlayerId encodeDegrees (playerId, direction))]
            | SetSpeechBubble (playerId, speechBubble) ->
                Encode.object [ ("setSpeechBubble", Encode.tuple2 encodePlayerId encodeOptionalSpeechBubble (playerId, speechBubble)) ]
            | SetPen (playerId, pen) ->
                Encode.object [ ("setPen", Encode.tuple2 encodePlayerId encodePen (playerId, pen)) ]
            | SetSizeFactor (playerId, sizeFactor) ->
                Encode.object [ ("setSizeFactor", Encode.tuple2 encodePlayerId Encode.float (playerId, sizeFactor)) ]
            | SetNextCostume playerId ->
                Encode.object [ ("setNextCostume", encodePlayerId playerId) ]
            | ControllerEvent (KeyDown keyboardKey) ->
                Encode.object [ ("keyDown", encodeKeyboardKey keyboardKey) ]
            | ControllerEvent (KeyUp keyboardKey) ->
                Encode.object [ ("keyUp", encodeKeyboardKey keyboardKey) ]
            | ControllerEvent (MouseMove position) ->
                Encode.object [ ("mouseMove", encodePosition position) ]
            | ControllerEvent (MouseClick (mouseButton, position)) ->
                Encode.object [ ("mouseClick", Encode.tuple2 encodeMouseButton encodePosition (mouseButton, position)) ]

        Encode.tuple2 Encode.guid encodeMsg (msgId, msg)

module UIToControllerMsg =
    open Serialization
    
    let decode: Decode.Decoder<IdentifiableMsg<_>> =
        let decoders =
            [
                ("messageProcessed", Decode.nil ControllerMsgProcessed)
                ("setMousePosition", decodePosition |> Decode.map (SetMousePosition >> UIEvent))
                ("applyMouseClick", Decode.tuple2 decodeMouseButton decodePosition |> Decode.map (ApplyMouseClick >> UIEvent))
            ]
            |> List.map (fun (key, decoder) ->
                Decode.field key decoder
            )

        Decode.tuple2 Decode.guid (Decode.oneOf decoders)
        |> Decode.map IdentifiableMsg

    let encode (IdentifiableMsg (msgId, msg)) =
        let encodeMsg msg =
            match msg with
            | ControllerMsgProcessed ->
                Encode.object [ ("messageProcessed", Encode.nil) ]
            | UIEvent (SetMousePosition position) ->
                Encode.object [ ("setMousePosition", encodePosition position) ]
            | UIEvent (ApplyMouseClick (mouseButton, position)) ->
                Encode.object [ ("applyMouseClick", Encode.tuple2 encodeMouseButton encodePosition (mouseButton, position)) ]

        Encode.tuple2 Encode.guid encodeMsg (msgId, msg)

module MessageProcessing =
    let private getMessageReceiver (stream: Stream) decoder =
        let reader = new StreamReader(stream)
        Observable.Create(fun (obs: IObserver<_>) ->
            let rec loop () = async {
                let! line = async {
                    try
                        let! line = reader.ReadLineAsync() |> Async.AwaitTask
                        return Option.ofObj line
                    with _ -> return None
                }
                match line with
                | Some line ->
                    match Decode.fromString decoder line with
                    | Ok message ->
                        obs.OnNext(message)
                        return! loop()
                    | Error e -> obs.OnError(exn e)
                | None -> obs.OnCompleted()
            }
            let cts = new CancellationTokenSource()
            Async.Start (loop(), cts.Token)

            let safeReaderDisposable = Disposable.create (fun () -> try reader.Dispose() with _ -> ())

            cts
            |> Disposable.compose safeReaderDisposable
        )
        |> Observable.publish
        |> Observable.refCount

    let private getMessageSender (stream: Stream) encode =
        let writer = new StreamWriter(stream)
        Observer.Create(
            Action<_>(fun msg ->
                let line =
                    encode msg
                    |> Encode.toString 0
                writer.WriteLine(line)
                writer.Flush()),
            fun () -> try writer.Dispose() with _ -> ()
        )
        |> Observer.Synchronize

    let forStream stream encode decode =
        let receiver = getMessageReceiver stream decode
        let sender = getMessageSender stream encode
        Subject.Create<_, _>(sender, receiver)

    type ResponseError =
        | ResponseError of exn
        | NoResponse

    let sendCommand (connection: ISubject<_, _>) command =
        let msgId = Guid.NewGuid()

        use waitHandle = new ManualResetEventSlim()
        let mutable response = None

        use subscription =
            connection
            |> Observable.filter (fun (IdentifiableMsg (mId, msg)) -> mId = msgId)
            |> Observable.take 1
            |> Observable.subscribeWithCallbacks
                (fun (IdentifiableMsg (mId, msg)) ->
                    response <- Some (Ok msg)
                    waitHandle.Set()
                )
                (fun e ->
                    response <- Some (Error e)
                    waitHandle.Set()
                )
                (fun () ->
                    waitHandle.Set()
                )

        connection.OnNext(IdentifiableMsg (msgId, command))

        waitHandle.Wait()

        match response with
        | Some (Ok msg) -> Ok msg
        | Some (Error e) -> Error (ResponseError e)
        | None -> Error NoResponse
