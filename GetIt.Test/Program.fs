open Expecto
open GetIt
open System.Drawing
open System.IO

let private getScreenshot communicationState =
    let (PngImage imageData) = UICommunication.makeScreenshot UICommunication.ScreenshotCaptureRegion.WindowContent communicationState
    // File.WriteAllBytes(System.DateTime.Now.ToString("yyyyMMdd-HHmmss.fffffff") + ".png", imageData)
    use imageStream = new MemoryStream(imageData)
    new Bitmap(imageStream)

[<CustomEquality; NoComparison>]
type BlurryColor =
    BlurryColor of (byte * byte * byte * byte)
        override x.Equals(y) =
            let areNumbersEqual n1 n2 =
                if n1 > n2 then n1 - n2 < 0xFuy
                else n2 - n1 < 0xFuy
            let isEqual (BlurryColor (r1, g1, b1, a1)) (BlurryColor (r2, g2, b2, a2)) =
                areNumbersEqual r1 r2 && areNumbersEqual g1 g2 && areNumbersEqual b1 b2 && areNumbersEqual a1 a2
            match y with
            | :? BlurryColor as y -> isEqual x y
            | _ -> false
        override x.GetHashCode() = 0

let getColor (color: Color) = BlurryColor (color.R, color.G, color.B, color.A)

module Coordinates =
    let private infoHeight = 50
    let sceneCenter (image: Bitmap) =
        (image.Width / 2, (image.Height - infoHeight) / 2)
    let relativeToSceneCenter (xOffset, yOffset) image =
        let (x, y) = sceneCenter image
        (x + xOffset, y + yOffset)
    let fullScene (image: Bitmap) =
        [
            for x in [0 .. image.Width - 1] do
            for y in [0 .. image.Height - infoHeight - 1] -> (x, y)
        ]
    let isOnScene (x, y) (image: Bitmap) =
        x >= 0 && x < image.Width && y >= 0 && y < image.Height - infoHeight

let getPixelAt coordinates (image: Bitmap) =
    coordinates image
    |> image.GetPixel
    |> getColor

let getPixelsAt coordinates (image: Bitmap) =
    coordinates image
    |> List.map (fun (x, y) ->
        let color = image.GetPixel(x, y) |> getColor
        ((x, y), color)
    )

let createEmptyImage (image: Bitmap) =
    [
        for x in [0 .. image.Width - 1] do
        for y in [0 .. image.Height - 1] -> ((x, y), None)
    ]

let setAllScenePixels color imageFn image =
    imageFn image
    |> List.map (fun (coords, oldColor) ->
        if Coordinates.isOnScene coords image then (coords, Some color)
        else (coords, oldColor)
    )

let setPixelsBetween leftTop rightBottom color imageFn image =
    let (left, top) = leftTop image
    let (right, bottom) = rightBottom image
    imageFn image
    |> List.map (fun ((x, y), oldColor) ->
        if x >= left && x < right && y >= top && y < bottom then ((x, y), Some color)
        else ((x, y), oldColor)
    )

let getImagePixels image imageFn =
    imageFn image
    |> List.choose (function
        | (coords, Some color) -> Some (coords, color)
        | _ -> None
    )

let white = getColor Color.White

let defaultWindowSize = SpecificSize { Width = 600.; Height = 400. }

let rectColor = getColor Color.Blue
let (rectWidth, rectHeight) = (50, 20)
let rect =
    let (BlurryColor (r, g, b, a)) = rectColor
    PlayerData.Create(SvgImage.CreateRectangle({ Red = r; Green = g; Blue = b; Alpha = a }, { Width = float rectWidth; Height = float rectHeight; }))

let tests =
    testSequenced <| testList "All" [
        testList "Startup" [
            test "Scene should be empty" {
                use state = UICommunication.showScene defaultWindowSize
                let image = getScreenshot state
                let colors = getPixelsAt Coordinates.fullScene image |> List.map snd
                Expect.allEqual colors white "All scene pixels should be white"
            }

            test "Player should start at scene center" {
                use state = UICommunication.showScene defaultWindowSize
                let playerId = UICommunication.addPlayer rect state
                let image = getScreenshot state
                let actualColors = getPixelsAt Coordinates.fullScene image
                let expectedColors =
                    createEmptyImage
                    |> setAllScenePixels white
                    |> setPixelsBetween (Coordinates.relativeToSceneCenter (-rectWidth / 2, -rectHeight / 2)) (Coordinates.relativeToSceneCenter (rectWidth / 2, rectHeight / 2)) rectColor
                    |> getImagePixels image
                Expect.equal expectedColors actualColors "Scene should have rectangle at the center and everything else empty"
            }

            test "Info height is constant" {
                use state = UICommunication.showScene defaultWindowSize
                for _ in [0..10] do
                    UICommunication.addPlayer (PlayerData.Turtle.WithVisibility(false)) state |> ignore
                let image = getScreenshot state
                let colors = getPixelsAt Coordinates.fullScene image |> List.map snd
                Expect.allEqual colors white "All scene pixels should be white"
            }
        ]

        testList "Movement" [
            test "Move forward works" {
                use state = UICommunication.showScene defaultWindowSize
                let playerId = UICommunication.addPlayer PlayerData.Turtle state
                UICommunication.changePosition playerId { X = 100.; Y = 0. } state
                let image = getScreenshot state
                let centerPixelColor = getPixelAt Coordinates.sceneCenter image
                Expect.equal centerPixelColor white "Center pixel should be white"
                let turtlePixelColor = getPixelAt (Coordinates.relativeToSceneCenter (100, 0)) image
                Expect.notEqual turtlePixelColor white "Turtle pixel should not be white"
            }
        ]
    ]

[<EntryPoint>]
let main args =
    runTestsWithCLIArgs [] args tests
