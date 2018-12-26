namespace GetIt
{
    [Equals]
    internal class MouseState
    {
        public static readonly MouseState Empty = new MouseState(Position.Zero);

        public MouseState(Position position)
        {
            Position = position;
        }

        public Position Position { get; }
    }
}