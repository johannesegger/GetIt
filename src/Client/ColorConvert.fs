module ColorConvert

open Fable.Core

type [<AllowNullLiteral>] RGBConvert =
    abstract hsl: byte -> byte -> byte -> int array

type [<AllowNullLiteral>] HSLConvert =
    abstract rgb: int -> int -> int -> byte array

type [<AllowNullLiteral>] ColorConvert =
    abstract rgb: RGBConvert
    abstract hsl: HSLConvert

[<Import("default", from="color-convert")>]
let convert: ColorConvert = jsNative