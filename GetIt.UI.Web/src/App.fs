module App.View

open Elmish
open Elmish.Debug
open Elmish.React
open Elmish.HMR // Must be last Elmish.* open declaration (see https://elmish.github.io/hmr/#Usage)
open Fable.Core.JsInterop
open Fable.Helpers.React

importAll "../sass/main.sass"

type Msg = class end

type Model = unit

let init () = (), Cmd.none

let update msg model =
    model, Cmd.none

let view model dispatch =
  div [] []

// App
Program.mkProgram init update view
#if DEBUG
|> Program.withDebugger
#endif
|> Program.withReact "elmish-app"
|> Program.run
