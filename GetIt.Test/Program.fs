open Expecto
open GetIt
open System.Drawing
open System.IO

let getScreenshot () =
    let (PngImage imageData) = Game.captureWindowContent ()
    use imageStream = new MemoryStream(imageData)
    // File.WriteAllBytes(System.DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png", imageData)
    new Bitmap(imageStream)

let getColor (color: Color) = (color.R, color.G, color.B, color.A)

module Coordinates =
    let private infoHeight = 50
    let sceneCenter (image: Bitmap) =
        (image.Width / 2, (image.Height - infoHeight) / 2)
    // let relativeToSceneCenter (xOffset, yOffset) image =
    //     let (x, y) = sceneCenter image
    //     (x + xOffset, y + yOffset)
    let fullScene (image: Bitmap) =
        [
            for x in [0 .. image.Width - 1] do
            for y in [0 .. image.Height - infoHeight - 1] -> (x, y)
        ]

let pixelAt coordinates (image: Bitmap) =
    coordinates image
    |> image.GetPixel
    |> getColor

let pixelsAt coordinates (image: Bitmap) =
    coordinates image
    |> List.map (image.GetPixel >> getColor)

let white = getColor Color.White

let tests =
    testSequenced <| testList "Startup" [
        test "Scene should be empty" {
            use _ = Game.ShowScene ()
            let image = getScreenshot ()
            let colors = pixelsAt Coordinates.fullScene image
            Expect.allEqual colors white "All scene pixels should be white"
        }

        test "Turtle should start at scene center" {
            use _ = Game.ShowSceneAndAddTurtle ()
            let image = getScreenshot ()
            let centerPixelColor = pixelAt Coordinates.sceneCenter image
            Expect.notEqual centerPixelColor white "Center pixel should not be white"
        }
    ]

[<EntryPoint>]
let main args =
    System.Environment.SetEnvironmentVariable("GET_IT_UI_CONTAINER_DIRECTORY", @".\GetIt.UI.Container\bin\Debug\netcoreapp3.1")
    runTestsWithCLIArgs [] args tests
