using System.Collections.Immutable;

namespace GetIt
{
    [Equals]
    internal sealed class State
    {
        public State(
            Rectangle sceneBounds,
            IImmutableList<Player> players,
            IImmutableList<PenLine> penLines,
            MouseState mouse,
            KeyboardState keyboard,
            IImmutableList<EventHandler> eventHandlers)
        {
            SceneBounds = sceneBounds ?? throw new System.ArgumentNullException(nameof(sceneBounds));
            Players = players ?? throw new System.ArgumentNullException(nameof(players));
            PenLines = penLines ?? throw new System.ArgumentNullException(nameof(penLines));
            Mouse = mouse ?? throw new System.ArgumentNullException(nameof(mouse));
            Keyboard = keyboard;
            EventHandlers = eventHandlers ?? throw new System.ArgumentNullException(nameof(eventHandlers));
        }

        public Rectangle SceneBounds { get; }
        public IImmutableList<Player> Players { get; }
        public IImmutableList<PenLine> PenLines { get; }
        public MouseState Mouse { get; }
        public KeyboardState Keyboard { get; }
        public IImmutableList<EventHandler> EventHandlers { get; }
    }
}