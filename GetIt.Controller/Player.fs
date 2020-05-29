namespace GetIt

open System
open System.Threading

/// A player that is added to the scene.
type Player internal (playerId, getPlayer, remove) =
    let mutable isDisposed = 0

    member internal x.PlayerId with get () : PlayerId = playerId
    member private x.Player with get () : PlayerData = getPlayer ()

    /// The actual size of the player.
    member x.Size with get () = x.Player.Size

    /// The factor that is used to change the size of the player.
    member x.SizeFactor with get () = x.Player.SizeFactor

    /// The position of the player's center point.
    member x.Position with get () = x.Player.Position

    /// The rectangular bounds of the player.
    /// Note that this doesn't take into account the current rotation of the player.
    member x.Bounds with get () = x.Player.Bounds

    /// The rotation of the player.
    member x.Direction with get () = x.Player.Direction

    /// The pen that belongs to the player.
    member x.Pen with get () = x.Player.Pen

    /// The current costume of the player.
    member x.Costume with get () = x.Player.Costume

    /// True, if the player should be drawn, otherwise false.
    member x.IsVisible with get () = x.Player.IsVisible

    /// Removes the player from the scene.
    abstract member Dispose: unit -> unit
    default x.Dispose () =
        if Interlocked.Exchange (&isDisposed, 1) = 0 then
            remove ()

    interface IDisposable with
        member x.Dispose () = x.Dispose ()
