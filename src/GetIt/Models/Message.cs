using System;
using OneOf;

namespace GetIt.Models
{
    public abstract class Message
        : OneOfBase<
            Message.SetSceneSize,
            Message.SetMousePosition,
            Message.SetPosition,
            Message.SetDirection,
            Message.Say,
            Message.SetPen,
            Message.SetSizeFactor,
            Message.AddPlayer,
            Message.RemovePlayer,
            Message.ClearScene,
            Message.AddKeyDownHandler,
            Message.RemoveKeyDownHandler,
            Message.TriggerKeyDownEvent,
            Message.AddClickPlayerHandler,
            Message.RemoveClickPlayerHandler,
            Message.TriggerClickPlayerEvent,
            Message.AddMouseEnterPlayerHandler,
            Message.RemoveMouseEnterPlayerHandler,
            Message.TriggerMouseEnterPlayerEvent>
    {
        public class SetSceneSize : Message
        {
            public SetSceneSize(Size size)
            {
                Size = size;
            }

            public Size Size { get; }
        }

        public class SetMousePosition : Message
        {
            public SetMousePosition(Position position)
            {
                Position = position;
            }

            public Position Position { get; }
        }

        public class SetPosition : Message
        {
            public SetPosition(Guid playerId, Position position)
            {
                PlayerId = playerId;
                Position = position;
            }

            public Guid PlayerId { get; }
            public Position Position { get; }
        }

        public class SetDirection : Message
        {
            public SetDirection(Guid playerId, Degrees angle)
            {
                PlayerId = playerId;
                Angle = angle;
            }

            public Guid PlayerId { get; }
            public Degrees Angle { get; }
        }

        public class Say : Message
        {
            public Say(Guid playerId, SpeechBubble speechBubble)
            {
                PlayerId = playerId;
                SpeechBubble = speechBubble;
            }

            public Guid PlayerId { get; }
            public SpeechBubble SpeechBubble { get; }
        }

        public class SetPen : Message
        {
            public SetPen(Guid playerId, Pen pen)
            {
                PlayerId = playerId;
                Pen = pen;
            }

            public Guid PlayerId { get; }
            public Pen Pen { get; }
        }

        public class SetSizeFactor : Message
        {
            public SetSizeFactor(Guid playerId, double sizeFactor)
            {
                PlayerId = playerId;
                SizeFactor = sizeFactor;
            }

            public Guid PlayerId { get; }
            public double SizeFactor { get; }
        }

        public class AddPlayer : Message
        {
            public AddPlayer(Player player)
            {
                Player = player;
            }

            public Player Player { get; }
        }

        public class RemovePlayer : Message
        {
            public RemovePlayer(Guid playerId)
            {
                PlayerId = playerId;
            }

            public Guid PlayerId { get; }
        }

        public class ClearScene : Message
        {
        }

        public class AddKeyDownHandler : Message
        {
            public AddKeyDownHandler(KeyDownHandler handler)
            {
                Handler = handler;
            }

            public KeyDownHandler Handler { get; }
        }

        public class RemoveKeyDownHandler : Message
        {
            public RemoveKeyDownHandler(KeyDownHandler handler)
            {
                Handler = handler;
            }

            public KeyDownHandler Handler { get; }
        }

        public class TriggerKeyDownEvent : Message
        {
            public TriggerKeyDownEvent(KeyboardKey key)
            {
                Key = key;
            }

            public KeyboardKey Key { get; }
        }

        public class AddClickPlayerHandler : Message
        {
            public AddClickPlayerHandler(ClickPlayerHandler handler)
            {
                Handler = handler;
            }

            public ClickPlayerHandler Handler { get; }
        }

        public class RemoveClickPlayerHandler : Message
        {
            public RemoveClickPlayerHandler(ClickPlayerHandler handler)
            {
                Handler = handler;
            }

            public ClickPlayerHandler Handler { get; }
        }

        public class TriggerClickPlayerEvent : Message
        {
            public TriggerClickPlayerEvent(Guid playerId)
            {
                PlayerId = playerId;
            }

            public Guid PlayerId { get; }
        }

        public class AddMouseEnterPlayerHandler : Message
        {
            public AddMouseEnterPlayerHandler(MouseEnterPlayerHandler handler)
            {
                Handler = handler;
            }

            public MouseEnterPlayerHandler Handler { get; }
        }

        public class RemoveMouseEnterPlayerHandler : Message
        {
            public RemoveMouseEnterPlayerHandler(MouseEnterPlayerHandler handler)
            {
                Handler = handler;
            }

            public MouseEnterPlayerHandler Handler { get; }
        }

        public class TriggerMouseEnterPlayerEvent : Message
        {
            public TriggerMouseEnterPlayerEvent(Guid playerId)
            {
                PlayerId = playerId;
            }

            public Guid PlayerId { get; }
        }
    }
}