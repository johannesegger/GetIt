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

let serializePlayer (player: Player): string =
#if FABLE_COMPILER
    player |> toJson |> btoa |> String.replace "=" "|"
#else
    failwith "not implemented"
#endif

#if FABLE_COMPILER
let deserializePlayer (value: string) =
    failwith "not implemented"
#else
let deserializePlayer jsonConverter (value: string) =
    value
    |> String.replace "|" "="
    |> System.Convert.FromBase64String
    |> System.Text.Encoding.Default.GetString
    |> fun s -> Json.JsonConvert.DeserializeObject<Player>(s, [|jsonConverter|])
#endif