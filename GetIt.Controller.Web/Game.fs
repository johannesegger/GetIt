namespace GetIt

/// Defines methods to setup a game, add players, register global events and more.
[<AbstractClass; Sealed>]
type Game() =
    /// Initializes and shows an empty scene with the default size and no players on it.
    static member ShowScene () =
        UICommunication.showScene (SpecificSize { Width = 800.; Height = 600. })

    /// Initializes and shows an empty scene with a specific size and no players on it.
    static member ShowScene (windowWidth, windowHeight) =
        UICommunication.showScene (SpecificSize { Width = windowWidth; Height = windowHeight })

    /// Initializes and shows an empty scene with maximized size and no players on it.
    static member ShowMaximizedScene () =
        UICommunication.showScene Maximized
