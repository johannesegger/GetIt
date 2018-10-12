using System;

namespace PlayAndLearn.Models
{
    [Equals]
    public sealed class Player
    {
        public Player(Guid id, Size size, Position position, Degrees direction, Pen pen, SpeechBubble speechBubble, Costume costume)
        {
            Id = id;
            Size = size ?? throw new ArgumentNullException(nameof(size));
            Position = position ?? throw new ArgumentNullException(nameof(position));
            Direction = direction ?? throw new ArgumentNullException(nameof(direction));
            Pen = pen ?? throw new ArgumentNullException(nameof(pen));
            SpeechBubble = speechBubble ?? throw new ArgumentNullException(nameof(speechBubble));
            Costume = costume ?? throw new ArgumentNullException(nameof(costume));
        }

        public Guid Id { get; }

        public Size Size { get; }

        public Position Position { get; }

        public Degrees Direction { get; }

        public Pen Pen { get; }

        public SpeechBubble SpeechBubble { get; }

        public Costume Costume { get; }
    }
}