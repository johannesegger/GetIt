namespace GetIt

open System
open System.Threading

/// A player that is added to the scene.
type Player(playerId) =
    let mutable isDisposed = 0

    member internal x.PlayerId with get () = playerId
    member private x.Player with get () = Map.find playerId (Model.getCurrent().Players)

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

    /// True, if the player should be drawn, otherwise false.
    member x.IsVisible with get () = x.Player.IsVisible

    /// Removes the player from the scene.
    abstract member Dispose: unit -> unit
    default x.Dispose () =
        if Interlocked.Exchange (&isDisposed, 1) = 0 then
            Connection.run UICommunication.removePlayer playerId

    interface IDisposable with
        member x.Dispose () = x.Dispose ()
