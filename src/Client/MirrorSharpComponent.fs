module MirrorSharp

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props

type [<AllowNullLiteral>] MirrorSharpInstance =
    abstract sendServerOptions: options: obj -> JS.Promise<unit>
    abstract destroy: options: obj -> unit

type [<AllowNullLiteral>] MirrorSharp =
    [<Emit "$0($1...)">] abstract Invoke: element: Browser.Element -> options: obj -> MirrorSharpInstance

let [<Import("default","../../node_modules/mirrorsharp/mirrorsharp")>] mirrorsharp: MirrorSharp = jsNative

type MirrorSharpProp =
    | ServiceUrl of string
    | InitialCode of string
    | OnInitialized of (MirrorSharpInstance -> unit)
    | OnUninitialized of (unit -> unit)
    | OnSlowUpdateWait of (unit -> unit)
    | OnSlowUpdateResult of (obj -> unit)
    | OnTextChange of ((unit -> string) -> unit)
    | OnConnectionChange of (string -> Browser.EventType -> unit)
    | OnServerError of (string -> unit)

type [<Pojo>] MirrorSharpProps = {
    Props: MirrorSharpProp list
}

type MirrorSharpState = unit

type MirrorSharpComponent(initProps) =
    inherit Component<MirrorSharpProps, MirrorSharpState>(initProps)

    let mutable htmlElement: Browser.Element = null
    let mutable mirrorSharpInstance: MirrorSharpInstance = null

    override this.componentDidMount() =
        let mainProps, events =
            this.props.Props
            |> List.fold
                (fun (mainProps, events) -> function
                    | OnSlowUpdateWait fn ->
                        mainProps, ("slowUpdateWait" ==> fn) :: events
                    | OnSlowUpdateResult fn ->
                        mainProps, ("slowUpdateResult" ==> fn) :: events
                    | OnTextChange fn ->
                        mainProps, ("textChange" ==> fn) :: events
                    | OnConnectionChange fn ->
                        mainProps, ("connectionChange" ==> fn) :: events
                    | OnServerError fn ->
                        mainProps, ("serverError" ==> fn) :: events
                    | ServiceUrl url ->
                        ("serviceUrl" ==> url) :: mainProps, events
                    | InitialCode _
                    | OnInitialized _
                    | OnUninitialized _ ->
                        mainProps, events
                )
                ([], [])
        let options = createObj (mainProps @ [ "on" ==> createObj events ])
        mirrorSharpInstance <- mirrorsharp.Invoke htmlElement options

        this.props.Props
        |> List.tryPick (function
            | OnInitialized fn -> Some fn
            | _ -> None
        )
        |> Option.iter (fun fn -> fn mirrorSharpInstance)

    override this.componentWillUnmount() =
        mirrorSharpInstance.destroy()

        this.props.Props
        |> List.tryPick (function
            | OnUninitialized fn -> Some fn
            | _ -> None
        )
        |> Option.iter (fun fn -> fn ())

    override this.render() =
        let valueProp =
            this.props.Props
            |> List.choose (function
                | InitialCode code -> DefaultValue code :> IHTMLProp |> Some
                | _ -> None
            )
        textarea ([ Ref (fun ref -> htmlElement <- ref) ] @ valueProp) []

let mirrorSharp props =
    ofType<MirrorSharpComponent, MirrorSharpProps, MirrorSharpState> { Props = props } []