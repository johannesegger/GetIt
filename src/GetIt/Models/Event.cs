using System;

namespace GetIt.Models
{
    public abstract class Event
    {
        public class KeyDown : Event
        {
            public KeyDown(KeyboardKey key)
            {
                Key = key;
            }

            public KeyboardKey Key { get; }
        }

        public class ClickPlayer : Event
        {
            public ClickPlayer(Guid playerId)
            {
                PlayerId = playerId;
            }

            public Guid PlayerId { get; }
        }

        public class MouseEnterPlayer : Event
        {
            public MouseEnterPlayer(Guid playerId)
            {
                PlayerId = playerId;
            }

            public Guid PlayerId { get; }
        }
    }
}