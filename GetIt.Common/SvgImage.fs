namespace GetIt

open System
open System.IO

/// Defines an image in scalable vector graphics format.
type SvgImage =
    {
        /// The size of the image.
        Size: Size
        /// The definition of the image in scalable vector graphics format.
        SvgData: string
    }
    with
        ///<summary>Creates a circle image.</summary>
        ///<param name="fillColor">The color that is used to fill the circle.</param>
        ///<param name="radius">The radius of the circle.</param>
        static member CreateCircle (fillColor, radius) =
            let size = { Width = 2. * radius; Height = 2. * radius }
            {
                Size = size
                SvgData =
                    sprintf """<svg xmlns="http://www.w3.org/2000/svg" width="%f" height="%f"><circle cx="%f" cy="%f" r="%f" style="fill:%s;fill-opacity:%f" /></svg>"""
                        size.Width size.Height radius radius radius (RGBAColor.rgbHexNotation fillColor) (RGBAColor.transparency fillColor)
            }

        ///<summary>Creates a polygon image.</summary>
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
            {
                Size = size
                SvgData =
                    sprintf """<svg xmlns="http://www.w3.org/2000/svg" width="%f" height="%f"><polygon points="%s" style="fill:%s;fill-opacity:%f;" /></svg>"""
                        size.Width size.Height pointString (RGBAColor.rgbHexNotation fillColor) (RGBAColor.transparency fillColor)
            }

        ///<summary>Creates a rectangle image.</summary>
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
            SvgImage.CreatePolygon (fillColor, points)

#if !FABLE_COMPILER
        ///<summary>Loads an image from an SVG file.</summary>
        ///<param name="path">The path to the SVG file.</param>
        static member Load (path) =
            try
                let content = File.ReadAllText path
                let (width, height) = Svg.getSizeFromSvgDocument content
                {
                    Size = { Width = width; Height = height }
                    SvgData = content
                }
            with e -> raise (GetItException (sprintf "Error while loading costume from path \"%s\"" path, e))
#endif
