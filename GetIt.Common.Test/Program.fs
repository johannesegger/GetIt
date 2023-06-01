open Expecto
open FsCheck
open GetIt

type Bounds = {
    LeftBottomX: int
    LeftBottomY: int
    Width: PositiveInt
    Height: PositiveInt
}
module Bounds =
    let toRectangle v =
        {
            Position = { X = v.LeftBottomX; Y = v.LeftBottomY }
            Size = { Width = v.Width.Get; Height = v.Height.Get }
        }

let tests = testList "All" [
    testList "Movement.bounceOffWall" [
        testProperty "object is inside" <| fun (bounds, (PositiveInt left, PositiveInt right, PositiveInt top, PositiveInt bottom), NormalFloat direction) ->
            let object = Bounds.toRectangle bounds
            let wall = {
                Position = object.Position - { X = left; Y = bottom }
                Size = { Width = float left + object.Size.Width + float right; Height = float bottom + object.Size.Height + float top }
            }
            let actual = Movement.bounceOffWall wall (object, Degrees.op_Implicit direction)
            Expect.isNone actual "Should not bounce off wall if not touching wall"

        testProperty "object is left of wall and direction is left" <| fun (bounds, (NegativeInt left, PositiveInt width, PositiveInt top, PositiveInt bottom)) ->
            let object = Bounds.toRectangle bounds
            let wall = {
                Position = object.Position - { X = left; Y = bottom }
                Size = { Width = width; Height = float bottom + object.Size.Height + float top }
            }
            [ (135., 45.); (180., 0); (225., 315.) ]
            |> List.map (fun (a, b) -> (Degrees.op_Implicit a, Degrees.op_Implicit b))
            |> List.iter (fun (direction, expectedDirection) ->
                let actual = Movement.bounceOffWall wall (object, direction)
                Expect.equal actual (Some expectedDirection) "Should bounce off wall if touching left wall"
            )

        testProperty "object is right of wall and direction is right" <| fun (bounds, (PositiveInt right, PositiveInt width, PositiveInt top, PositiveInt bottom)) ->
            let object = Bounds.toRectangle bounds
            let wall = {
                Position = object.Position - { X = float right + float width; Y = bottom }
                Size = { Width = width; Height = float bottom + object.Size.Height + float top }
            }
            [ (45., 135.); (0, 180.); (315., 225.) ]
            |> List.map (fun (a, b) -> (Degrees.op_Implicit a, Degrees.op_Implicit b))
            |> List.iter (fun (direction, expectedDirection) ->
                let actual = Movement.bounceOffWall wall (object, direction)
                Expect.equal actual (Some expectedDirection) "Should bounce off wall if touching left wall"
            )

        testProperty "object is left and top of wall and direction is left and top" <| fun (bounds, (NegativeInt left, PositiveInt width, NegativeInt top, PositiveInt height)) ->
            let object = Bounds.toRectangle bounds
            let wall = {
                Position = object.Position - { X = left; Y = float (-top + height) }
                Size = { Width = width; Height = height }
            }
            let actual = Movement.bounceOffWall wall (object, Degrees.op_Implicit 135.)
            Expect.equal actual (Some (Degrees.op_Implicit 315.)) "Should bounce off wall if touching left and top wall"
    ]

    testList "Rectangle.containsRectangle" [
        testProperty "fully enclosed" <| fun (bounds: Bounds) ->
            let inner = Bounds.toRectangle bounds
            let outer = {
                Position = { X = inner.Position.X - 1.; Y = inner.Position.Y - 1. }
                Size = { Width = inner.Size.Width + 2.; Height = inner.Size.Height + 2. }
            }
            let actual = outer |> Rectangle.containsRectangle inner
            Expect.isTrue actual "Outer rectangle should contain inner rectangle"
            let actual = inner |> Rectangle.containsRectangle outer
            Expect.isFalse actual "Inner rectangle should not contain outer rectangle"

        testProperty "partially enclosed" <| fun (bounds: Bounds) ->
            let box = Bounds.toRectangle bounds
            let offsetArb =
                let xOffsetGen = Gen.choose (-int box.Size.Height, int box.Size.Height)
                let yOffsetGen = Gen.choose (-int box.Size.Width, int box.Size.Width)
                Gen.zip xOffsetGen yOffsetGen
                |> Gen.filter ((<>) (0, 0))
                |> Arb.fromGen
            Prop.forAll offsetArb <| fun (xOffset, yOffset) ->
                let bounds = {
                    Position = { X = box.Position.X + float xOffset; Y = box.Position.Y + float yOffset }
                    Size = box.Size
                }
                let actual = bounds |> Rectangle.containsRectangle box
                Expect.isFalse actual "Bounds should not fully contain box"
            |> fun p -> Check.One({ Config.QuickThrowOnFailure with QuietOnSuccess = true }, p)

        testProperty "not touching" <| fun bounds ->
            let box1 = Bounds.toRectangle bounds
            let offsets = [
                (0., box1.Size.Height + 1.)
                (box1.Size.Width + 1., box1.Size.Height + 1.)
                (box1.Size.Width + 1., 0.)
                (box1.Size.Width + 1., -(box1.Size.Height + 1.))
                (0., -(box1.Size.Height + 1.))
                (-(box1.Size.Width + 1.), -(box1.Size.Height + 1.))
                (-(box1.Size.Width + 1.), 0.)
                (-(box1.Size.Width + 1.), box1.Size.Height + 1.)
            ]
            offsets
            |> List.iter (fun (dx, dy) ->
                let box2 = {
                    Position = box1.Position + { X = dx; Y = dy }
                    Size = box1.Size
                }
                let actual = box1 |> Rectangle.containsRectangle box2
                Expect.isFalse actual "box1 should not contain box2"
            )
    ]
]

[<EntryPoint>]
let main args =
    runTestsWithCLIArgs [] args tests
