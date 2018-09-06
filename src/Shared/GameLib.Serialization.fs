module GameLib.Serialization

open GameLib.Data.Server

module String =
    let replace (a: string) (b: string) (text: string) = text.Replace(a, b)

#if FABLE_COMPILER
open Fable.Core
open Fable.Core.JsInterop

[<Emit("btoa($0)")>]
let btoa (text: string): string = jsNative
#else
open Newtonsoft
#endif

let serializeState (state: ScriptState): string =
#if FABLE_COMPILER
    state |> toJson |> btoa |> String.replace "=" "|"
#else
    failwith "not implemented"
#endif

#if FABLE_COMPILER
let deserializeState (value: string) =
    failwith "not implemented"
#else
let deserializeState jsonConverter (value: string) =
    value
    |> String.replace "|" "="
    |> System.Convert.FromBase64String
    |> System.Text.Encoding.Default.GetString
    |> fun s -> Json.JsonConvert.DeserializeObject<ScriptState>(s, [|jsonConverter|])
#endif