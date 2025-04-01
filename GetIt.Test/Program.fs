module internal Program

open Expecto
open GetIt
open SkiaSharp
open System
open System.Drawing
open System.IO

module internal Win32 =
    open System.Runtime.InteropServices

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type WinPoint =
        val mutable x: int
        val mutable y: int

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type Rect =
        val mutable Left: int
        val mutable Top: int
        val mutable Right: int
        val mutable Bottom: int

    type WindowPlacementFlags =
        | WPF_ASYNCWINDOWPLACEMENT = 0x04u
        | WPF_RESTORETOMAXIMIZED = 0x02u
        | WPF_SETMINPOSITION = 0x01u

    type WindowPlacementShowCommand =
        | SW_HIDE = 0u
        | SW_MAXIMIZE = 3u
        | SW_MINIMIZE = 6u
        | SW_RESTORE = 9u
        | SW_SHOW = 5u
        | SW_SHOWMAXIMIZED = 3u
        | SW_SHOWMINIMIZED = 2u
        | SW_SHOWMINNOACTIVE = 7u
        | SW_SHOWNA = 8u
        | SW_SHOWNOACTIVATE = 4u
        | SW_SHOWNORMAL = 1u

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type WINDOWPLACEMENT =
        val mutable Length: uint32
        val mutable Flags: WindowPlacementFlags
        val mutable ShowCmd: WindowPlacementShowCommand
        val mutable PtMinPosition: WinPoint
        val mutable PtMaxPosition: WinPoint
        val mutable RcNormalPosition: Rect
        val mutable RcDevice: Rect

    [<DllImport("user32.dll", SetLastError = true)>]
    extern bool GetWindowPlacement(IntPtr hWnd, WINDOWPLACEMENT& lpwndpl)

let private getScreenshot communicationState =
    let (PngImage imageData) = UICommunication.makeScreenshot communicationState
    // File.WriteAllBytes(sprintf "%s-%d.png" (System.DateTime.Now.ToString("yyyyMMdd-HHmmss.fffffff")) communicationState.UIWindowProcess.Id, imageData)
    let image = SKImage.FromEncodedData imageData
    let bitmap = SKBitmap.FromImage image
    bitmap

let getColor (color: SKColor) = (color.Red, color.Green, color.Blue, color.Alpha)

module Coordinates =
    let private infoHeight = 50
    let sceneCenter (image: SKBitmap) =
        image.Width / 2, (image.Height - infoHeight) / 2
    let relativeToSceneCenter (xOffset, yOffset) image =
        let (x, y) = sceneCenter image
        x + xOffset, y + yOffset
    let range leftTop rightBottom (image: SKBitmap) =
        let (left, top) = leftTop image
        let (right, bottom) = rightBottom image
        [
            for x in [left .. right - 1] do
            for y in [top .. bottom - 1] -> (x, y)
        ]
    let fullScene (image: SKBitmap) =
        range (fun image -> (0, 0)) (fun image -> (image.Width, image.Height - infoHeight)) image
    let infoSection (image: SKBitmap) =
        range (fun image -> (0, image.Height - infoHeight + 1)) (fun image -> (image.Width, image.Height)) image

let getPixelAt coordinates (image: SKBitmap) =
    coordinates image
    |> image.GetPixel
    |> getColor

let getPixelsAt coordinates (image: SKBitmap) =
    (Map.empty, coordinates image)
    ||> List.fold (fun state coords ->
        let color = image.GetPixel coords |> getColor
        Map.add coords color state
    )

let createEmptyImage (image: SKBitmap) = Map.empty

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

let white = getColor SKColors.White
let red = getColor SKColors.Red
let blue = getColor SKColors.Blue
let black = getColor SKColors.Black

let defaultSceneSize = SpecificSize { Width = 600.; Height = 400. }

let rectColor = blue
let (rectWidth, rectHeight) = (50, 20)
let rect =
    let (r, g, b, a) = rectColor
    PlayerData.Create(SvgImage.CreateRectangle({ Red = r; Green = g; Blue = b; Alpha = a }, { Width = float rectWidth; Height = float rectHeight }))

