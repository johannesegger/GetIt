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
open Fulma.FontAwesome
open Thoth.Elmish
open MirrorSharp
open GameLib.Data
open GameLib.Data.Global
open GameLib.Execution
open GameLib.Serialization

importAll "../../node_modules/font-awesome/scss/font-awesome.scss"
importAll "../../node_modules/firacode/distr/fira_code.css"
importAll "./sass/main.sass"

type MirrorSharpConnectionState = Open | Closed | Error

type InitializedMirrorSharpState = {
    Instance: MirrorSharpInstance
    ConnectionState: MirrorSharpConnectionState
}

type MirrorSharpState =
    | NotInitialized
    | Initialized of InitializedMirrorSharpState

type NotRunnableReason =
    | TimedOut

type UserProgram =
    | NotCompiled
    | NotRunnable of NotRunnableReason
    | Runnable of Player list
    | Running of Player list
    | HasErrors of CompilationError list

type DrawnLine = {
    From: Position
    To: Position
    Color: RGBColor
    Weight: float
}

type DragState =
    | NotDragging
    | Dragging of Position

type Model = {
    MirrorSharp: MirrorSharpState
    Code: string
    UserProgram: UserProgram
    IsProgramInSyncWithServer: bool
    Player: Player
    DragState: DragState
    DrawnLines: DrawnLine list
}

type Msg =
    | InitializedMirrorSharp of MirrorSharpInstance
    | UninitializedMirrorSharp
    | MirrorSharpConnectionChanged of MirrorSharpConnectionState
    | MirrorSharpServerError of string
    | SendMirrorSharpServerOptions
    | SendMirrorSharpServerOptionsResponse of Result<unit, exn>
    | CodeChanged of string
    | CodeCompilationStarted
    | CodeCompilationFinished of RunScriptResult
    | RunCode
    | StartDragPlayer of Position
    | DragPlayer of Position
    | StopDragPlayer

let init () =
    let code = "/*Player.SetPenColor(new RGBColor(0xFF, 0x00, 0xFF));
for (var i = 0; i < 20; i++)
{
    Player.TurnOffPen();
    Player.GoTo(0, -200 + 20 * i);
    Player.TurnOnPen();
    for (int j = 0; j < 60; j++)
    {
        Player.SetPenWeight((j % 5) + 1);
        Player.RotateCounterClockwise(6);
        Player.Go(5);
        Player.ShiftPenColor(0.01);
    }
}*/"
    let model =
        { MirrorSharp = NotInitialized
          Code = code
          UserProgram = NotCompiled
          IsProgramInSyncWithServer = code = ""
          Player =
            { Position = { X = 0.; Y = 0. }
              Direction = 0.
              Pen =
                { Weight = 1.
                  Color = { Red = 0uy; Green = 0uy; Blue = 0uy }
                  IsOn = false }
              SpeechBubble = None
              CostumeUrl = "/images/models/turtle.png" }
          DragState = NotDragging
          DrawnLines = [] }

    model, Cmd.none

let private toast title message =
    Toast.message message
    |> Toast.title title
    |> Toast.position Toast.TopRight
    |> Toast.noTimeout
    |> Toast.withCloseButton
    |> Toast.dismissOnClick

