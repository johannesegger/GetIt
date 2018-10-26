namespace GetIt
{
    [Equals]
    public sealed class Position
    {
        public static readonly Position Zero = new Position(0, 0);

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