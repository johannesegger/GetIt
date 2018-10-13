namespace GetIt.Models
{
    [Equals]
    public sealed class Position
    {
        public Position(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }
        public double Y { get; }

        public override string ToString()
        {
            return $"({X:F2}, {Y:F2})";
        }
    }
}