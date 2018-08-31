module GameLib.Data

type RGBColor =
    { Red: byte
      Green: byte
      Blue: byte }

type Position =
    { X: float
      Y: float }
    static member (+) (p1: Position, p2: Position) =
        { X = p1.X + p2.X; Y = p1.Y + p2.Y }
    static member (-) (p1: Position, p2: Position) =
        { X = p1.X - p2.X; Y = p1.Y - p2.Y }

type Size =
    { Width: float
      Height: float }

module Global =
    type Pen =
        { Color: RGBColor
          Weight: float
          IsOn: bool }

    type Player =
        { Position: Position
          Direction: float
          Pen: Pen
          CostumeUrl: string }