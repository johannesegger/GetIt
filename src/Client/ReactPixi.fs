module rec ReactPixi

open Fable.Core
open Fable.Core.JsInterop
open Fable.Helpers.React
open Fable.Import.React

type Point = {
    x: float
    y: float
}

type IPixiProp = interface end

type InteractiveProps =
    | Interactive of bool
    | Mousedown of (MouseEvent -> unit)
    | Mousemove of (MouseEvent -> unit)
    | Mouseup of (MouseEvent -> unit)
    | Mouseupoutside of (MouseEvent -> unit)
    interface IPixiProp

type PositionProps =
    | X of float
    | Y of float
    interface IPixiProp

type SizeProps =
    | Width of float
    | Height of float
    interface IPixiProp

type IStageProp = inherit IPixiProp

type StageOption =
    | BackgroundColor of int

type StageProps =
    | Options of StageOption list
    interface IStageProp

let inline stage (props: IPixiProp list) elems =
    ofImport "Stage" "@inlet/react-pixi" (keyValueList CaseRules.LowerFirst props) elems

type IContainerProps = inherit IPixiProp

let inline container (props: IPixiProp list) elems =
    ofImport "Container" "@inlet/react-pixi" (keyValueList CaseRules.LowerFirst props) elems

type SpriteProps =
    | Image of string
    | Rotation of float
    | Anchor of Point
    | Scale of Point
    | Skew of Point
    interface IPixiProp

let inline sprite (props : IPixiProp list) elems =
    ofImport "Sprite" "@inlet/react-pixi" (keyValueList CaseRules.LowerFirst props) elems

type TextStyleProps =
    | FontFamily of string
    | FontSize of string
    | Fill of string

type TextProps =
    | Text of string
    | Anchor of Point
    | [<CompiledName("style")>] TextStyle of TextStyleProps list
    interface IPixiProp

let inline text (props: IPixiProp list) elems =
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