let tests =
    testList "All" [
        testList "Startup" [
            test "Scene should be empty" {
                use state = UICommunication.showScene defaultSceneSize
                let image = getScreenshot state
                let colors = getPixelsAt Coordinates.fullScene image |> Map.toList |> List.map snd
                Expect.allEqual colors white "All scene pixels should be white"
            }

            yield!
                [(50, 20); (10, 10); (2, 100); (100, 2)]
                |> List.map (fun (width, height) ->
                    test (sprintf "Player (%d x %d) should start at scene center" width height) {
                        use state = UICommunication.showScene defaultSceneSize
                        let playerData = PlayerData.Create(SvgImage.CreateRectangle(RGBAColors.blue, { Width = float width; Height = float height }))
                        let playerId = UICommunication.addPlayer playerData state
                        let image = getScreenshot state
                        let actualColors = getPixelsAt Coordinates.fullScene image
                        let expectedColors =
                            createEmptyImage
                            |> setAllScenePixels white
                            |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (-width / 2, -height / 2)) (Coordinates.relativeToSceneCenter (width / 2, height / 2))) blue
                            |> doCreateImage image
                        let valueDiff = Map.valueDiff actualColors expectedColors
                        Expect.isTrue valueDiff.IsEmpty "Scene should have rectangle at the center and everything else empty"
                    }
                )

            test "Info height is constant" {
                use state = UICommunication.showScene defaultSceneSize
                for _ in [0..10] do
                    UICommunication.addPlayer (PlayerData.Turtle.WithVisibility(false)) state |> ignore
                let image = getScreenshot state
                let colors = getPixelsAt Coordinates.fullScene image |> Map.toList |> List.map snd
                Expect.allEqual colors white "All scene pixels should be white"
            }
        ]

        testList "Movement" [
            test "Change position" {
                use state = UICommunication.showScene defaultSceneSize
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
                Expect.isTrue valueDiff.IsEmpty "Scene should have rectangle at (130, 70) and everything else empty"
            }

            test "Rotate around center" {
                use state = UICommunication.showScene defaultSceneSize
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
                use state = UICommunication.showScene defaultSceneSize
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
                use state = UICommunication.showScene defaultSceneSize
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
                use state = UICommunication.showScene defaultSceneSize
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

            test "Line order" {
                use state = UICommunication.showScene defaultSceneSize
                let playerId = UICommunication.addPlayer { rect with Position = { X = -50.; Y = 0. }; Pen = { IsOn = true; Weight = 2.; Color = RGBAColors.red }; IsVisible = false } state
                UICommunication.changePosition playerId { X = 100.; Y = 0. } state
                UICommunication.setPenState playerId false state
                UICommunication.changePosition playerId { X = -50.; Y = 50. } state
                UICommunication.setPenState playerId true state
                UICommunication.setPenColor playerId RGBAColors.blue state
                UICommunication.changePosition playerId { X = 0.; Y = -100. } state
                let image = getScreenshot state
                let actualColors = getPixelsAt Coordinates.fullScene image
                let expectedColors =
                    createEmptyImage
                    |> setAllScenePixels white
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (-50, -1)) (Coordinates.relativeToSceneCenter (50, 1))) red
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (-1, -50)) (Coordinates.relativeToSceneCenter (1, 50))) blue
                    |> doCreateImage image
                let valueDiff = Map.valueDiff actualColors expectedColors
                Expect.isTrue valueDiff.IsEmpty "Blue vertical line should be above red horizontal line"
            }
        ]

        testList "Look" [
            test "Hidden" {
                use state = UICommunication.showScene defaultSceneSize
                let playerId = UICommunication.addPlayer { rect with IsVisible = false } state
                let image = getScreenshot state
                let colors = getPixelsAt Coordinates.fullScene image |> Map.toList |> List.map snd
                Expect.allEqual colors white "All scene pixels should be white"
            }

            test "Next costume" {
                use state = UICommunication.showScene defaultSceneSize
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
                use state = UICommunication.showScene defaultSceneSize
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
                use state = UICommunication.showScene defaultSceneSize
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
                use state = UICommunication.showScene defaultSceneSize
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

            test "Costume from file" {
                use state = UICommunication.showScene defaultSceneSize
                let playerId = UICommunication.addPlayer (PlayerData.Create(SvgImage.Load (Path.Combine(__SOURCE_DIRECTORY__, "data", "rect-costume.svg")))) state
                let image = getScreenshot state
                let actualColors = getPixelsAt Coordinates.fullScene image
                let expectedColors =
                    createEmptyImage
                    |> setAllScenePixels white
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (-25, -10)) (Coordinates.relativeToSceneCenter (25, 10))) blue
                    |> doCreateImage image
                let valueDiff = Map.valueDiff actualColors expectedColors
                Expect.isTrue valueDiff.IsEmpty "Scene should have blue rectangle at center"
            }

            test "Speech bubble shows" {
                use state = UICommunication.showScene defaultSceneSize
                let playerId = UICommunication.addPlayer (PlayerData.Create(SvgImage.CreateRectangle(RGBAColors.black, Size.zero))) state
                UICommunication.say playerId "Hi" state
                let image = getScreenshot state
                let colors = getPixelsAt Coordinates.fullScene image |> Map.toList |> List.map snd
                Expect.exists colors ((<>) white) "Scene should have non-white pixels"
            }

            test "Speech bubble hides" {
                use state = UICommunication.showScene defaultSceneSize
                let playerId = UICommunication.addPlayer (PlayerData.Create(SvgImage.CreateRectangle(RGBAColors.black, Size.zero))) state
                UICommunication.say playerId "Hi" state
                UICommunication.shutUp playerId state
                let image = getScreenshot state
                let colors = getPixelsAt Coordinates.fullScene image |> Map.toList |> List.map snd
                Expect.allEqual colors white "All scene pixels should be white"
            }
        ]

        testList "Layer" [
            yield!
                [
                    test "Initial player layer", (fun _ _ -> ())
                    test "Bring player at top layer to front", fun (player1Id, player2Id, player3Id) -> UICommunication.bringToFront player1Id
                    test "Mix bring to front with order 1,2,3", fun (player1Id, player2Id, player3Id) state ->
                        UICommunication.bringToFront player1Id state
                        UICommunication.bringToFront player2Id state
                        UICommunication.bringToFront player3Id state
                        UICommunication.bringToFront player3Id state
                        UICommunication.bringToFront player3Id state
                        UICommunication.bringToFront player3Id state
                        UICommunication.bringToFront player2Id state
                        UICommunication.bringToFront player1Id state
                    test "Send player at bottom layer to back", fun (player1Id, player2Id, player3Id) -> UICommunication.sendToBack player3Id
                    test "Mix send to back with order 1,2,3", fun (player1Id, player2Id, player3Id) state ->
                        UICommunication.sendToBack player3Id state
                        UICommunication.sendToBack player2Id state
                        UICommunication.sendToBack player1Id state
                        UICommunication.sendToBack player1Id state
                        UICommunication.sendToBack player1Id state
                        UICommunication.sendToBack player2Id state
                        UICommunication.sendToBack player3Id state
                ]
                |> List.map (fun (test, fn) ->
                    test {
                        use state = UICommunication.showScene defaultSceneSize
                        let player1Id = UICommunication.addPlayer (PlayerData.Create(SvgImage.CreateRectangle(RGBAColors.red, { Width = 50.; Height = 20. })).WithPosition({ X = -10.; Y = 0. })) state
                        let player2Id = UICommunication.addPlayer (PlayerData.Create(SvgImage.CreateRectangle(RGBAColors.blue, { Width = 50.; Height = 20. })).WithPosition({ X = 0.; Y = 0. })) state
                        let player3Id = UICommunication.addPlayer (PlayerData.Create(SvgImage.CreateRectangle(RGBAColors.black, { Width = 50.; Height = 20. })).WithPosition({ X = 10.; Y = 0. })) state
                        fn (player1Id, player2Id, player3Id) state
                        let image = getScreenshot state
                        let actualColors = getPixelsAt Coordinates.fullScene image
                        let expectedColors =
                            createEmptyImage
                            |> setAllScenePixels white
                            |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (-35, -10)) (Coordinates.relativeToSceneCenter (15, 10))) red
                            |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (15, -10)) (Coordinates.relativeToSceneCenter (25, 10))) blue
                            |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (25, -10)) (Coordinates.relativeToSceneCenter (35, 10))) black
                            |> doCreateImage image
                        let valueDiff = Map.valueDiff actualColors expectedColors
                        Expect.isTrue valueDiff.IsEmpty "Scene should have red player in front of blue player and blue player in front of black player"
                    }
                )

            yield!
                [
                    test "Bring player at bottom layer to front", fun (player1Id, player2Id, player3Id) -> UICommunication.bringToFront player3Id
                    test "Send players at higher layer to back", fun (player1Id, player2Id, player3Id) state ->
                        UICommunication.sendToBack player1Id state
                        UICommunication.sendToBack player2Id state
                    test "Mix bring to front with order 3,1,2", fun (player1Id, player2Id, player3Id) state ->
                        UICommunication.bringToFront player3Id state
                        UICommunication.bringToFront player2Id state
                        UICommunication.bringToFront player1Id state
                        UICommunication.bringToFront player1Id state
                        UICommunication.bringToFront player1Id state
                        UICommunication.bringToFront player2Id state
                        UICommunication.bringToFront player1Id state
                        UICommunication.bringToFront player3Id state
                    test "Mix send to back with order 3,1,2", fun (player1Id, player2Id, player3Id) state ->
                        UICommunication.sendToBack player1Id state
                        UICommunication.sendToBack player2Id state
                        UICommunication.sendToBack player3Id state
                        UICommunication.sendToBack player3Id state
                        UICommunication.sendToBack player3Id state
                        UICommunication.sendToBack player3Id state
                        UICommunication.sendToBack player1Id state
                        UICommunication.sendToBack player2Id state
                ]
                |> List.map (fun (test, fn) ->
                    test {
                        use state = UICommunication.showScene defaultSceneSize
                        let player1Id = UICommunication.addPlayer (PlayerData.Create(SvgImage.CreateRectangle(RGBAColors.red, { Width = 50.; Height = 20. })).WithPosition({ X = -10.; Y = 0. })) state
                        let player2Id = UICommunication.addPlayer (PlayerData.Create(SvgImage.CreateRectangle(RGBAColors.blue, { Width = 50.; Height = 20. })).WithPosition({ X = 0.; Y = 0. })) state
                        let player3Id = UICommunication.addPlayer (PlayerData.Create(SvgImage.CreateRectangle(RGBAColors.black, { Width = 50.; Height = 20. })).WithPosition({ X = 10.; Y = 0. })) state
                        fn (player1Id, player2Id, player3Id) state
                        let image = getScreenshot state
                        let actualColors = getPixelsAt Coordinates.fullScene image
                        let expectedColors =
                            createEmptyImage
                            |> setAllScenePixels white
                            |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (-35, -10)) (Coordinates.relativeToSceneCenter (-15, 10))) red
                            |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (-15, -10)) (Coordinates.relativeToSceneCenter (35, 10))) black
                            |> doCreateImage image
                        let valueDiff = Map.valueDiff actualColors expectedColors
                        Expect.isTrue valueDiff.IsEmpty "Scene should have black player in front of red player and red player in front of blue player"
                    }
                )
        ]

        test "Remove player" {
            use state = UICommunication.showScene defaultSceneSize
            let playerId = UICommunication.addPlayer rect state
            UICommunication.removePlayer playerId state
            let image = getScreenshot state
            let colors = getPixelsAt Coordinates.fullScene image |> Map.toList |> List.map snd
            Expect.allEqual colors white "All scene pixels should be white"
            let colors = getPixelsAt Coordinates.infoSection image |> Map.toList |> List.distinctBy snd
            Expect.hasLength colors 1 "All info section pixels should have same color"
        }

        testList "Window size" [
            yield!
                [ (150., 200.); (200., 100.); (500., 500.) ]
                |> List.map (fun (width, height) ->
                    test (sprintf "Specific size (%f, %f)" width height) {
                        use state = UICommunication.showScene (SpecificSize { Width = width; Height = height })
                        let image = getScreenshot state
                        Expect.equal (float image.Width, float image.Height) (width, height) "Actual scene size should match desired size"
                    }
                )

            test "Maximized" {
                use state = UICommunication.showScene Maximized
                let mutable result = Win32.WINDOWPLACEMENT()
                result.Length <- uint32 <| System.Runtime.InteropServices.Marshal.SizeOf(result)
                if not <| Win32.GetWindowPlacement(state.UIWindowProcess.MainWindowHandle, &result)
                then raise (System.ComponentModel.Win32Exception("Failed to get window placement (GetWindowPlacement returned false)"))
                Expect.isTrue (result.ShowCmd.HasFlag(Win32.WindowPlacementShowCommand.SW_MAXIMIZE)) "Window should be maximized"
            }
        ]

        testList "Background" [
            test "Background is stretching" {
                use state = UICommunication.showScene defaultSceneSize
                UICommunication.setBackground (SvgImage.CreateRectangle(RGBAColors.red, { Width = 1.; Height = 1. })) state
                let image = getScreenshot state
                let colors = getPixelsAt Coordinates.fullScene image |> Map.toList |> List.map snd
                Expect.allEqual colors red "All scene pixels should be red"
            }

            test "Player is in front of background" {
                use state = UICommunication.showScene defaultSceneSize
                let playerId = UICommunication.addPlayer rect state
                UICommunication.setBackground (SvgImage.CreateRectangle(RGBAColors.red, { Width = 1.; Height = 1. })) state
                let image = getScreenshot state
                let actualColors = getPixelsAt Coordinates.fullScene image
                let expectedColors =
                    createEmptyImage
                    |> setAllScenePixels red
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (-rectWidth / 2, -rectHeight / 2)) (Coordinates.relativeToSceneCenter (rectWidth / 2, rectHeight / 2))) rectColor
                    |> doCreateImage image
                let valueDiff = Map.valueDiff actualColors expectedColors
                Expect.isTrue valueDiff.IsEmpty "Scene should have rectangle at the center and everything else should be background"
            }

            test "Pen line is in front of background" {
                use state = UICommunication.showScene defaultSceneSize
                let playerId = UICommunication.addPlayer { rect with Pen = { IsOn = true; Weight = 50.; Color = RGBAColors.black }; IsVisible = false } state
                UICommunication.setPosition playerId { X = 100.; Y = 0. } state
                UICommunication.setBackground (SvgImage.CreateRectangle(RGBAColors.red, { Width = 1.; Height = 1. })) state
                let image = getScreenshot state
                let actualColors = getPixelsAt Coordinates.fullScene image
                let expectedColors =
                    createEmptyImage
                    |> setAllScenePixels red
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (0, -25)) (Coordinates.relativeToSceneCenter (100, 25))) black
                    |> doCreateImage image
                let valueDiff = Map.valueDiff actualColors expectedColors
                Expect.isTrue valueDiff.IsEmpty "Scene should have 50px wide line from (0, 0) to (0, 100) and everything else should be background"
            }

            test "Speech bubble is in front of background" {
                use state = UICommunication.showScene defaultSceneSize
                let playerId = UICommunication.addPlayer (PlayerData.Create(SvgImage.CreateRectangle(RGBAColors.black, Size.zero))) state
                UICommunication.setBackground (SvgImage.CreateRectangle(RGBAColors.red, { Width = 1.; Height = 1. })) state
                UICommunication.say playerId "Hi" state
                let image = getScreenshot state
                let colors = getPixelsAt Coordinates.fullScene image |> Map.toList |> List.map snd
                Expect.exists colors ((<>) red) "Scene should have non-red pixels"
            }
        ]

        testList "Batching" [
            test "Changes during batch" {
                use state = UICommunication.showScene defaultSceneSize
                UICommunication.startBatch state
                let playerId = UICommunication.addPlayer rect state
                UICommunication.setPenState playerId true state
                UICommunication.setDirection playerId (Degrees.op_Implicit 90.) state
                UICommunication.setPosition playerId { X = 100.; Y = 100. } state
                UICommunication.say playerId "Hi!" state
                let image = getScreenshot state
                let colors = getPixelsAt Coordinates.fullScene image |> Map.toList |> List.map snd
                Expect.allEqual colors white "Scene should still be empty"
            }
            test "Apply batch" {
                use state = UICommunication.showScene defaultSceneSize
                UICommunication.startBatch state
                let playerId = UICommunication.addPlayer rect state
                UICommunication.setPenState playerId true state
                UICommunication.setDirection playerId (Degrees.op_Implicit 90.) state
                UICommunication.setPosition playerId { X = 100.; Y = 100. } state
                UICommunication.say playerId "Hi!" state
                UICommunication.applyBatch state
                let image = getScreenshot state
                let colors = getPixelsAt Coordinates.fullScene image |> Map.toList |> List.map snd
                Expect.exists colors ((<>) white) "Scene should have non-white pixels"
            }
            test "Relative changes during batch" {
                use state = UICommunication.showScene defaultSceneSize
                UICommunication.startBatch state
                let playerId = UICommunication.addPlayer rect state
                for _ in [1..10] do
                    let player = (MutableModel.getCurrent state.MutableModel).Players |> Map.toList |> List.map snd |> List.head
                    UICommunication.setPosition playerId (player.Position + { X = 13.; Y = 7. }) state
                UICommunication.applyBatch state
                let image = getScreenshot state
                let actualColors = getPixelsAt Coordinates.fullScene image
                let expectedColors =
                    createEmptyImage
                    |> setAllScenePixels white
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (130 - rectWidth / 2, -70 - rectHeight / 2)) (Coordinates.relativeToSceneCenter (130 + rectWidth / 2, -70 + rectHeight / 2))) rectColor
                    |> doCreateImage image
                let valueDiff = Map.valueDiff actualColors expectedColors
                Expect.isTrue valueDiff.IsEmpty "Scene should have rectangle at (130, 70) and everything else empty"
            }
            test "Apply nested batch" {
                use state = UICommunication.showScene defaultSceneSize
                UICommunication.startBatch state
                let playerId = UICommunication.addPlayer rect state
                UICommunication.startBatch state
                UICommunication.setPosition playerId { X = 100.; Y = 100. } state
                UICommunication.applyBatch state
                let image = getScreenshot state
                let colors = getPixelsAt Coordinates.fullScene image |> Map.toList |> List.map snd
                Expect.allEqual colors white "Scene should still be empty"
            }
            test "Apply root batch" {
                use state = UICommunication.showScene defaultSceneSize
                UICommunication.startBatch state
                let playerId = UICommunication.addPlayer rect state
                UICommunication.startBatch state
                UICommunication.setPosition playerId { X = 100.; Y = 100. } state
                UICommunication.applyBatch state
                UICommunication.setPosition playerId { X = 200.; Y = 100. } state
                UICommunication.applyBatch state
                let image = getScreenshot state
                let actualColors = getPixelsAt Coordinates.fullScene image
                let expectedColors =
                    createEmptyImage
                    |> setAllScenePixels white
                    |> setPixelsBetween (Coordinates.range (Coordinates.relativeToSceneCenter (200 - rectWidth / 2, -100 - rectHeight / 2)) (Coordinates.relativeToSceneCenter (200 + rectWidth / 2, -100 + rectHeight / 2))) rectColor
                    |> doCreateImage image
                let valueDiff = Map.valueDiff actualColors expectedColors
                Expect.isTrue valueDiff.IsEmpty "Scene should have rectangle at (200, 100) and everything else empty"
            }
        ]
    ]

[<EntryPoint>]
let main args =
    match Environment.GetEnvironmentVariable "GET_IT_SERVER_ADDRESS" |> Option.ofObj with
    | Some _ ->
        GetIt.UI.Container.Program.main [||]
    | None -> runTestsWithCLIArgs [] args tests
