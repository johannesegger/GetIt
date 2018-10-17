using System;
using LanguageExt;
using OneOf;

namespace GetIt.Models
{
    public abstract class Message
        : OneOfBase<
            Message.SetSceneSize,
            Message.SetMousePosition,
            Message.SetKeyboardKeyPressed,
            Message.SetKeyboardKeyReleased,
            Message.SetPosition,
            Message.SetDirection,
            Message.SetSpeechBubble,
            Message.UpdateAnswer,
            Message.ApplyAnswer,
            Message.SetPen,
            Message.SetSizeFactor,
            Message.AddPlayer,
            Message.RemovePlayer,
            Message.ClearScene,
            Message.AddEventHandler,
            Message.RemoveEventHandler,
            Message.TriggerEvent,
            Message.ExecuteAction>
    {
        public class SetSceneSize : Message
        {
            public SetSceneSize(Size size)
            {
                Size = size;
            }

            public Size Size { get; }
        }

        public class SetKeyboardKeyPressed : Message
        {
            public SetKeyboardKeyPressed(KeyboardKey key)
            {
                Key = key;
            }

            public KeyboardKey Key { get; }
        }

        public class SetKeyboardKeyReleased : Message
        {
            public SetKeyboardKeyReleased(KeyboardKey key)
            {
                Key = key;
            }

            public KeyboardKey Key { get; }
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

        public class SetSpeechBubble : Message
        {
            public SetSpeechBubble(Guid playerId, Option<SpeechBubble> speechBubble)
            {
                PlayerId = playerId;
                SpeechBubble = speechBubble;
            }

            public Guid PlayerId { get; }
            public Option<SpeechBubble> SpeechBubble { get; }
        }

        public class UpdateAnswer : Message
        {
            public UpdateAnswer(Guid playerId, string answer)
            {
                PlayerId = playerId;
                Answer = answer;
            }

            public Guid PlayerId { get; }
            public string Answer { get; }
        }

        public class ApplyAnswer : Message
        {
            public ApplyAnswer(Guid playerId, string answer)
            {
                PlayerId = playerId;
                Answer = answer;
            }

            public Guid PlayerId { get; }
            public string Answer { get; }
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

        public class AddEventHandler : Message
        {
            public AddEventHandler(EventHandler handler)
            {
                Handler = handler;
            }

            public EventHandler Handler { get; }
        }

        public class RemoveEventHandler : Message
        {
            public RemoveEventHandler(EventHandler handler)
            {
                Handler = handler;
            }

            public EventHandler Handler { get; }
        }

        public class TriggerEvent : Message
        {
            public TriggerEvent(Event @event)
            {
                Event = @event;
            }

            public Event Event { get; }
        }

        public class ExecuteAction : Message
        {
            public ExecuteAction(Action action)
            {
                Action = action;
            }

            public Action Action { get; }
        }
    }
}