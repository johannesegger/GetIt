namespace GetIt
{
    /// <summary>
    /// Defines a 2D rectangle.
    /// </summary>
    [Equals]
    public sealed class Rectangle
    {
        /// <summary>
        /// Initializes all properties of an instance.
        /// </summary>
        public Rectangle(Position position, Size size)
        {
            Position = position;
            Size = size;
        }

        /// <summary>
        /// The position of the left bottom corner.
        /// </summary>
        public Position Position { get; }

        /// <summary>
        /// The size of the rectangle.
        /// </summary>
        public Size Size { get; }

        /// <summary>
        /// The x-offset of the left edge.
        /// </summary>
        [IgnoreDuringEquals] public double Left => Position.X;

        /// <summary>
        /// The x-offset of the right edge.
        /// </summary>
        [IgnoreDuringEquals] public double Right => Position.X + Size.Width;

        /// <summary>
        /// The y-offset of the top edge.
        /// </summary>
        [IgnoreDuringEquals] public double Top => Position.Y + Size.Height;

        /// <summary>
        /// The y-offset of the bottom edge.
        /// </summary>
        [IgnoreDuringEquals] public double Bottom => Position.Y;
    }
}