namespace GetIt
{
    /// <summary>
    /// Defines a 2D position.
    /// </summary>
    [Equals]
    public sealed class Position
    {
        /// <summary>
        /// Origin of a 2D plane.
        /// </summary>
        /// <returns></returns>
        public static readonly Position Zero = new Position(0, 0);

        /// <summary>
        /// Initializes all properties of an instance.
        /// </summary>
        public Position(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// X coordinate.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Y coordinate.
        /// </summary>
        public double Y { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"({X:F2}, {Y:F2})";
        }
    }
}