let rec update msg currentModel =
    match msg with
    | InitializedMirrorSharp instance ->
        let model =
            { currentModel with
                MirrorSharp =
                    Initialized
                        { Instance = instance
                          ConnectionState = Closed } }
        update SendMirrorSharpServerOptions model
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
        let cmd =
            toast "Server error" message
            |> Toast.error
        currentModel, cmd
    | SendMirrorSharpServerOptions ->
        match currentModel.MirrorSharp with
        | Initialized mirrorSharp ->
            let model =
                { currentModel with
                    IsProgramInSyncWithServer = false }
            let cmd =
                Cmd.ofPromise
                    mirrorSharp.Instance.sendServerOptions
                    (createObj [ "language" ==> "C#"; "x-player" ==> serializePlayer currentModel.Player ]) // Remove language if https://github.com/ashmind/mirrorsharp/pull/85 is merged
                    (Ok >> SendMirrorSharpServerOptionsResponse)
                    (Result.Error >> SendMirrorSharpServerOptionsResponse)
            model, cmd
        | NotInitialized ->
            currentModel, Cmd.none
    | SendMirrorSharpServerOptionsResponse (Ok ()) ->
        currentModel, Cmd.none
    | SendMirrorSharpServerOptionsResponse (Result.Error e) ->
        let cmd =
            toast "Set server options" e.Message
            |> Toast.error
        currentModel, cmd
    | CodeChanged code ->
        let model =
            { currentModel with
                Code = code
                IsProgramInSyncWithServer = false }
        model, Cmd.none
    | CodeCompilationStarted ->
        let model =
            { currentModel with
                UserProgram = NotCompiled }
        model, Cmd.none
    | CodeCompilationFinished result ->
        let userProgram =
            match result with
            | RunScriptResult.Skipped (CompilationErrors compilationErrors) ->
                UserProgram.HasErrors compilationErrors
            | RunScriptResult.RanToCompletion instructions ->
                UserProgram.Runnable instructions
            | RunScriptResult.TimedOut ->
                UserProgram.NotRunnable NotRunnableReason.TimedOut

        let model =
            { currentModel with
                UserProgram = userProgram
                IsProgramInSyncWithServer = true }
        model, Cmd.none
    | RunCode ->
        if currentModel.IsProgramInSyncWithServer
        then
            match currentModel.UserProgram with
            | UserProgram.Runnable (instruction :: instructions)
            | Running (instruction :: instructions) ->
                let model =
                    { currentModel with
                        Player = instruction
                        UserProgram = Running instructions }
                let model' =
                    if model.Player.Pen.IsOn && currentModel.Player.Position <> model.Player.Position
                    then
                        let line =
                            { From = currentModel.Player.Position
                              To = model.Player.Position
                              Color = model.Player.Pen.Color
                              Weight = model.Player.Pen.Weight }
                        { model with DrawnLines = line :: model.DrawnLines}
                    else model
                let duration =
                    match instruction with
                    | { SpeechBubble = Some (_, Some duration) } -> duration
                    | _ -> TimeSpan.FromMilliseconds 50.
                let cmd =
                    Cmd.ofAsync
                        Async.Sleep
                        (int duration.TotalMilliseconds)
                        (fun () -> RunCode)
                        (fun _e -> RunCode)
                model', cmd
            | UserProgram.Runnable [] -> currentModel, Cmd.none
            | Running [] -> update SendMirrorSharpServerOptions currentModel
            | NotCompiled
            | UserProgram.HasErrors _
            | NotRunnable _ -> currentModel, Cmd.none
        else currentModel, Cmd.none
    | StartDragPlayer position ->
        let model =
            { currentModel with
                DragState = Dragging position
                // TODO this feels like cheating because `IsProgramInSyncWithServer`
                // would be set to `false` as soon as server options are sent,
                // maybe prohibit dragging while the programming is running?
                IsProgramInSyncWithServer = false }
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
        update SendMirrorSharpServerOptions model

