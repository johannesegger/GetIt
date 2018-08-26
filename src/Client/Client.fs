module Client

open Elmish
open Elmish.React
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fulma
open Fulma.Extensions
open MirrorSharp
open GameLib.Data
open GameLib.Instruction

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
    | HasErrors of Diagnostic list

type Size = {
    Width: int
    Height: int
}

type Pen = {
    Weight: float
    Color: RGBColor
    IsOn: bool
}

type Player = {
    Size: Size
    Position: Position
    Direction: float
    Pen: Pen
    CostumeUrl: string
}

type Model = {
    MirrorSharp: MirrorSharpState
    Code: string
    UserProgram: UserProgram
    Player: Player
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

let init () =
    let model =
        { MirrorSharp = NotInitialized
          Code = "Player.Pen.SetColor(new RGBColor(0xFF, 0x00, 0xFF));
for (var i = 0; i < 20; i++)
{
    Player.Pen.TurnOff();
    Player.GoTo(0, -200 + 20 * i);
    Player.Pen.TurnOn();
    for (int j = 0; j < 60; j++)
    {
        Player.Pen.SetWeight((j % 5) + 1);
        Player.RotateClockwise(6);
        Player.Go(5);
        Player.Pen.ShiftColor(0.01);
    }
}"
          UserProgram = NotCompiled
          Player =
            { Size = { Width = 50; Height = 50 }
              Position = { X = 0.; Y = 0. }
              Direction = 0.
              Pen =
                { Weight = 1.
                  Color = { Red = 0uy; Green = 0uy; Blue = 0uy }
                  IsOn = false }
              CostumeUrl = "/images/models/turtle.png" } }

    model, Cmd.none

let update msg currentModel =
    match msg with
    | InitializedMirrorSharp instance ->
        let model =
            { currentModel
                with MirrorSharp =
                        Initialized
                            { Instance = instance
                              ConnectionState = Closed } }
        model, Cmd.none
    | UninitializedMirrorSharp ->
        let model =
            { currentModel
                with MirrorSharp = NotInitialized }
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
        let model =
            { currentModel with
                UserProgram =
                    if result.Diagnostics |> Seq.exists (fun d -> d.Severity = Error)
                    then HasErrors result.Diagnostics
                    else Runnable result.Instructions }
        model, Cmd.none
    | RunCode ->
        currentModel, Cmd.none

let view model dispatch =
    let host = Browser.window.location.host
    let mirrorSharpServiceUrl = sprintf "ws://%s/mirrorsharp" host

    let parseConnectionState = function
        | "open" -> Open
        | "close" -> Closed
        | "error" -> MirrorSharpConnectionState.Error
        | x -> failwithf "Invalid connection state: %s" x

    let mapCodeCompilationResult result =
        { Diagnostics = toJson result?diagnostics |> ofJson<Diagnostic list>
          Instructions = ofJson<GameInstruction list> !!result?x }

    div [ Style [ Display "flex"; FlexDirection "column" ] ]
        [ Navbar.navbar [ Navbar.Color IsSuccess; Navbar.Props [ Style [ Flex "0 0 auto" ] ] ]
            [ Navbar.Item.div [ ]
                [ Heading.h2 [ Heading.Modifiers [ Modifier.TextColor IsWhiteTer ] ]
                    [ str "Play and Learn" ] ] ]
          Columns.columns [ Columns.Props [ Style [ Flex "1" ] ] ]
            [ Column.column [ Column.Props [ Style [ Display "flex"; FlexDirection "column"; Height "100%" ] ] ]
                [ Button.list [ Button.List.Props [ Style [ Padding "1rem 1rem 0 1rem" ] ] ]
                    [ Button.button
                        [ Button.OnClick (fun _ev -> dispatch RunCode)
                          Button.Color IsSuccess
                          Button.IsOutlined
                          Button.Props [ Style [ Flex "0 0 auto" ] ] ] [ str "Run ▶" ]
                    ]
                  mirrorSharp
                    [ ServiceUrl mirrorSharpServiceUrl
                      InitialCode model.Code
                      OnInitialized (InitializedMirrorSharp >> dispatch)
                      OnUninitialized (fun () -> dispatch UninitializedMirrorSharp)
                      OnSlowUpdateWait (fun () -> dispatch CodeCompilationStarted)
                      OnSlowUpdateResult (mapCodeCompilationResult >> CodeCompilationFinished >> dispatch)
                      OnTextChange (fun getText -> dispatch (CodeChanged (getText())))
                      OnConnectionChange (fun state _event -> parseConnectionState state |> MirrorSharpConnectionChanged |> dispatch)
                      OnServerError (MirrorSharpServerError >> dispatch) ]
                ]
              Column.column [ Column.Width (Screen.All, Column.IsNarrow) ]
                [ Divider.divider [ Divider.IsVertical; Divider.Props [ Style [ Height "100%" ] ] ] ]
              Column.column []
                [ canvas [] [] ]
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
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
