namespace GetIt

open System

/// Defines a colored geometry path.
type GeometryPath = {
    /// Color that is used to fill the path.
    FillColor: RGBAColor
    /// SVG path data (see https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/curves/path-data).
    Data: string
}

/// Defines a player costume.
type Costume =
    {
        /// The size of the costume.
        Size: Size
        /// The paths that define the costume.
        Paths: GeometryPath list
    }
    with
        ///<summary>Creates a circle costume.</summary>
        ///<param name="fillColor">The color that is used to fill the circle.</param>
        ///<param name="radius">The radius of the circle.</param>
        static member CreateCircle (fillColor, radius) =
            {
                Size = { Width = 2. * radius; Height = 2. * radius }
                Paths =
                    [
                        { FillColor = fillColor
                          Data =
                            sprintf "M 0,%f A %f,%f 0 1 0 %f,%f A %f,%f 0 1 0 0,%f"
                                radius radius radius (2. * radius) radius radius radius radius}
                    ]
            }

        ///<summary>Creates a polygon costume.</summary>
        ///<param name="fillColor">The color that is used to fill the polygon.</param>
        ///<param name="points">The points that define the polygon.</param>
        static member CreatePolygon (fillColor, [<ParamArray>] points) =
            let points =
                points
                |> Array.map (fun p -> { p with Y = -p.Y })
                |> List.ofArray

            let xs = points |> List.map (fun p -> p.X)
            let ys = points |> List.map (fun p -> p.Y)
            let minX = List.min xs
            let maxX = List.max xs
            let minY = List.min ys
            let maxY = List.max ys

            let path =
                points
                |> List.mapi (fun i p ->
                    let x = p.X - minX
                    let y = p.Y - minY
                    sprintf "%s %f,%f" (if i > 0 then "L " else "") x y
                )
                |> String.concat " "
                |> sprintf "M %s Z"

            { Size = { Width = maxX - minX; Height = maxY - minY }
              Paths =
                [
                    { FillColor = fillColor; Data = path }
                ]
            }

        ///<summary>Creates a rectangle costume.</summary>
        ///<param name="fillColor">The color that is used to fill the rectangle.</param>
        ///<param name="size">The size of the rectangle.</param>
        static member CreateRectangle (fillColor, size) =
            let points =
                [|
                    { X = 0.; Y = 0. }
                    { X = 0.; Y = size.Height }
                    { X = size.Width; Y = size.Height }
                    { X = size.Width; Y = 0. }
                |]
            Costume.CreatePolygon (fillColor, points)
