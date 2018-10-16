using System.Collections.Immutable;

namespace GetIt.Models
{
    [Equals]
    public sealed class State
    {
        public State(
            Rectangle sceneBounds,
            IImmutableList<Player> players,
            IImmutableList<PenLine> penLines,
            Position mousePosition,
            IImmutableList<EventHandler> eventHandlers)
        {
            SceneBounds = sceneBounds ?? throw new System.ArgumentNullException(nameof(sceneBounds));
            Players = players ?? throw new System.ArgumentNullException(nameof(players));
            PenLines = penLines ?? throw new System.ArgumentNullException(nameof(penLines));
            MousePosition = mousePosition ?? throw new System.ArgumentNullException(nameof(mousePosition));
            EventHandlers = eventHandlers ?? throw new System.ArgumentNullException(nameof(eventHandlers));
        }

        public Rectangle SceneBounds { get; }
        public IImmutableList<Player> Players { get; }
        public IImmutableList<PenLine> PenLines { get; }
        public Position MousePosition { get; }
        public IImmutableList<EventHandler> EventHandlers { get; }
    }
}