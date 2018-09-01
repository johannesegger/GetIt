module ReactDraggable

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Browser
open Fable.Helpers.React

type DraggableBounds =
    | [<CompiledName("left")>]DraggableBoundsLeft of float
    | [<CompiledName("right")>]DraggableBoundsRight of float
    | [<CompiledName("top")>]DraggableBoundsTop of float
    | [<CompiledName("bottom")>]DraggableBoundsBottom of float

type DraggableData =
    { node: HTMLElement
      x: float
      y: float
      deltaX: float
      deltaY: float
      lastX: float
      lastY: float }

type ControlPosition =
    | X of float
    | Y of float

type DraggableProps =
    | AllowAnyClick of bool
    | Cancel of string
    | Disabled of bool
    | EnableUserSelectHack of bool
    | OffsetParent of HTMLElement
    | Grid of float * float
    | Handle of string
    | OnStart of (React.MouseEvent -> DraggableData -> unit)
    | OnDrag of (React.MouseEvent -> DraggableData -> unit)
    | OnStop of (React.MouseEvent -> DraggableData -> unit)
    | [<CompiledName("onMouseDown")>]OnDraggableMouseDown of (MouseEvent -> unit)
    | Axis of U4<string, string, string, string>
    | Bounds of DraggableBounds list //U3<DraggableBounds, string, obj>
    | DefaultClassName of string
    | DefaultClassNameDragging of string
    | DefaultClassNameDragged of string
    | DefaultPosition of ControlPosition list
    | [<CompiledName("position")>]DraggablePosition of ControlPosition list

let draggable (props: DraggableProps list) elems =
    ofImport "default" "react-draggable" (keyValueList CaseRules.LowerFirst props) elems