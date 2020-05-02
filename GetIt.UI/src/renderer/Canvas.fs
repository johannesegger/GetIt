// see https://github.com/MangelMaxime/Elmish.Canvas
module Canvas

open Browser.Types
open Fable.Core
open Fable.React
open Fable.React.Props

type Line =
    {
        Start: float * float
        End: float * float
        Weight: float
        Color: string
    }

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
    | Line of Line

let rec private drawOp (ctx: CanvasRenderingContext2D) = function
    | Rect opts -> ctx.rect opts
    | Stroke -> ctx.stroke()
    | Batch ops -> List.iter (drawOp ctx) ops
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
    | Line line ->
        ctx.beginPath()
        ctx.strokeStyle <- U3.Case1 line.Color
        ctx.lineWidth <- line.Weight
        let (startX, startY) = line.Start
        ctx.moveTo(startX, startY)
        let (endX, endY) = line.End
        ctx.lineTo(endX, endY)
        ctx.stroke ()

let drawOps ctx ops =
    (ops, ())
    ||> List.foldBack (fun op () -> drawOp ctx op)

type private Props =
    | Height of float
    | Width of float
    | DrawOps of DrawOp list
    | OnTick of ((float * float) -> unit)
    | IsPlaying of bool
    | OnMouseMove of (MouseEvent -> unit)
    | Style of HTMLAttr

open Fable.Core.JsInterop

type Size =
    { Width : float
      Height : float }

type CanvasBuilder =
    { Size : Size
      DrawOps : DrawOp list
      IsPlaying : bool
      OnTick : (float * float) -> unit
      OnMouseMove : MouseEvent -> unit
      Style : CSSProp list }

let inline private canvas (props: Props list) : ReactElement =
    ofImport "default" "./js/react_canvas.js" (keyValueList CaseRules.LowerFirst props) [ ]

let initialize (size : Size) : CanvasBuilder =
    { Size = size
      DrawOps = []
      OnTick = ignore
      IsPlaying = true
      OnMouseMove = ignore
      Style = [] }

let draw (drawOp : DrawOp) (builder : CanvasBuilder) : CanvasBuilder =
    { builder with DrawOps = drawOp :: builder.DrawOps }

let playing value (builder : CanvasBuilder) : CanvasBuilder =
    { builder with IsPlaying = value }

let onTick callback (builder : CanvasBuilder) : CanvasBuilder =
    { builder with OnTick = callback }

let onMouseMove callback (builder : CanvasBuilder) : CanvasBuilder =
    { builder with OnMouseMove = callback}

let withStyle style (builder : CanvasBuilder) : CanvasBuilder =
    { builder with Style = style }

let render (builder : CanvasBuilder) =
    canvas [ Width builder.Size.Width
             Height builder.Size.Height
             DrawOps builder.DrawOps
             OnTick builder.OnTick
             IsPlaying builder.IsPlaying
             OnMouseMove builder.OnMouseMove
             Style !!(keyValueList CaseRules.LowerFirst builder.Style) ]
