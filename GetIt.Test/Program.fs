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
let red = getColor Color.Red
let blue = getColor Color.Blue
let black = getColor Color.Black

let defaultWindowSize = SpecificSize { Width = 600.; Height = 400. }

let rectColor = blue
let (rectWidth, rectHeight) = (50, 20)
let rect =
    let (r, g, b, a) = rectColor
    PlayerData.Create(SvgImage.CreateRectangle({ Red = r; Green = g; Blue = b; Alpha = a }, { Width = float rectWidth; Height = float rectHeight }))

let tests =
    testSequenced <| testList "All" [
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

            test "Rotate around center" {
                use state = UICommunication.showScene defaultWindowSize
                let playerId = UICommunication.addPlayer rect state
                UICommunication.setPosition playerId { X = 10.; Y = 30. } state
                UICommunication.setDirection playerId (Degrees.op_Implicit 90.) state
                let image = getScreenshot state
                let actualColors = getPixelsAt Coordinates.fullScene image
                let expectedColors =
                    createEmptyImage
                    |> setAllScenePixels white
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (10 - rectHeight / 2, -30 - rectWidth / 2)) (Coordinates.relativeToSceneCenter (10 + rectHeight / 2, -30 + rectWidth / 2))) rectColor
                    |> doCreateImage image
                let valueDiff = Map.valueDiff actualColors expectedColors
                Expect.isTrue valueDiff.IsEmpty "Scene should have rectangle at (10, 30) turned up and everything else empty"
            }
        ]

        testList "Pen" [
            test "Line position" {
                use state = UICommunication.showScene defaultWindowSize
                let playerId = UICommunication.addPlayer { rect with Pen = { IsOn = true; Weight = 50.; Color = RGBAColors.red }; IsVisible = false } state
                UICommunication.setPosition playerId { X = 100.; Y = 0. } state
                let image = getScreenshot state
                let actualColors = getPixelsAt Coordinates.fullScene image
                let expectedColors =
                    createEmptyImage
                    |> setAllScenePixels white
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (0, -25)) (Coordinates.relativeToSceneCenter (100, 25))) red
                    |> doCreateImage image
                let valueDiff = Map.valueDiff actualColors expectedColors
                Expect.isTrue valueDiff.IsEmpty "Scene should have 50px wide line from (0, 0) to (0, 100)"
            }

            test "Toggle pen state" {
                use state = UICommunication.showScene defaultWindowSize
                let playerId = UICommunication.addPlayer { rect with Pen = { rect.Pen with Weight = 2. }; IsVisible = false } state
                UICommunication.setPosition playerId { X = -100.; Y = 0. } state
                UICommunication.setPenState playerId true state
                UICommunication.setPosition playerId { X = -100.; Y = 100. } state
                UICommunication.setPosition playerId { X = 0.; Y = 100. } state
                UICommunication.setPenState playerId false state
                UICommunication.setPosition playerId { X = 0.; Y = 0. } state
                UICommunication.setPenState playerId true state
                UICommunication.setPosition playerId { X = 100.; Y = 0. } state
                UICommunication.setPosition playerId { X = 100.; Y = 100. } state
                let image = getScreenshot state
                let actualColors = getPixelsAt Coordinates.fullScene image
                let expectedColors =
                    createEmptyImage
                    |> setAllScenePixels white
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (-101, -100)) (Coordinates.relativeToSceneCenter (-99, 0))) black
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (-100, -101)) (Coordinates.relativeToSceneCenter (0, -99))) black
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (0, -1)) (Coordinates.relativeToSceneCenter (100, 1))) black
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (99, -100)) (Coordinates.relativeToSceneCenter (101, 0))) black
                    |> doCreateImage image
                let valueDiff = Map.valueDiff actualColors expectedColors
                Expect.isTrue valueDiff.IsEmpty "Scene should have 2px wide line like ┍ ┙"
            }

            test "Clear pen lines" {
                use state = UICommunication.showScene defaultWindowSize
                let playerId = UICommunication.addPlayer { rect with Pen = { IsOn = true; Weight = 50.; Color = RGBAColors.red }; IsVisible = false } state
                UICommunication.changePosition playerId { X = 100.; Y = 0. } state
                UICommunication.changePosition playerId { X = -100.; Y = 100. } state
                UICommunication.changePosition playerId { X = -100.; Y = -100. } state
                UICommunication.changePosition playerId { X = 100.; Y = 0. } state
                UICommunication.clearScene state
                let image = getScreenshot state
                let colors = getPixelsAt Coordinates.fullScene image |> Map.toList |> List.map snd
                Expect.allEqual colors white "All scene pixels should be white"
            }
        ]

        testList "Look" [
            test "Next costume" {
                use state = UICommunication.showScene defaultWindowSize
                let playerData =
                    PlayerData.Create([
                        SvgImage.CreateRectangle(RGBAColors.blue, { Width = float rectWidth; Height = float rectHeight })
                        SvgImage.CreateRectangle(RGBAColors.red, { Width = float rectWidth; Height = float rectHeight })
                    ])
                let playerId = UICommunication.addPlayer playerData state
                UICommunication.setNextCostume playerId state
                let image = getScreenshot state
                let actualColors = getPixelsAt Coordinates.fullScene image
                let expectedColors =
                    createEmptyImage
                    |> setAllScenePixels white
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (-rectWidth / 2, -rectHeight / 2)) (Coordinates.relativeToSceneCenter (rectWidth / 2, rectHeight / 2))) red
                    |> doCreateImage image
                let valueDiff = Map.valueDiff actualColors expectedColors
                Expect.isTrue valueDiff.IsEmpty "Scene should have red rectangular player at center"
            }

            test "Next next costume" {
                use state = UICommunication.showScene defaultWindowSize
                let playerData =
                    PlayerData.Create([
                        SvgImage.CreateRectangle(RGBAColors.blue, { Width = float rectWidth; Height = float rectHeight })
                        SvgImage.CreateRectangle(RGBAColors.red, { Width = float rectWidth; Height = float rectHeight })
                    ])
                let playerId = UICommunication.addPlayer playerData state
                UICommunication.setNextCostume playerId state
                UICommunication.setNextCostume playerId state
                let image = getScreenshot state
                let actualColors = getPixelsAt Coordinates.fullScene image
                let expectedColors =
                    createEmptyImage
                    |> setAllScenePixels white
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (-rectWidth / 2, -rectHeight / 2)) (Coordinates.relativeToSceneCenter (rectWidth / 2, rectHeight / 2))) blue
                    |> doCreateImage image
                let valueDiff = Map.valueDiff actualColors expectedColors
                Expect.isTrue valueDiff.IsEmpty "Scene should have blue rectangular player at center"
            }

            test "Size factor" {
                use state = UICommunication.showScene defaultWindowSize
                let playerId = UICommunication.addPlayer { rect with SizeFactor = 2. } state
                let image = getScreenshot state
                let actualColors = getPixelsAt Coordinates.fullScene image
                let expectedColors =
                    createEmptyImage
                    |> setAllScenePixels white
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (-rectWidth, -rectHeight)) (Coordinates.relativeToSceneCenter (rectWidth, rectHeight))) rectColor
                    |> doCreateImage image
                let valueDiff = Map.valueDiff actualColors expectedColors
                Expect.isTrue valueDiff.IsEmpty "Scene should have blue rectangular player at center"
            }

            test "Polygon costume" {
                use state = UICommunication.showScene defaultWindowSize
                let playerData =
                    PlayerData.Create([
                        SvgImage.CreatePolygon(
                            RGBAColors.blue,
                            [|
                                { X = -15.; Y = 15. }
                                { X = -15.; Y = 65. }
                                { X = 15.; Y = 65. }
                                { X = 15.; Y = 15. }
                                { X = 45.; Y = 15. }
                                { X = 45.; Y = -15. }
                                { X = 15.; Y = -15. }
                                { X = 15.; Y = -65. }
                                { X = -15.; Y = -65. }
                                { X = -15.; Y = -15. }
                                { X = -45.; Y = -15. }
                                { X = -45.; Y = 15. }
                                { X = -15.; Y = 15. }
                            |]
                        )
                    ])
                let playerId = UICommunication.addPlayer playerData state
                let image = getScreenshot state
                let actualColors = getPixelsAt Coordinates.fullScene image
                let expectedColors =
                    createEmptyImage
                    |> setAllScenePixels white
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (-15, -65)) (Coordinates.relativeToSceneCenter (15, 65))) blue
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (-45, -15)) (Coordinates.relativeToSceneCenter (45, 15))) blue
                    |> doCreateImage image
                let valueDiff = Map.valueDiff actualColors expectedColors
                Expect.isTrue valueDiff.IsEmpty "Scene should have blue plus-like player at center"
            }

            test "Speech bubble shows" {
                use state = UICommunication.showScene defaultWindowSize
                let playerId = UICommunication.addPlayer (PlayerData.Create(SvgImage.CreateRectangle(RGBAColors.black, Size.zero))) state
                UICommunication.say playerId "Hi" state
                let image = getScreenshot state
                let colors = getPixelsAt Coordinates.fullScene image |> Map.toList |> List.map snd
                Expect.exists colors ((<>) white) "Scene should have non-white pixels"
            }

            test "Speech bubble hides" {
                use state = UICommunication.showScene defaultWindowSize
                let playerId = UICommunication.addPlayer (PlayerData.Create(SvgImage.CreateRectangle(RGBAColors.black, Size.zero))) state
                UICommunication.say playerId "Hi" state
                UICommunication.shutUp playerId state
                let image = getScreenshot state
                let colors = getPixelsAt Coordinates.fullScene image |> Map.toList |> List.map snd
                Expect.allEqual colors white "All scene pixels should be white"
            }
        ]
    ]

[<EntryPoint>]
let main args =
    runTestsWithCLIArgs [] args tests
