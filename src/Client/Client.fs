module Client

open System
open Elmish
open Elmish.React
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fulma
open Fulma.Extensions
open MirrorSharp
open ReactPixi
open GameLib.Data
open GameLib.Instruction

importAll "../../node_modules/font-awesome/scss/font-awesome.scss"

type MirrorSharpConnectionState = Open | Closed | Error

type InitializedMirrorSharpState = {
    Instance: MirrorSharpInstance
    ConnectionState: MirrorSharpConnectionState
}

type MirrorSharpState =
    | NotInitialized
    | Initialized of InitializedMirrorSharpState

type DiagnosticSpan = {
    Start: int
    Length: int
}

type DiagnosticSeverity = Hidden | Info | Warning | Error

type Diagnostic = {
    Id: string
    Message: string
    Severity: DiagnosticSeverity
    Span: DiagnosticSpan
    Tags: string list
}

type UserProgram =
    | NotCompiled
    | Runnable of GameInstruction list
    | Running of GameInstruction list * GameInstruction list
    | HasErrors of Diagnostic list

type Size = {
    Width: float
    Height: float
}

type Pen = {
    Color: RGBColor
    Weight: float
    IsOn: bool
}

type Player = {
    Position: Position
    Direction: float
    Pen: Pen
    CostumeUrl: string
}

type DrawnLine = {
    From: Position
    To: Position
    Color: RGBColor
    Weight: float
}

type ProgramState =
    | NoConnection
    | HasErrors
    | Compiled

type DragState =
    | NotDragging
    | Dragging of Position

type Model = {
    MirrorSharp: MirrorSharpState
    Code: string
    UserProgram: UserProgram
    ProgramState: ProgramState
    Player: Player
    DragState: DragState
    DrawnLines: DrawnLine list
}

type CodeCompilationResult = {
    Diagnostics: Diagnostic list
    Instructions: GameInstruction list
}

type Msg =
    | InitializedMirrorSharp of MirrorSharpInstance
    | UninitializedMirrorSharp
    | MirrorSharpConnectionChanged of MirrorSharpConnectionState
    | MirrorSharpServerError of string
    | CodeChanged of string
    | CodeCompilationStarted
    | CodeCompilationFinished of CodeCompilationResult
    | RunCode
    | StartDragPlayer of Position
    | DragPlayer of Position
    | StopDragPlayer

let init () =
    let model =
        { MirrorSharp = NotInitialized
          Code = "Player.Pen.SetColor(new RGBColor(0xFF, 0x00, 0xFF));
for (var i = 0; i < 20; i++)
{
    Player.Pen.TurnOff();
    Player.GoTo(0, 200 - 20 * i);
    Player.Pen.TurnOn();
    for (int j = 0; j < 60; j++)
    {
        Player.Pen.SetWeight((j % 5) + 1);
        Player.RotateCounterClockwise(6);
        Player.Go(5);
        Player.Pen.ShiftColor(0.01);
    }
}"
          UserProgram = NotCompiled
          ProgramState = NoConnection
          Player =
            { Position = { X = 0.; Y = 0. }
              Direction = 0.
              Pen =
                { Weight = 1.
                  Color = { Red = 0uy; Green = 0uy; Blue = 0uy }
                  IsOn = false }
              CostumeUrl = "/images/models/turtle.png" }
          DragState = NotDragging
          DrawnLines = [] }

    model, Cmd.none

