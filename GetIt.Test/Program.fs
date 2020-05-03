open Expecto
open GetIt
open System.Drawing
open System.IO

let getScreenshot () =
    let (PngImage imageData) = Game.makeScreenshot ()
    use imageStream = new MemoryStream(imageData)
    // File.WriteAllBytes(System.DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png", imageData)
    new Bitmap(imageStream)

let getColor (color: Color) = (color.R, color.G, color.B, color.A)

let pixelAt (x, y) (image: Bitmap) =
    image.GetPixel(x, y) |> getColor

let white = getColor Color.White

let tests =
    test "Turtle should start at scene center" {
        use _ = Game.ShowSceneAndAddTurtle ()
        let image = getScreenshot ()
        let centerPixelColor = pixelAt (image.Width / 2, image.Height / 2) image
        Expect.notEqual centerPixelColor white "Center pixel should not be white"
    }

[<EntryPoint>]
let main args =
    System.Environment.SetEnvironmentVariable("GET_IT_TEST", "1")
    runTestsWithCLIArgs [] args tests
