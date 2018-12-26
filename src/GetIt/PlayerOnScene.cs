using System;
using System.Linq;
using System.Threading;
using GetIt.Internal;

namespace GetIt
{
    /// <summary>
    /// A player that is added to the scene.
    /// </summary>
    public class PlayerOnScene : IDisposable
    {
        private int isDisposed = 0;

        internal Guid Id { get; }
        internal Player Player => Game.State.Players.Single(p => p.Id == Id);

        /// <summary>
        /// The actual size of the player.
        /// </summary>
        public Size Size => Player.Size;

        /// <summary>
        /// The factor that is used to resize the player.
        /// </summary>
        public double SizeFactor => Player.SizeFactor;

        /// <summary>
        /// The position of the player.
        /// </summary>
        public Position Position => Player.Position;

        /// <summary>
        /// The actual bounds of the player.
        /// </summary>
        public Rectangle Bounds => Player.Bounds;

        /// <summary>
        /// The direction of the player.
        /// </summary>
        public Degrees Direction => Player.Direction;

        /// <summary>
        /// The pen of the player.
        /// </summary>
        public Pen Pen => Player.Pen;

        internal PlayerOnScene(Guid id)
        {
            Id = id;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.Exchange(ref isDisposed, 1) != 0)
            {
                return;
            }
            Game.DispatchMessageAndWaitForUpdate(new Message.RemovePlayer(Id));
        }
    }
}