let update msg currentModel =
    match msg with
    | InitializedMirrorSharp instance ->
        let model =
            { currentModel with
                MirrorSharp =
                    Initialized
                        { Instance = instance
                          ConnectionState = Closed } }
        model, Cmd.none
    | UninitializedMirrorSharp ->
        let model =
            { currentModel with
                MirrorSharp = NotInitialized }
        model, Cmd.none
    | MirrorSharpConnectionChanged connectionState ->
        match currentModel.MirrorSharp with
        | Initialized mirrorSharpState ->
            let model =
                { currentModel with
                    MirrorSharp = Initialized { mirrorSharpState with ConnectionState = connectionState }
                }
            model, Cmd.none
        | NotInitialized -> currentModel, Cmd.none
    | MirrorSharpServerError message ->
        // TODO show toast?
        currentModel, Cmd.none
    | CodeChanged code ->
        let model =
            { currentModel with
                Code = code }
        model, Cmd.none
    | CodeCompilationStarted ->
        let model =
            { currentModel with
                UserProgram = NotCompiled }
        model, Cmd.none
    | CodeCompilationFinished result ->
        let hasErrors =
            result.Diagnostics
            |> Seq.exists (fun d -> d.Severity = Error)
        let model =
            { currentModel with
                UserProgram =
                    if hasErrors
                    then UserProgram.HasErrors result.Diagnostics
                    else Runnable result.Instructions
                ProgramState =
                    if hasErrors
                    then HasErrors
                    else Compiled }
        model, Cmd.none
    | RunCode ->
        let wrapAngle value = (value % 360. + 360.) % 360.

        let shiftColor shift color =
            match ColorConvert.convert.rgb.hsl color.Red color.Green color.Blue with
            | [| h; s; l |] ->
                match ColorConvert.convert.hsl.rgb (h + int (shift * 360.)) s l with
                | [| r; g; b |] -> { Red = r; Green = g; Blue = b }
                | x -> failwithf "Unknown RGB value %A" x
            | x -> failwithf "Unknown HSL value %A" x
        
        let applyPenInstruction pen = function
            | TurnOn -> { pen with IsOn = true }
            | TurnOff ->  { pen with IsOn = false }
            | ToggleOnOff -> { pen with IsOn = not pen.IsOn }
            | SetColor color -> { pen with Color = color }
            | ShiftColor shift -> { pen with Color = shiftColor shift pen.Color }
            | SetWeight weight -> { pen with Weight = weight }
            | ChangeWeight weight -> { pen with Weight = pen.Weight + weight }

        let applyPlayerInstruction player = function
            | SetPosition position -> { player with Position = position }
            | ChangePosition position -> { player with Position = player.Position + position }
            | Go steps ->
                let directionRadians = player.Direction / 180. * Math.PI
                let delta =
                    { X = Math.Cos(directionRadians) * steps
                      Y = -Math.Sin(directionRadians) * steps }
                { player with
                    Position = player.Position + delta }
            | SetDirection direction -> { player with Direction = wrapAngle direction }
            | ChangeDirection direction -> { player with Direction = player.Direction + direction |> wrapAngle }

        let applyInstruction player = function
            | PlayerInstruction x -> applyPlayerInstruction player x
            | PenInstruction x -> { player with Pen = applyPenInstruction player.Pen x }
        
        match currentModel.UserProgram with
        | Runnable ((instruction :: instructions) as allInstructions)
        | Running ((instruction :: instructions), allInstructions) ->
            let model =
                { currentModel with
                    Player = applyInstruction currentModel.Player instruction
                    UserProgram = Running (instructions, allInstructions) }
            let model' =
                if currentModel.Player.Position <> model.Player.Position && currentModel.Player.Pen.IsOn
                then
                    let line =
                        { From = currentModel.Player.Position
                          To = model.Player.Position
                          Color = currentModel.Player.Pen.Color
                          Weight =currentModel.Player.Pen.Weight }
                    { model with DrawnLines = line :: model.DrawnLines}
                else model
            let cmd = Cmd.ofAsync Async.Sleep 50 (fun () -> RunCode) (fun _e -> RunCode)
            model', cmd
        | Runnable [] -> currentModel, Cmd.none
        | Running ([], instructions) ->
            let model =
                { currentModel with
                    UserProgram = Runnable instructions }
            model, Cmd.none
        | NotCompiled
        | UserProgram.HasErrors _ -> currentModel, Cmd.none
    | StartDragPlayer position ->
        let model =
            { currentModel with
                DragState = Dragging position }
        model, Cmd.none
    | DragPlayer position ->
        match currentModel.DragState with
        | Dragging previousDragPosition ->
            let model =
                { currentModel with
                    Player =
                        { currentModel.Player with
                            Position = currentModel.Player.Position + (position - previousDragPosition) }
                    DragState = Dragging position }
            model, Cmd.none
        | NotDragging -> currentModel, Cmd.none
    | StopDragPlayer ->
        let model =
            { currentModel with
                DragState = NotDragging }
        model, Cmd.none

