namespace GetIt

open System

/// Defines a player costume.
type Costume =
    {
        /// The size of the costume.
        Size: Size
        /// The definition of the costume in SVG format.
        SvgData: string
    }
    with
        ///<summary>Creates a circle costume.</summary>
        ///<param name="fillColor">The color that is used to fill the circle.</param>
        ///<param name="radius">The radius of the circle.</param>
        static member CreateCircle (fillColor, radius) =
            let size = { Width = 2. * radius; Height = 2. * radius }
            {
                Size = size
                SvgData =
                    sprintf """<svg width="%f" height="%f"><circle cx="%f" cy="%f" r="%f" style="fill:%s;fill-opacity:%f" /></svg>"""
                        size.Width size.Height radius radius radius (RGBAColor.rgbHexNotation fillColor) (RGBAColor.transparency fillColor)
            }

        ///<summary>Creates a polygon costume.</summary>
        ///<param name="fillColor">The color that is used to fill the polygon.</param>
        ///<param name="points">The points that define the polygon.</param>
        static member CreatePolygon (fillColor, [<ParamArray>] points) =
            let points = Array.toList points

            let xs = points |> List.map (fun p -> p.X)
            let ys = points |> List.map (fun p -> p.Y)
            let minX = List.min xs
            let maxX = List.max xs
            let minY = List.min ys
            let maxY = List.max ys

            let pointString =
                points
                |> List.map (fun p -> sprintf "%f,%f" p.X (maxY - p.Y))
                |> String.concat " "

            let size = { Width = maxX - minX; Height = maxY - minY }
            { Size = size
              SvgData =
                sprintf """<svg width="%f" height="%f"><polygon points="%s" style="fill:%s;fill-opacity:%f;" /></svg>"""
                    size.Width size.Height pointString (RGBAColor.rgbHexNotation fillColor) (RGBAColor.transparency fillColor)
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
