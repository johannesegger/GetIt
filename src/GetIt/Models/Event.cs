using System;

namespace GetIt
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

        public class KeyUp : Event
        {
            public KeyUp(KeyboardKey key)
            {
                Key = key;
            }

            public KeyboardKey Key { get; }
        }

        public class ClickScene : Event
        {
            public ClickScene(Position position)
            {
                Position = position;
            }

            public Position Position { get; }
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