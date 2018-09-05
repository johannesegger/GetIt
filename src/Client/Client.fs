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
open ReactDraggable
open GameLib.Data
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

type LimitedRunnableReason =
    | TooManyInstructions of Instruction list

type UserProgram =
    | NotCompiled
    | NotRunnable of NotRunnableReason
    | Runnable of Instruction list
    | LimitedRunnable of LimitedRunnableReason
    | Running of Instruction list
    | HasErrors of CompilationError list

type DragState =
    | NotDragging
    | Dragging of Position

type DrawnLine =
    { From: Position
      To: Position
      Color: RGBColor
      Weight: float }

type Player =
    { Position: Position
      Direction: float
      Pen: Pen
      SpeechBubble: (string * TimeSpan option) option
      CostumeUrl: string
      Size: Size
      DrawnLines: DrawnLine list }

type Model = {
    MirrorSharp: MirrorSharpState
    Code: string
    UserProgram: UserProgram
    IsProgramInSyncWithServer: bool
    Player: Player
    DragState: DragState
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
    | ResetPlayer

let init () =
    let code = ""
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
              CostumeUrl = "/images/models/turtle.png"
              Size = { Width = 50.; Height = 50. }
              DrawnLines = [] }
          DragState = NotDragging }

    model, Cmd.none

let private toast title message =
    Toast.message message
    |> Toast.title title
    |> Toast.position Toast.TopRight
    |> Toast.noTimeout
    |> Toast.withCloseButton
    |> Toast.dismissOnClick

let toServerPlayer player =
    { Server.Player.Position = player.Position
      Server.Player.Direction = player.Direction
      Server.Player.Pen = player.Pen
      Server.Player.Size = player.Size
    }

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
                    (createObj
                        [ "language" ==> "C#" // Remove if https://github.com/ashmind/mirrorsharp/pull/85 is merged
                          "x-player" ==> (currentModel.Player |> toServerPlayer |> serializePlayer) ])
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
            | RunScriptResult.StoppedExecution StopReason.TimedOut ->
                UserProgram.NotRunnable NotRunnableReason.TimedOut
            | RunScriptResult.StoppedExecution (StopReason.TooManyInstructions instructions) ->
                UserProgram.LimitedRunnable (LimitedRunnableReason.TooManyInstructions instructions)

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
            | UserProgram.LimitedRunnable (TooManyInstructions (instruction :: instructions))
            | Running (instruction :: instructions) ->
                let applyPlayerInstruction player = function
                    | SetPositionInstruction position ->
                        let drawnLines =
                            if player.Pen.IsOn && position <> player.Position
                            then
                                let line =
                                    { From = player.Position
                                      To = position
                                      Color = player.Pen.Color
                                      Weight = player.Pen.Weight }
                                line :: player.DrawnLines
                            else player.DrawnLines
                        { player with
                            Position = position
                            DrawnLines = drawnLines }
                    | SetDirectionInstruction direction -> { player with Direction = direction }
                    | SayInstruction data -> { player with SpeechBubble = Some data }
                    | SetPenOnInstruction isOn -> { player with Pen = { player.Pen with IsOn = isOn } }
                    | SetPenColorInstruction color -> { player with Pen = { player.Pen with Color = color } }
                    | SetPenWeigthInstruction weight -> { player with Pen = { player.Pen with Weight = weight } }

                let applySceneInstruction player = function
                    | ClearLinesInstruction -> { player with DrawnLines = [] }

                let applyInstruction player = function
                    | PlayerInstruction instruction -> applyPlayerInstruction player instruction
                    | SceneInstruction instruction -> applySceneInstruction player instruction

                let player =
                    match currentModel.Player with
                    | { SpeechBubble = (Some (_, Some _)) } as p -> { p with SpeechBubble = None }
                    | x -> x

                let model =
                    { currentModel with
                        Player = applyInstruction player instruction
                        UserProgram = Running instructions }
                    
                let duration =
                    match model.Player with
                    | { SpeechBubble = Some (_, Some duration) } -> duration
                    | _ -> TimeSpan.FromMilliseconds 50.
                let cmd =
                    Cmd.ofAsync
                        Async.Sleep
                        (int duration.TotalMilliseconds)
                        (fun () -> RunCode)
                        (fun _e -> RunCode)
                model, cmd
            | UserProgram.Runnable []
            | UserProgram.LimitedRunnable (TooManyInstructions []) -> currentModel, Cmd.none
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
            let newPosition = currentModel.Player.Position + (position - previousDragPosition)
            let model =
                { currentModel with
                    Player = { currentModel.Player with Position = newPosition }
                    DragState = Dragging position }
            model, Cmd.none
        | NotDragging -> currentModel, Cmd.none
    | StopDragPlayer ->
        let model =
            { currentModel with
                DragState = NotDragging }
        update SendMirrorSharpServerOptions model
    | ResetPlayer ->
        let model =
            { currentModel with
                Player =
                    { currentModel.Player with
                        Position = { X = 0.; Y = 0. }
                        Direction = 0. } }
        update SendMirrorSharpServerOptions model

