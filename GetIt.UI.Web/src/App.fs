module GetIt.UI

open Browser.Types
open Elmish
open Elmish.Debug
open Elmish.React
open Elmish.Streams
open Elmish.HMR // Must be last Elmish.* open declaration (see https://elmish.github.io/hmr/#Usage)
open Fable.Core.JsInterop
open Fable.Elmish.Nile
open Fable.React
open Fable.React.Props
open FSharp.Control
open Thoth.Json

importAll "../sass/main.sass"

type Model = unit

let init () =
    printfn "Initializing model"
    ()

let update msg model =
    printfn "Received msg %A" msg
    model

let view model dispatch =
    div [ Id "main" ] [
        div [ Id "scene" ] []
        div [ Id "info" ] []
    ]

let observeSubTreeAdditions (parent: Node) : IAsyncObservable<Node> =
    Browser.Dom.console.log("Setting up sub tree additions", parent)
    AsyncRx.create (fun obs -> async {
        printfn "Subscribing to subtree additions"
        let onMutate mutations =
            mutations
            |> Seq.collect (fun m -> m?addedNodes)
            |> Seq.iter (obs.OnNextAsync >> Async.StartImmediate)
        let mutationObserver = createNew Browser.Dom.window?MutationObserver (onMutate)
        let mutationObserverConfig = createObj [
            "childList" ==> true
            "subtree" ==> true
        ]
        mutationObserver?observe(parent, mutationObserverConfig)
        return AsyncDisposable.Create (fun () -> async {
            mutationObserver?disconnect()
        })
    })

let observeResize (element: HTMLElement) : IAsyncObservable<float * float> =
    AsyncRx.create (fun obs -> async {
        let resizeObserver = createNew Browser.Dom.window?ResizeObserver (fun entries ->
            entries
            |> Seq.exactlyOne
            |> fun e -> (e?contentRect?width, e?contentRect?height)
            |> obs.OnNextAsync
            |> Async.StartImmediate
        )
        resizeObserver?observe(element)
        return AsyncDisposable.Create (fun () -> async {
            resizeObserver?disconnect()
        })
    })

let stream states msgs =
    [
        msgs

        Browser.Dom.document.querySelector "#elmish-app"
        |> observeSubTreeAdditions
        |> AsyncRx.choose (fun (n: Node) ->
            if n.nodeType = n.ELEMENT_NODE then Some (n :?> HTMLElement) else None
        )
        |> AsyncRx.startWith [ Browser.Dom.document.body ]
        |> AsyncRx.choose (fun n -> n.querySelector("#scene") :?> HTMLElement |> Option.ofObj)
        // |> AsyncRx.take 1
        |> AsyncRx.flatMapLatest observeResize
        |> AsyncRx.map(fun (width, height) ->
            {
                Position = { X = -width / 2.; Y = -height / 2. }
                Size = { Width = width; Height = height }
            }
            |> SetSceneBounds
            |> UIMsg
        )
        |> AsyncRx.tapOnNext (printfn "Sending %A")
        |> AsyncRx.msgChannel (sprintf "ws://%s%s" Browser.Dom.window.location.host MessageChannel.endpoint) (Encode.channelMsg >> Encode.toString 0) (Decode.fromString Decode.channelMsg >> Result.toOption)
    ]
    |> AsyncRx.mergeSeq

Program.mkSimple init update view
|> Program.withStream stream
#if DEBUG
|> Program.withDebugger
|> Program.withConsoleTrace
#endif
|> Program.withReactBatched "elmish-app"
|> Program.run
