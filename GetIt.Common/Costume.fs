namespace GetIt

type GeometryPath =
    { FillColor: RGBA
      Data: string }

type Costume =
    { Size: Size
      Paths: GeometryPath list }

// TODO add C# wrappers
module Costume =
    let createCircle fillColor radius =
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

    let createPolygon fillColor points =
        let points = List.map (fun p -> { p with Y = -p.Y }) points

        let minX = (List.minBy (fun p -> p.X) points).X
        let maxX = (List.maxBy (fun p -> p.X) points).X
        let minY = (List.minBy (fun p -> p.Y) points).Y
        let maxY = (List.maxBy (fun p -> p.Y) points).Y

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

    let createRectangle fillColor size =
        let points =
            [
                { X = 0.; Y = 0. }
                { X = 0.; Y = size.Height }
                { X = size.Width; Y = size.Height }
                { X = size.Width; Y = 0. }
            ]
        createPolygon fillColor points
            