namespace GetIt
{
    [Equals]
    public sealed class Rectangle
    {
        public Rectangle(Position position, Size size)
        {
            Position = position;
            Size = size;
        }

        public Position Position { get; }
        public Size Size { get; }

        [IgnoreDuringEquals] public double Left => Position.X;
        [IgnoreDuringEquals] public double Right => Position.X + Size.Width;
        [IgnoreDuringEquals] public double Top => Position.Y + Size.Height;
        [IgnoreDuringEquals] public double Bottom => Position.Y;
    }
}