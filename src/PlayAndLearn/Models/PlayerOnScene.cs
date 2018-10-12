using System;
using System.Linq;
using System.Threading;

namespace PlayAndLearn.Models
{
    public class PlayerOnScene : IDisposable
    {
        private int isDisposed = 0;

        internal Guid Id { get; }
        internal Player Player => Game.State.Players.Single(p => p.Id == Id);
        public Size Size => Player.Size;
        public double SizeFactor => Player.SizeFactor;
        public Position Position => Player.Position;
        public Degrees Direction => Player.Direction;
        public Pen Pen => Player.Pen;

        public PlayerOnScene(Guid id)
        {
            Id = id;
        }

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