namespace GetIt.Models
{
    [Equals]
    public class MouseState
    {
        public static readonly MouseState Empty = new MouseState(Position.Zero);

        public MouseState(Position position)
        {
            Position = position;
        }

        public Position Position { get; }
    }
}