namespace GetIt

open System
open System.IO
open System.Reactive.Linq
open System.Threading
open FSharp.Control.Reactive
open Thoth.Json.Net

type ControllerToUIMsg =
    | MsgProcessed
    | ShowScene
    | AddPlayer of PlayerId * PlayerData
    | RemovePlayer of PlayerId
    | SetPosition of PlayerId * Position
    | SetDirection of PlayerId * Degrees
    | SetSpeechBubble of PlayerId * SpeechBubble option
    | SetPen of PlayerId * Pen
    | SetSizeFactor of PlayerId * float
    | SetNextCostume of PlayerId

type UIToControllerMsg =
    | MsgProcessed
    | InitializedScene of sceneBounds: Rectangle

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

module ControllerToUIMsg =
    open Serialization

    let decode: Decode.Decoder<IdentifiableMsg<_>> =
        let decoders =
            [
                ("messageProcessed", Decode.nil ControllerToUIMsg.MsgProcessed)
                ("showScene", Decode.nil ShowScene)
                ("addPlayer", Decode.tuple2 decodePlayerId decodePlayerData |> Decode.map AddPlayer)
                ("removePlayer", decodePlayerId |> Decode.map RemovePlayer)
                ("setPosition", Decode.tuple2 decodePlayerId decodePosition |> Decode.map SetPosition)
                ("setDirection", Decode.tuple2 decodePlayerId decodeDegrees |> Decode.map SetDirection)
                ("setSpeechBubble", Decode.tuple2 decodePlayerId decodeOptionalSpeechBubble |> Decode.map SetSpeechBubble)
                ("setPen", Decode.tuple2 decodePlayerId decodePen |> Decode.map SetPen)
                ("setSizeFactor", Decode.tuple2 decodePlayerId Decode.float |> Decode.map SetSizeFactor)
                ("setNextCostume", decodePlayerId |> Decode.map SetNextCostume)
            ]
            |> List.map (fun (key, decoder) ->
                Decode.field key decoder
            )

        Decode.tuple2 Decode.guid (Decode.oneOf decoders)
        |> Decode.map IdentifiableMsg

    let encode msgId msg =
        let encodeMsg msg =
            match msg with
            | ControllerToUIMsg.MsgProcessed ->
                Encode.object [ ("messageProcessed", Encode.nil) ]
            | ShowScene ->
                Encode.object [ ("showScene", Encode.nil) ]
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
        Encode.tuple2 Encode.guid encodeMsg (msgId, msg)

module UIToControllerMsg =
    open Serialization
    
    let decode: Decode.Decoder<IdentifiableMsg<_>> =
        let decodeRectangle =
            Decode.object (fun get ->
                { Position = get.Required.Field "position" decodePosition
                  Size = get.Required.Field "size" decodeSize }
            )

        let decoders =
            [
                ("messageProcessed", Decode.nil UIToControllerMsg.MsgProcessed)
                ("initializedScene", decodeRectangle |> Decode.map InitializedScene)
            ]
            |> List.map (fun (key, decoder) ->
                Decode.field key decoder
            )

        Decode.tuple2 Decode.guid (Decode.oneOf decoders)
        |> Decode.map IdentifiableMsg

    let encode msgId msg =
        let encodeMsg msg =
            match msg with
            | UIToControllerMsg.MsgProcessed ->
                Encode.object [ ("messageProcessed", Encode.nil) ]
            | InitializedScene sceneBounds ->
                Encode.object [ ("initializedScene", encodeRectangle sceneBounds) ]
        Encode.tuple2 Encode.guid encodeMsg (msgId, msg)

module MessageProcessing =
    let getMessages (reader: TextReader) decoder =
        Observable.Create(fun (obs: IObserver<_>) ->
            let rec loop () = async {
                let! line = reader.ReadLineAsync() |> Async.AwaitTask
                if isNull line
                then
                    obs.OnCompleted()
                else
                    match Decode.fromString decoder line with
                    | Ok message -> obs.OnNext(message)
                    | Error e -> obs.OnError(exn e)
                    return! loop()
            }
            let cts = new CancellationTokenSource()
            Async.Start (loop(), cts.Token)

            let safeReaderDisposable = Disposable.create (fun () -> try reader.Dispose() with _ -> ())

            cts
            |> Disposable.compose safeReaderDisposable
        )
        |> Observable.publish
        |> Observable.refCount
