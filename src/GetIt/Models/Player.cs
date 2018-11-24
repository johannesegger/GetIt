using System;
using System.Collections.Immutable;
using LanguageExt;
using static LanguageExt.Prelude;

namespace GetIt
{
    [Equals]
    public sealed class Player
    {
        public Player(
            Guid id,
            double sizeFactor,
            Position position,
            Degrees direction,
            Pen pen,
            Option<SpeechBubble> speechBubble,
            IImmutableList<Costume> costumes,
            int costumeIndex)
        {
            Id = id;
            SizeFactor = sizeFactor;
            Position = position ?? throw new ArgumentNullException(nameof(position));
            Direction = direction ?? throw new ArgumentNullException(nameof(direction));
            Pen = pen ?? throw new ArgumentNullException(nameof(pen));
            SpeechBubble = speechBubble;
            Costumes = costumes ?? throw new ArgumentNullException(nameof(costumes));
            if (Costumes.Count < 1)
            {
                throw new ArgumentException("Player must have at least one costume", nameof(costumes));
            }
            CostumeIndex = costumeIndex;
        }

        public Guid Id { get; }
        public double SizeFactor { get; }
        [IgnoreDuringEquals] public Size Size => Costume.Size * SizeFactor;
        public Position Position { get; }
        [IgnoreDuringEquals] public Rectangle Bounds =>
            new Rectangle(
                new Position(Position.X - Size.Width / 2, Position.Y - Size.Height / 2),
                Size);
        public Degrees Direction { get; }
        public Pen Pen { get; }
        public Option<SpeechBubble> SpeechBubble { get; }
        public IImmutableList<Costume> Costumes { get; }
        public int CostumeIndex { get; }
        [IgnoreDuringEquals] public Costume Costume => Costumes[CostumeIndex];

        public static Player Create(IImmutableList<Costume> costumes)
        {
            return new Player(
                Guid.NewGuid(),
                1,
                Position.Zero,
                Degrees.Zero,
                Pen.Default,
                None,
                costumes,
                0);
        }

        public static Player Create(Costume costume)
        {
            return Create(ImmutableList.Create(costume));
        }
    }
}