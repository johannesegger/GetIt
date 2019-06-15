namespace GetIt

open System

/// Defines an angle in degrees where 0 <= angle < 360.
type Degrees = private Degrees of float
    with
    static member op_Equality (x: Degrees, y: Degrees) = x = y
    static member op_Inequality (x: Degrees, y: Degrees) = x <> y
    static member op_GreaterThan (x: Degrees, y: Degrees) = x > y
    static member op_GreaterThanOrEqual (x: Degrees, y: Degrees) = x >= y
    static member op_LessThan (x: Degrees, y: Degrees) = x < y
    static member op_LessThanOrEqual (x: Degrees, y: Degrees) = x <= y
    static member private Create(value) =
        Degrees ((value % 360. + 360.) % 360.)

    static member (+) (Degrees v1, Degrees v2) =
        Degrees.Create (v1 + v2)

    static member (-) (Degrees v1, Degrees v2) =
        Degrees.Create (v1 - v2)

    static member (~-) (Degrees v) =
        Degrees.Create -v

    static member op_Implicit value =
        Degrees.Create value

    override this.ToString () =
        let (Degrees value) = this
        value.ToString()

    interface IFormattable with
        member this.ToString (format: string, formatProvider: IFormatProvider) =
            let (Degrees value) = this
            value.ToString(format, formatProvider)

/// For internal use only.
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Degrees =
    let zero = Degrees 0.

    let value (Degrees v) = v

    let toRadians (Degrees v) = v / 180. * Math.PI

/// Defines some common directions.
[<AbstractClass; Sealed>]
type Directions =
    /// Right direction.
    static member Right = Degrees.op_Implicit 0.
    /// Up direction.
    static member Up = Degrees.op_Implicit 90.
    /// Left direction.
    static member Left = Degrees.op_Implicit 180.
    /// Down direction.
    static member Down = Degrees.op_Implicit 270.

/// Defines a two-dimensional position.
type Position =
    {
        /// The horizontal coordinate of the position.
        X: float
        /// The vertical coordinate of the position.
        Y: float
    }
    override this.ToString() = sprintf "(%.2f, %.2f)" this.X this.Y
    static member (+) (p1, p2) =
        {
            X = p1.X + p2.X
            Y = p1.Y + p2.Y
        }

/// For internal use only.
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Position =
    let zero = { X = 0.; Y = 0. }

    let angleTo positionTo positionFrom =
        let dx = positionTo.X - positionFrom.X
        let dy = positionTo.Y - positionFrom.Y
        let atan2 = Math.Atan2(dy, dx) * 180. / Math.PI
        Degrees.op_Implicit (if atan2 < 0. then atan2 + 360. else atan2)

    let distanceTo positionTo positionFrom =
        let dx = positionTo.X - positionFrom.X
        let dy = positionTo.Y - positionFrom.Y
        Math.Sqrt (dx * dx + dy * dy)

/// Defines a two-dimensional size.
type Size =
    {
        /// The horizontal size.
        Width: float
        /// The vertical size.
        Height: float
    }
    static member (*) (size, factor) =
        {
            Width = size.Width * factor
            Height = size.Height * factor
        }

/// For internal use only.
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Size =
    let zero =
        { Width = 0.; Height = 0. }

    let scale boxSize size =
        let widthRatio = boxSize.Width / size.Width;
        let heightRatio = boxSize.Height / size.Height;
        let ratio = Math.Min(widthRatio, heightRatio);
        size * ratio

/// Defines a rectangle in a two-dimensional coordinate system.
type Rectangle =
    {
        /// The position of the rectangle.
        Position: Position
        /// The size of the rectangle.
        Size: Size
    }
    /// The x-coordinate of the left edge.
    member this.Left with get() = this.Position.X
    /// The x-coordinate of the right edge.
    member this.Right with get() = this.Position.X + this.Size.Width
    /// The y-coordinate of the top edge.
    member this.Top with get() = this.Position.Y + this.Size.Height
    /// The y-coordinate of the bottom edge.
    member this.Bottom with get() = this.Position.Y

/// For internal use only.
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Rectangle =
    let zero =
        { Position = Position.zero; Size = Size.zero }

    let contains position (rectangle: Rectangle) =
        rectangle.Left <= position.X &&
        rectangle.Right >= position.X &&
        rectangle.Top >= position.Y &&
        rectangle.Bottom <= position.Y
