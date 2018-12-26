namespace GetIt
{
    [Equals]
    internal sealed class PenLine
    {
        public PenLine(Position start, Position end, double weight, RGBA color)
        {
            Start = start ?? throw new System.ArgumentNullException(nameof(start));
            End = end ?? throw new System.ArgumentNullException(nameof(end));
            Weight = weight;
            Color = color ?? throw new System.ArgumentNullException(nameof(color));
        }

        public Position Start { get; }
        public Position End { get; }
        public double Weight { get; }
        public RGBA Color { get; }
    }
}