using System;

namespace PlayAndLearn.Models
{
    [Equals]
    public sealed class Player
    {
        public Player(
            Guid id,
            Size originalSize,
            double sizeFactor,
            Position position,
            Degrees direction,
            Pen pen,
            SpeechBubble speechBubble,
            Costume costume)
        {
            Id = id;
            OriginalSize = originalSize ?? throw new ArgumentNullException(nameof(originalSize));
            SizeFactor = sizeFactor;
            Position = position ?? throw new ArgumentNullException(nameof(position));
            Direction = direction ?? throw new ArgumentNullException(nameof(direction));
            Pen = pen ?? throw new ArgumentNullException(nameof(pen));
            SpeechBubble = speechBubble ?? throw new ArgumentNullException(nameof(speechBubble));
            Costume = costume ?? throw new ArgumentNullException(nameof(costume));
        }

        public Guid Id { get; }
        public Size OriginalSize { get; }
        public double SizeFactor { get; }
        [IgnoreDuringEquals] public Size Size => OriginalSize * SizeFactor;
        public Position Position { get; }
        public Degrees Direction { get; }
        public Pen Pen { get; }
        public SpeechBubble SpeechBubble { get; }
        public Costume Costume { get; }
    }
}