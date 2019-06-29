namespace GetIt

open System

/// Provides some easy to use functions for randomness.
module Randomly =
    let private generator = Random()

    /// Randomly selects an item from a list
    [<CompiledName("SelectOneOf")>]
    let selectOneOf([<ParamArray>] items) =
        let index = generator.Next(Array.length items)
        Array.item index items