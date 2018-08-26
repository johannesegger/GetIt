module rec ReactPixi

open Fable.Core
open Fable.Core.JsInterop
open Fable.Helpers.React

type Point = {
    x: float
    y: float
}

type StageOption =
    | BackgroundColor of int

type StageProps =
    | Width of float
    | Height of float
    | Options of StageOption list

let inline stage (props: StageProps list) elems =
    ofImport "Stage" "@inlet/react-pixi" (keyValueList CaseRules.LowerFirst props) elems

type ContainerProps =
    | X of float
    | Y of float

let inline container (props: ContainerProps list) elems =
    ofImport "Container" "@inlet/react-pixi" (keyValueList CaseRules.LowerFirst props) elems

type SpriteProps =
    | Image of string
    | X of float
    | Y of float
    | Rotation of float
    | Anchor of Point
    | Scale of Point
    | Skew of Point
    | Width of float
    | Height of float

let inline sprite (props : SpriteProps list) elems =
    ofImport "Sprite" "@inlet/react-pixi" (keyValueList CaseRules.LowerFirst props) elems

type TextStyleProps =
    | FontFamily of string
    | FontSize of string
    | Fill of string

type TextProps =
    | X of float
    | Y of float
    | Text of string
    | Anchor of Point
    | [<CompiledName("style")>] TextStyle of TextStyleProps list

let inline text (props: TextProps list) elems =
    ofImport "Text" "@inlet/react-pixi" (keyValueList CaseRules.LowerFirst props) elems

type [<AllowNullLiteral>] GraphicsContext =
    abstract clear: unit -> unit
    abstract lineStyle: float -> string -> unit
    abstract moveTo: float -> float -> unit
    abstract lineTo: float -> float -> unit


type GraphicsProps =
    | Draw of (GraphicsContext -> unit)

let inline graphics (props: GraphicsProps list) elems =
    ofImport "Graphics" "@inlet/react-pixi" (keyValueList CaseRules.LowerFirst props) elems
