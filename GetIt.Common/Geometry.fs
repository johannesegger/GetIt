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

    static member op_Implicit (Degrees v) =
        v

    override this.ToString () =
        let (Degrees value) = this
        value.ToString()
#if !FABLE_COMPILER
    interface IFormattable with
        member this.ToString (format: string, formatProvider: IFormatProvider) =
            let (Degrees value) = this
            value.ToString(format, formatProvider)
#endif

module internal Degrees =
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
    /// Calculates the angle to another position.
    member x.AngleTo positionTo =
        let dx = positionTo.X - x.X
        let dy = positionTo.Y - x.Y
        let atan2 = Math.Atan2(dy, dx) * 180. / Math.PI
        Degrees.op_Implicit (if atan2 < 0. then atan2 + 360. else atan2)
    /// Calculates the distance to another position.
    member x.DistanceTo positionTo =
        let dx = positionTo.X - x.X
        let dy = positionTo.Y - x.Y
        Math.Sqrt (dx * dx + dy * dy)
    /// Adds two positions by adding their x and y coordinates.
    static member (+) (p1, p2) =
        {
            X = p1.X + p2.X
            Y = p1.Y + p2.Y
        }
    /// Subtracts two positions by subtracting their x and y coordinates.
    static member (-) (p1, p2) =
        {
            X = p1.X - p2.X
            Y = p1.Y - p2.Y
        }
    /// Negates a position by negating its x and y coordinate.
    static member (~-) (p) =
        {
            X = -p.X
            Y = -p.Y
        }
    override this.ToString() = sprintf "(%.2f, %.2f)" this.X this.Y

module internal Position =
    let zero = { X = 0.; Y = 0. }
    let angleTo positionTo (positionFrom: Position) = positionFrom.AngleTo(positionTo)
    let distanceTo positionTo (positionFrom: Position) = positionFrom.DistanceTo(positionTo)

/// Defines a two-dimensional size.
type Size =
    {
        /// The horizontal size.
        Width: float
        /// The vertical size.
        Height: float
    }
    /// Multiplies the width and height of a size with a factor.
    static member (*) (size, factor) =
        {
            Width = size.Width * factor
            Height = size.Height * factor
        }

module internal Size =
    let zero =
        { Width = 0.; Height = 0. }

    let scale boxSize size =
        let widthRatio = boxSize.Width / size.Width
        let heightRatio = boxSize.Height / size.Height
        let ratio = Math.Min(widthRatio, heightRatio)
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

module internal Rectangle =
    let zero =
        { Position = Position.zero; Size = Size.zero }

    let contains position (rectangle: Rectangle) =
        rectangle.Left <= position.X &&
        rectangle.Right >= position.X &&
        rectangle.Top >= position.Y &&
        rectangle.Bottom <= position.Y

    let containsRectangle (inner: Rectangle) (outer: Rectangle) =
        inner.Left >= outer.Left &&
        inner.Right <= outer.Right &&
        inner.Top <= outer.Top &&
        inner.Bottom >= outer.Bottom

module internal Movement =
    let bounceOffWall (wall: Rectangle) (object: Rectangle, direction: Degrees) =
        let newDirection =
            if object.Top > wall.Top && direction > Degrees 0. && direction < Degrees 180. then Degrees.zero - direction
            elif object.Bottom < wall.Bottom && direction > Degrees 180. then Degrees.zero - direction
            else direction
        
        let newDirection =
            if object.Left < wall.Left && newDirection > Degrees 90. && newDirection < Degrees 270. then Degrees 180. - newDirection
            elif object.Right > wall.Right && (newDirection < Degrees 90. || newDirection > Degrees 270.) then Degrees 180. - newDirection
            else newDirection

        if direction <> newDirection then Some newDirection
        else None
