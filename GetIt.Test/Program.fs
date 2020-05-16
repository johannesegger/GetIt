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
    let range leftTop rightBottom (image: Bitmap) =
        let (left, top) = leftTop image
        let (right, bottom) = rightBottom image
        [
            for x in [left .. right - 1] do
            for y in [top .. bottom - 1] -> (x, y)
        ]
    let fullScene (image: Bitmap) =
        range (fun image -> (0, 0)) (fun image -> (image.Width, image.Height - infoHeight)) image

let getPixelAt coordinates (image: Bitmap) =
    coordinates image
    |> image.GetPixel
    |> getColor

let getPixelsAt coordinates (image: Bitmap) =
    (Map.empty, coordinates image)
    ||> List.fold (fun state coords ->
        let color = image.GetPixel coords |> getColor
        Map.add coords color state
    )

let createEmptyImage (image: Bitmap) = Map.empty

let setAllScenePixels color imageFn image =
    (imageFn image, Coordinates.fullScene image)
    ||> List.fold (fun img coords ->
        Map.add coords color img
    )

let setPixelsBetween range color imageFn image =
    (imageFn image, range image)
    ||> List.fold (fun img coords ->
        Map.add coords color img
    )

let doCreateImage image imageFn =
    imageFn image

module Map =
    let valueDiff a b =
        (Map.empty, Map.toList a |> List.map fst)
        ||> List.fold (fun result key ->
            let va = Map.find key a
            match Map.tryFind key b with
            | Some vb when va = vb -> result
            | Some vb -> Map.add key (va, vb) result
            | None -> result
        )

let white = getColor Color.White

let defaultWindowSize = SpecificSize { Width = 600.; Height = 400. }

let rectColor = getColor Color.Blue
let (rectWidth, rectHeight) = (50, 20)
let rect =
    let (BlurryColor (r, g, b, a)) = rectColor
    PlayerData.Create(SvgImage.CreateRectangle({ Red = r; Green = g; Blue = b; Alpha = a }, { Width = float rectWidth; Height = float rectHeight; }))

let tests =
    testList "All" [
        testList "Startup" [
            test "Scene should be empty" {
                use state = UICommunication.showScene defaultWindowSize
                let image = getScreenshot state
                let colors = getPixelsAt Coordinates.fullScene image |> Map.toList |> List.map snd
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
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (-rectWidth / 2, -rectHeight / 2)) (Coordinates.relativeToSceneCenter (rectWidth / 2, rectHeight / 2))) rectColor
                    |> doCreateImage image
                let valueDiff = Map.valueDiff actualColors expectedColors
                Expect.isTrue valueDiff.IsEmpty "Scene should have rectangle at the center and everything else empty"
            }

            test "Info height is constant" {
                use state = UICommunication.showScene defaultWindowSize
                for _ in [0..10] do
                    UICommunication.addPlayer (PlayerData.Turtle.WithVisibility(false)) state |> ignore
                let image = getScreenshot state
                let colors = getPixelsAt Coordinates.fullScene image |> Map.toList |> List.map snd
                Expect.allEqual colors white "All scene pixels should be white"
            }
        ]

        testList "Movement" [
            test "Change position" {
                use state = UICommunication.showScene defaultWindowSize
                let playerId = UICommunication.addPlayer rect state
                for _ in [1..10] do
                    UICommunication.changePosition playerId { X = 13.; Y = 7. } state
                let image = getScreenshot state
                let actualColors = getPixelsAt Coordinates.fullScene image
                let expectedColors =
                    createEmptyImage
                    |> setAllScenePixels white
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (130 - rectWidth / 2, -70 - rectHeight / 2)) (Coordinates.relativeToSceneCenter (130 + rectWidth / 2, -70 + rectHeight / 2))) rectColor
                    |> doCreateImage image
                let valueDiff = Map.valueDiff actualColors expectedColors
                Expect.isTrue valueDiff.IsEmpty "Scene should have rectangle at (100, 50) and everything else empty"
            }
        ]
    ]

[<EntryPoint>]
let main args =
    runTestsWithCLIArgs [] args tests