let private sceneWidth, sceneHeight = 500., 500.

let private lineView line =
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

    div [ Style (linePositionStyle line) ] []

let private speechBubbleView player =
    match player.SpeechBubble with
    | Some (text, _) ->
        let yProp, yClassName =
            if player.Position.Y > 0.
            then Top (sceneHeight / 2. - player.Position.Y + player.Size.Height), "top"
            else Bottom (sceneHeight / 2. + player.Position.Y + player.Size.Height), "bottom"
        div
            [ Style
                [ Position "absolute"
                  Left (player.Position.X + sceneWidth / 2.)
                  yProp
                  Transform "translate(-50%, 0)" ]
              ClassName (sprintf "speech-bubble %s" yClassName) ]
            [ p [] [ str text ] ]
        |> Some
    | _ -> None

let private playerView player dispatch =
    draggable
        [ Bounds
            [ DraggableBoundsLeft (-player.Size.Width / 2.)
              DraggableBoundsTop (-player.Size.Height / 2.)
              DraggableBoundsRight (sceneWidth - player.Size.Width / 2.)
              DraggableBoundsBottom (sceneHeight - player.Size.Height / 2.) ]
          DraggablePosition
            [ X (player.Position.X + sceneWidth / 2. - player.Size.Width / 2.)
              Y (sceneHeight / 2. - player.Position.Y - player.Size.Height / 2.) ]
          OnStart (fun e d ->
            let position = { X = d.x; Y = -d.y}
            dispatch (StartDragPlayer position))
          OnDrag (fun e d ->
            let position = { X = d.x; Y = -d.y}
            dispatch (DragPlayer position))
          OnStop (fun e d -> dispatch StopDragPlayer) ]
        [ div
            [ Style
                [ Position "absolute"
                  Left "0"
                  Top "0"
                  Width (sprintf "%fpx" player.Size.Width)
                  Height (sprintf "%fpx" player.Size.Height) ] ]
            [ img
                [ Src player.CostumeUrl
                  Draggable false
                  Style [ Transform (sprintf "rotate(%fdeg)" (360. - player.Direction)) ] ] ] ]

let private infoView model dispatch =
    let tooltipProps: IHTMLProp list =
        match model.DragState with
        | Dragging _ -> []
        | NotDragging ->
            [ ClassName (String.concat " " [ Tooltip.ClassName; Tooltip.IsTooltipBottom ])
              Tooltip.dataTooltip "Double-click to reset player" ]
      
    div
        [ yield Style [ Position "absolute"; Left "20px"; Bottom "20px"; Cursor "default"; !!("userSelect", "none") ] :> IHTMLProp
          yield OnDoubleClick (fun _ -> dispatch ResetPlayer) :> IHTMLProp
          yield! tooltipProps ]
        [ str (sprintf "X: %.2f | Y: %.2f | ∠ %.2f°" model.Player.Position.X model.Player.Position.Y model.Player.Direction) ]

let private sceneView model dispatch =
    div
        [ Style
            [ Position "relative"
              Width (sprintf "%fpx" sceneWidth)
              Height (sprintf "%fpx" sceneWidth)
              MarginTop (sprintf "%fpx" (model.Player.Size.Height / 2.))
              Background "#eeeeee"
              Overflow "hidden" ] ]
        [ yield! List.map lineView model.Player.DrawnLines
          yield! Option.toList (speechBubbleView model.Player)
          yield playerView model.Player dispatch
          yield infoView model dispatch ]

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
        | LimitedRunnable _ -> IsWarning, true
        | Running _ -> IsSuccess, true
        | HasErrors _ -> IsDanger, false

    let runTooltip =
        if not model.IsProgramInSyncWithServer
        then Some ("Compiling", [])
        else
            match model.UserProgram with
            | NotRunnable TimedOut ->
                Some ("Execution of your program timed out. You might have an endless loop somewhere.", [ Tooltip.IsMultiline ])
            | LimitedRunnable (TooManyInstructions instructions) ->
                Some ((sprintf "Your program has too many instructions. Only the first %d instructions will be executed." instructions.Length), [ Tooltip.IsMultiline ])
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
