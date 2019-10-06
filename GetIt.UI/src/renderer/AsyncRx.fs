module AsyncRx

open Browser.Types
open Fable.Core.JsInterop
open FSharp.Control

let observeSubTreeAdditions (parent: Node) : IAsyncObservable<Node> =
    AsyncRx.create (fun obs -> async {
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

let observeSceneSizeFromWindowResize =
    AsyncRx.create (fun obs -> async {
        let resizeCanvas evt =
            obs.OnNextAsync (Browser.Dom.window.innerWidth, Browser.Dom.window.innerHeight)
            |> Async.StartImmediate
            ()
        resizeCanvas ()
        Browser.Dom.window.addEventListener("resize", resizeCanvas, false)
        return AsyncDisposable.Create (fun () -> async {
            Browser.Dom.window.removeEventListener("resize", resizeCanvas, false)
        })
    })
    |> AsyncRx.choose (fun (windowWidth, windowHeight) ->
        match Browser.Dom.document.querySelector "#info" :?> HTMLElement |> Option.ofObj with
        | Some info -> Some (windowWidth, windowHeight - info.offsetHeight)
        | None -> None
    )
