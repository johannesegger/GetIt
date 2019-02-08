module GetIt

type Position =
    { X: float
      Y: float }
    override this.ToString() = sprintf "(%.2f, %.2f)" this.X this.Y

module Position =
    [<CompiledName("Zero")>]
    let zero = { X = 0.; Y = 0. }

type Size =
    { Width: float
      Height: float }
    static member (*) (size, factor) =
        { Width = size.Width * factor
          Height = size.Height * factor }

module internal Size =
    open System

    let scale boxSize size =
        let widthRatio = boxSize.Width / size.Width;
        let heightRatio = boxSize.Height / size.Height;
        let ratio = Math.Min(widthRatio, heightRatio);
        size * ratio

type Degrees = private Degrees of double 
    with
        static member private Create(value) =
            Degrees ((value % 360. + 360.) % 360.)
        static member (+) (Degrees v1, Degrees v2) =
            Degrees.Create (v1 + v2)
        static member (-) (Degrees v1, Degrees v2) =
            Degrees.Create (v1 - v2)
        static member op_Implicit value =
            Degrees.Create value
module Degrees =
    [<CompiledName("Zero")>]
    let zero = Degrees 0.
