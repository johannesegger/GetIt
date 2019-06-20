// https://github.com/MangelMaxime/Elmish.Canvas/blob/master/src/Canvas.fs
module Canvas

open Browser.Types
open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props

type DrawOp =
    | LineTo of (float * float)
    | MoveTo of (float * float)
    | BeginPath
    | Scale of (float * float)
    | Rotate of float
    | Save
    | Translate of (float * float)
    | Restore
    | Fill
    | Rect of (float * float * float * float)
    | FillStyle of U3<string,CanvasGradient,CanvasPattern>
    | StrokeStyle of U3<string,CanvasGradient,CanvasPattern>
    | Batch of DrawOp list
    | Stroke
    | ClearReact of (float * float * float * float)
    | DrawLoadedImage of elementSelector: string * position: (float * float) * size: (float * float)

let rec drawOps (ctx : CanvasRenderingContext2D) (ops : DrawOp list) = promise {
    for op in ops do
        match op with
        | Rect opts -> ctx.rect opts
        | Stroke -> ctx.stroke()
        | Batch ops -> do! drawOps ctx ops
        | LineTo opts -> ctx.lineTo opts
        | MoveTo opts -> ctx.moveTo opts
        | BeginPath -> ctx.beginPath()
        | Scale opts -> ctx.scale opts
        | Rotate opts -> ctx.rotate opts
        | Save -> ctx.save()
        | Translate opts -> ctx.translate opts
        | Restore -> ctx.restore()
        | Fill -> ctx.fill()
        | FillStyle opts -> ctx.fillStyle <- opts
        | StrokeStyle opts -> ctx.strokeStyle <- opts
        | ClearReact opts -> ctx.clearRect opts
        | DrawLoadedImage (elementSelector, (x, y), (width, height)) ->
            let image = Browser.Dom.document.querySelector elementSelector :?> HTMLImageElement
            ctx.drawImage (U3.Case1 image, x, y, width, height)
}

type private Props =
    | Id of string
    | Height of float
    | Width of float
    | DrawOps of DrawOp array
    | OnTick of ((float * float) -> unit)
    | IsPlaying of bool
    | OnMouseMove of (MouseEvent -> unit)
    | Style of HTMLAttr

open Fable.Core.JsInterop

type Size =
    { Width : float
      Height : float }

type CanvasBuilder =
    {
        Id: string option
        Size : Size
        DrawOps : DrawOp list
        IsPlaying : bool
        OnTick : (float * float) -> unit
        OnMouseMove : MouseEvent -> unit
        Style : CSSProp list
    }

let inline private canvas (props: Props list) : ReactElement =
    ofImport "default" "./js/react_canvas.js" (keyValueList CaseRules.LowerFirst props) [ ]

let initialize (size : Size) : CanvasBuilder =
    {
        Id = None
        Size = size
        DrawOps = []
        OnTick = ignore
        IsPlaying = true
        OnMouseMove = ignore
        Style = []
    }

let draw (drawOp : DrawOp) (builder : CanvasBuilder) : CanvasBuilder =
    { builder with DrawOps = builder.DrawOps @ [drawOp] }

let playing value (builder : CanvasBuilder) : CanvasBuilder =
    { builder with IsPlaying = value }

let onTick callback (builder : CanvasBuilder) : CanvasBuilder =
    { builder with OnTick = callback }

let onMouseMove callback (builder : CanvasBuilder) : CanvasBuilder =
    { builder with OnMouseMove = callback}

let withStyle style (builder : CanvasBuilder) : CanvasBuilder =
    { builder with Style = style }

let withId value (builder : CanvasBuilder) : CanvasBuilder =
    { builder with Id = Some value }

let render (builder : CanvasBuilder) =
    canvas [
        yield! builder.Id |> Option.map Id |> Option.toList
        yield Width builder.Size.Width
        yield Height builder.Size.Height
        yield DrawOps (List.toArray builder.DrawOps)
        yield OnTick builder.OnTick
        yield IsPlaying builder.IsPlaying
        yield OnMouseMove builder.OnMouseMove
        yield Style !!(keyValueList CaseRules.LowerFirst builder.Style)
    ]