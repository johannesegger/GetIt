namespace GetIt

open System
open System.Threading

/// <summary>
/// A player that is added to the scene.
/// </summary>
type Player(playerId) =
    let mutable isDisposed = 0

    member internal x.PlayerId with get () = playerId
    member private x.Player with get () = Map.find playerId (Model.getCurrent().Players)
    /// <summary>
    /// The actual size of the player.
    /// </summary>
    member x.Size with get () = x.Player.Size

    /// <summary>
    /// The factor that is used to resize the player.
    /// </summary>
    member x.SizeFactor with get () = x.Player.SizeFactor

    /// <summary>
    /// The position of the player.
    /// </summary>
    member x.Position with get () = x.Player.Position

    /// <summary>
    /// The actual bounds of the player.
    /// </summary>
    member x.Bounds with get () = x.Player.Bounds

    /// <summary>
    /// The direction of the player.
    /// </summary>
    member x.Direction with get () = x.Player.Direction

    /// <summary>
    /// The pen of the player.
    /// </summary>
    member x.Pen with get () = x.Player.Pen

    /// <summary>
    /// Removes the player from the scene.
    /// </summary>
    abstract member Dispose: unit -> unit
    default x.Dispose () =
        if Interlocked.Exchange (&isDisposed, 1) = 0 then
            UICommunication.sendCommand (RemovePlayer playerId)

    interface IDisposable with
        member x.Dispose () = x.Dispose ()
