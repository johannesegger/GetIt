module GetIt.UI

open Browser.Types
open Elmish
open Elmish.Bridge
open Elmish.Debug
open Elmish.React
open Elmish.HMR // Must be last Elmish.* open declaration (see https://elmish.github.io/hmr/#Usage)
open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props

importAll "../sass/main.sass"

type ClientMsg = class end

type Msg =
    | ServerMsg of ControllerMsg
    | UIMsg of UIMsg
    | ClientMsg of ClientMsg

type Model = unit

let init () =
    printfn "Initializing model"
    (), Cmd.none

let update msg model =
    printfn "Received msg %A" msg
    model, Cmd.none

let view model dispatch =
    div [ Id "main" ] [
        div [ Id "scene" ] []
        div [ Id "info" ] []
    ]

let subscription model =
    Cmd.ofSub (fun dispatch ->
        let onMutate mutations =
            let sceneElement =
                mutations
                |> Seq.collect (fun m -> m?addedNodes)
                |> Seq.choose (fun (n: Node) ->
                    if n.nodeType = n.ELEMENT_NODE then Some (n :?> HTMLElement) else None
                )
                |> Seq.choose (fun n -> n.querySelector("#scene") :?> HTMLElement |> Option.ofObj)
                |> Seq.tryHead
            match sceneElement with
            | Some e ->
                jsThis?disconnect()

                let obs = createNew Browser.Dom.window?ResizeObserver (fun () ->
                    let sceneBounds = {
                        Position = { X = -e.offsetWidth / 2.; Y = -e.offsetHeight / 2. }
                        Size = { Width = e.offsetWidth; Height = e.offsetHeight }
                    }
                    Bridge.Send (UIMsg (SetSceneBounds sceneBounds))
                )
                obs?observe(e)
            | None -> ()
        let mutationObserverConfig = createObj [
            "childList" ==> true
            "subtree" ==> true
        ]
        let mutationObserver = createNew Browser.Dom.window?MutationObserver (onMutate)
        mutationObserver?observe(Browser.Dom.document.querySelector "#elmish-app", mutationObserverConfig)
    )

Program.mkProgram init update view
|> Program.withBridge CommunicationBridge.endpoint
|> Program.withSubscription subscription
#if DEBUG
|> Program.withDebugger
|> Program.withConsoleTrace
#endif
|> Program.withReactBatched "elmish-app"
|> Program.run
