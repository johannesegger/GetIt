module Client

open Elmish
open Elmish.React
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import.Browser
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack
open Fulma
open Shared

module MirrorSharp =
    importAll "./sass/mirrorsharp.sass"

    type [<AllowNullLiteral>] MirrorSharp =
        [<Emit "$0($1...)">] abstract Invoke: element: HTMLTextAreaElementType -> options: obj -> unit
    
    let [<Import("default","../../node_modules/mirrorsharp/mirrorsharp")>] mirrorsharp: MirrorSharp = jsNative

open MirrorSharp

type Model = unit

type Msg =
    | InitializedMirrorSharp of Result<unit, exn>

let initMirrorSharp element wsUrl =
    mirrorsharp.Invoke element (createObj [ "serviceUrl" ==> wsUrl ])

let init () =
    let host = "localhost:8085" // window.location.host
    let mirrorSharpServiceUrl = sprintf "ws://%s/mirrorsharp" host
    let cmd =
        Cmd.ofPromise
            (fun serviceUrl -> promise {
                do! Promise.create(fun success fail ->
                    window.addEventListener_load (ignore >> success)
                )
                let codeElement = document.querySelector ".code" :?> HTMLTextAreaElementType
                return initMirrorSharp codeElement serviceUrl
            })
            mirrorSharpServiceUrl
            (Ok >> InitializedMirrorSharp)
            (Error >> InitializedMirrorSharp)
    (), cmd

let update msg currentModel =
    match msg with
    | InitializedMirrorSharp (Ok ()) ->
        console.info "Successfully initialized MirrorSharp"
        currentModel, Cmd.none
    | InitializedMirrorSharp (Error e) ->
        console.error ("Error while initializing MirrorSharp", e)
        currentModel, Cmd.none

let view model dispatch =
    div []
        [ Navbar.navbar [ Navbar.Color IsPrimary ]
            [ Navbar.Item.div [ ]
                [ Heading.h2 [ ]
                    [ str "Play and Learn" ] ] ]
          textarea [ ClassName "code" ] [] ]


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