let view model dispatch =
    let host = Browser.window.location.host
    let mirrorSharpServiceUrl = sprintf "ws://%s/mirrorsharp" host

    let parseConnectionState = function
        | "open" -> Open
        | "close" -> Closed
        | "error" -> MirrorSharpConnectionState.Error
        | x -> failwithf "Invalid connection state: %s" x

    let mapCodeCompilationResult result =
        { Diagnostics =
            !!result?diagnostics
            |> Seq.map (fun d ->
                let parseSeverity = function
                | "error" -> Error
                | "warning" -> Warning
                | "info" -> Info
                | "hidden" -> Hidden
                | x -> failwithf "Invalid diagnostic severity: %s" x

                { Id = !!d?id
                  Message = !!d?message
                  Severity = parseSeverity !!d?severity
                  Span = { Start = !!d?start; Length = !!d?length }
                  Tags = Seq.toList !!d?tags }
            )
            |> Seq.toList
          Instructions = ofJson<GameInstruction list> !!result?x }
          
    let sceneWidth, sceneHeight = 500., 500.

    let color, isRunnable =
        match model.ProgramState with
        | NoConnection -> IsWarning, false
        | HasErrors -> IsDanger, false
        | Compiled -> IsSuccess, true

    div [ Style [ Display "flex"; FlexDirection "column" ] ]
        [ Navbar.navbar [ Navbar.Color color; Navbar.Props [ Style [ Flex "0 0 auto" ] ] ]
            [ Navbar.Item.div []
                [ Heading.h2 []
                    [ str "Play and Learn" ] ] ]
          Columns.columns [ Columns.Props [ Style [ Flex "1" ] ] ]
            [ Column.column [ Column.Props [ Style [ Display "flex"; FlexDirection "column"; CSSProp.Height "100%" ] ] ]
                [ Button.list [ Button.List.Props [ Style [ Padding "1rem 1rem 0 1rem" ] ] ]
                    [ Button.button
                        [ Button.OnClick (fun _ev -> dispatch RunCode)
                          Button.Color color
                          Button.IsOutlined
                          Button.Disabled (not isRunnable)
                          Button.Props [ Style [ Flex "0 0 auto" ] ] ] [ str "Run ▶" ] ]
                  mirrorSharp
                    [ ServiceUrl mirrorSharpServiceUrl
                      InitialCode model.Code
                      OnInitialized (InitializedMirrorSharp >> dispatch)
                      OnUninitialized (fun () -> dispatch UninitializedMirrorSharp)
                      OnSlowUpdateWait (fun () -> dispatch CodeCompilationStarted)
                      OnSlowUpdateResult (mapCodeCompilationResult >> CodeCompilationFinished >> dispatch)
                      OnTextChange (fun getText -> dispatch (CodeChanged (getText())))
                      OnConnectionChange (fun state _event -> parseConnectionState state |> MirrorSharpConnectionChanged |> dispatch)
                      OnServerError (MirrorSharpServerError >> dispatch) ] ]
              Column.column [ Column.Width (Screen.All, Column.IsNarrow) ]
                [ Divider.divider [ Divider.IsVertical; Divider.Props [ Style [ CSSProp.Height "100%" ] ] ] ]
              Column.column [ Column.Width (Screen.All, Column.IsNarrow) ]
                [ stage [ Width sceneWidth; Height sceneHeight; Options [ BackgroundColor 0xEEEEEE ] ]
                    [ container [ X (sceneWidth / 2.); Y (sceneHeight / 2.) ]
                        [ let draw line (ctx: GraphicsContext) =
                            ctx.clear()
                            let color = sprintf "0x%02x%02x%02x" line.Color.Red line.Color.Green line.Color.Blue
                            ctx.lineStyle line.Weight color
                            ctx.moveTo line.From.X line.From.Y
                            ctx.lineTo line.To.X line.To.Y
                          yield!
                            model.DrawnLines
                            |> List.map (fun line ->
                                graphics [ Draw (draw line) ] []
                            )
                          let commonSpriteProps: IPixiProp list =
                            [ Image model.Player.CostumeUrl
                              X model.Player.Position.X
                              Y model.Player.Position.Y
                              Rotation ((360. - model.Player.Direction) / 180. * Math.PI)
                              Anchor { x = 0.5; y = 0.5 }
                              Interactive true
                              Mousedown (fun evt ->
                                let position =
                                    { X = !!evt?data?``global``?x
                                      Y = !!evt?data?``global``?y }
                                dispatch (StartDragPlayer position)) ]
                          let dragSpriteProps: IPixiProp list =
                              match model.DragState with
                              | Dragging _ ->
                                  [ Mousemove (fun evt ->
                                        let delta =
                                            { X = !!evt?data?``global``?x
                                              Y = !!evt?data?``global``?y }
                                        dispatch (DragPlayer delta))
                                    Mouseup (fun _e -> dispatch StopDragPlayer) ]
                              | NotDragging -> []
                          yield sprite (commonSpriteProps @ dragSpriteProps) []
                        ]
                      text
                        [ X 20.
                          Y (sceneHeight - 20.)
                          Text (sprintf "X: %.2f | Y: %.2f | ∠ %.2f°" model.Player.Position.X model.Player.Position.Y model.Player.Direction)
                          Anchor { x = 0.; y = 1. }
                          TextStyle [ FontFamily "Segoe UI"; FontSize "14px"; Fill "gray" ] ] [] ] ]
            ]
        ]


#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
// |> Program.withConsoleTrace
// |> Program.withHMR
#endif
|> Program.withReact "elmish-app"
#if DEBUG
// |> Program.withDebugger
#endif
|> Program.run
