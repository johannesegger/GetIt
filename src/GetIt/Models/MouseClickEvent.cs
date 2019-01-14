namespace GetIt
{
    /// <summary>
    /// Event data for a mouse click.
    /// </summary>
    public class MouseClickEvent
    {
        internal MouseClickEvent(Position position, MouseButton mouseButton)
        {
            Position = position;
            MouseButton = mouseButton;
        }

        /// <summary>
        /// The position of the mouse click.
        /// </summary>
        /// <value></value>
        public Position Position { get; }

        /// <summary>
        /// The button of the mouse that clicked.
        /// </summary>
        /// <value></value>
        public MouseButton MouseButton { get; }
    }
}