let private sceneView model dispatch =
    let sceneWidth, sceneHeight = 500., 500.

    let playerPositionStyle player =
        let left = sprintf "%fpx" (player.Position.X + sceneWidth / 2.)
        let bottom = sprintf "%fpx" (player.Position.Y + sceneHeight / 2.)
        [ Position "absolute"
          Left left
          Bottom bottom
          Transform (sprintf "translate(-50%%, 50%%) rotate(%fdeg)" (360. - player.Direction)) ]

    let getAngle p1 p2 =
        let dx = p2.X - p1.X
        let dy = p2.Y - p1.Y
        let atan2 = Math.Atan2(dy, dx) * 180. / Math.PI
        if atan2 < 0. then atan2 + 360.
        else atan2
      
    let getLength p1 p2 =
        let dx = p2.X - p1.X
        let dy = p2.Y - p1.Y
        Math.Sqrt(dx * dx + dy * dy)

    let linePositionStyle line =
        let direction = getAngle line.From line.To
        let left = sprintf "%fpx" (line.From.X + sceneWidth / 2.)
        let bottom = sprintf "%fpx" (line.From.Y + sceneHeight / 2.)
        [ Position "absolute"
          Left left
          Bottom bottom
          Transform (sprintf "translate(0, 50%%) rotate(%fdeg)" (360. - direction))
          TransformOrigin "left center"
          Height (sprintf "%fpx" line.Weight)
          Width (sprintf "%fpx" (getLength line.From line.To))
          Background (sprintf "rgb(%d, %d, %d)" line.Color.Red line.Color.Green line.Color.Blue) ]

    let dragSpriteProps : IHTMLProp list =
        match model.DragState with
        | Dragging _ ->
            [ OnMouseMove (fun e ->
                let delta = { X = e.pageX; Y = -e.pageY }
                dispatch (DragPlayer delta)
              )
              OnMouseUp (fun _e -> dispatch StopDragPlayer)
            ]
        | NotDragging -> []

    div
        [ yield Style
            [ Position "relative"
              Width (sprintf "%fpx" sceneWidth)
              Height (sprintf "%fpx" sceneWidth)
              Background "#eeeeee" ] :> IHTMLProp
          yield! dragSpriteProps ]
        [ for line in model.DrawnLines -> div [ Style (linePositionStyle line) ] []
          yield div
            [ Style (playerPositionStyle model.Player)
              OnMouseDown (fun e ->
                e.preventDefault()
                let position = { X = e.pageX; Y = -e.pageY }
                dispatch (StartDragPlayer position)) ]
            [ img [ Src model.Player.CostumeUrl ] ]
          yield div [ Style [ Position "absolute"; Left "20px"; Bottom "20px" ] ]
            [ str (sprintf "X: %.2f | Y: %.2f | ∠ %.2f°" model.Player.Position.X model.Player.Position.Y model.Player.Direction) ] ]

let view model dispatch =
    let host = Browser.window.location.host
    let mirrorSharpServiceUrl = sprintf "ws://%s/mirrorsharp" host

    let parseConnectionState = function
        | "open" -> Open
        | "close" -> Closed
        | "error" -> MirrorSharpConnectionState.Error
        | x -> failwithf "Invalid connection state: %s" x

    let mapCodeCompilationResult result =
        ofJson<RunScriptResult> !!result?x

    let color, isRunnable =
        match model.UserProgram with
        | NotCompiled -> IsSuccess, false
        | NotRunnable _ -> IsWarning, false
        | Runnable _ -> IsSuccess, true
        | Running _ -> IsSuccess, true
        | HasErrors _ -> IsDanger, false

    let runTooltip =
        if not model.IsProgramInSyncWithServer
        then Some ("Compiling", [])
        else
            match model.UserProgram with
            | NotRunnable TimedOut -> Some ("Execution of your program timed out. You might have an endless loop somewhere", [ Tooltip.IsMultiline ])
            | _ -> None

    div [ Style [ Display "flex"; FlexDirection "column" ] ]
        [ Navbar.navbar [ Navbar.Color color; Navbar.Props [ Style [ Flex "0 0 auto" ] ] ]
            [ Navbar.Item.div []
                [ Heading.h2 []
                    [ str "Play and Learn" ] ] ]
          Columns.columns [ Columns.Props [ Style [ Flex "1" ] ] ]
            [ Column.column [ Column.Props [ Style [ Display "flex"; FlexDirection "column"; CSSProp.Height "100%" ] ] ]
                [ Button.list [ Button.List.Props [ Style [ Padding "1rem 1rem 0 1rem" ] ] ]
                    [ Button.button
                        [ yield Button.OnClick (fun _ev -> dispatch RunCode)
                          yield Button.Color color
                          yield Button.IsOutlined
                          yield Button.Disabled (not (isRunnable && model.IsProgramInSyncWithServer))
                          yield Button.Props
                            [ yield Style [ Flex "0 0 auto" ] :> IHTMLProp
                              match runTooltip with
                              | Some (text, _) -> yield Tooltip.dataTooltip text :> IHTMLProp
                              | None -> () ]
                          match runTooltip with
                          | Some (_, classes) ->
                            let classString = [ Tooltip.ClassName; Tooltip.IsTooltipRight ] @ classes |> String.concat " "
                            yield Button.CustomClass classString
                          | None -> () ]
                        [ span [] [ str "Run" ]
                          Icon.faIcon [ ] [ Fa.icon Fa.I.Play ] ] ]
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
                [ sceneView model dispatch ]
            ]
        ]

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
|> Program.withHMR
#endif
|> Toast.Program.withToast Toast.renderFulma
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
