namespace GetIt

open System
open System.Security.Cryptography

/// Provides some easy to use functions for randomness.
module Randomly =
    /// Randomly selects an item from a list
    [<CompiledName("SelectOneOf")>]
    let selectOneOf([<ParamArray>] items) =
        let index = RandomNumberGenerator.GetInt32(Array.length items)
        Array.item index items