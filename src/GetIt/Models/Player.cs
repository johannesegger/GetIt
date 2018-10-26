using System;
using LanguageExt;
using static LanguageExt.Prelude;

namespace GetIt
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
            Option<SpeechBubble> speechBubble,
            Costume costume)
        {
            Id = id;
            OriginalSize = originalSize ?? throw new ArgumentNullException(nameof(originalSize));
            SizeFactor = sizeFactor;
            Position = position ?? throw new ArgumentNullException(nameof(position));
            Direction = direction ?? throw new ArgumentNullException(nameof(direction));
            Pen = pen ?? throw new ArgumentNullException(nameof(pen));
            SpeechBubble = speechBubble;
            Costume = costume ?? throw new ArgumentNullException(nameof(costume));
        }

        public Guid Id { get; }
        public Size OriginalSize { get; }
        public double SizeFactor { get; }
        [IgnoreDuringEquals] public Size Size => OriginalSize * SizeFactor;
        public Position Position { get; }
        [IgnoreDuringEquals] public Rectangle Bounds =>
            new Rectangle(
                new Position(Position.X - Size.Width / 2, Position.Y - Size.Height / 2),
                Size);
        public Degrees Direction { get; }
        public Pen Pen { get; }
        public Option<SpeechBubble> SpeechBubble { get; }
        public Costume Costume { get; }

        public static Player Create(Size originalSize, Costume costume)
        {
            return new Player(
                Guid.NewGuid(),
                originalSize,
                1,
                Position.Zero,
                Degrees.Zero,
                Pen.Default,
                None,
                costume);
        }
    }
}