open Expecto
open GetIt
open System.Drawing
open System.IO

let private getScreenshot communicationState =
    let (PngImage imageData) = UICommunication.makeScreenshot UICommunication.ScreenshotCaptureRegion.WindowContent communicationState
    // File.WriteAllBytes(System.DateTime.Now.ToString("yyyyMMdd-HHmmss.fffffff") + ".png", imageData)
    use imageStream = new MemoryStream(imageData)
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

let defaultWindowSize = SpecificSize { Width = 600.; Height = 400. }

let tests =
    testList "Startup" [
        test "Scene should be empty" {
            use state = UICommunication.showScene defaultWindowSize
            let image = getScreenshot state
            let colors = pixelsAt Coordinates.fullScene image
            Expect.allEqual colors white "All scene pixels should be white"
        }

        test "Turtle should start at scene center" {
            use state = UICommunication.showScene defaultWindowSize
            let playerId = UICommunication.addPlayer PlayerData.Turtle state
            let image = getScreenshot state
            let centerPixelColor = pixelAt Coordinates.sceneCenter image
            Expect.notEqual centerPixelColor white "Center pixel should not be white"
        }

        test "Info height is constant" {
            use state = UICommunication.showScene defaultWindowSize
            for _ in [0..10] do
                UICommunication.addPlayer (PlayerData.Turtle.WithVisibility(false)) state |> ignore
            let image = getScreenshot state
            let colors = pixelsAt Coordinates.fullScene image
            Expect.allEqual colors white "All scene pixels should be white"
        }
    ]

[<EntryPoint>]
let main args =
    System.Environment.SetEnvironmentVariable("GET_IT_UI_CONTAINER_DIRECTORY", @".\GetIt.UI.Container\bin\Debug\netcoreapp3.1")
    runTestsWithCLIArgs [] args tests
