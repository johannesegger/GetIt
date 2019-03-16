namespace GetIt

[<AutoOpen>]
module Utils =
    let curry fn a b = fn (a, b)