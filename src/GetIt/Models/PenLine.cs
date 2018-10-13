namespace GetIt.Models
{
    [Equals]
    public sealed class PenLine
    {
        public PenLine(Position start, Position end, double weight, RGB color)
        {
            Start = start ?? throw new System.ArgumentNullException(nameof(start));
            End = end ?? throw new System.ArgumentNullException(nameof(end));
            Weight = weight;
            Color = color ?? throw new System.ArgumentNullException(nameof(color));
        }

        public Position Start { get; }
        public Position End { get; }
        public double Weight { get; }
        public RGB Color { get; }
    }
}