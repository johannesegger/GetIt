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
            IImmutableList<KeyDownHandler> keyDownHandlers,
            IImmutableList<ClickPlayerHandler> clickPlayerHandlers,
            IImmutableList<MouseEnterPlayerHandler> mouseEnterPlayerHandlers)
        {
            SceneBounds = sceneBounds ?? throw new System.ArgumentNullException(nameof(sceneBounds));
            Players = players ?? throw new System.ArgumentNullException(nameof(players));
            PenLines = penLines ?? throw new System.ArgumentNullException(nameof(penLines));
            MousePosition = mousePosition ?? throw new System.ArgumentNullException(nameof(mousePosition));
            KeyDownHandlers = keyDownHandlers ?? throw new System.ArgumentNullException(nameof(keyDownHandlers));
            ClickPlayerHandlers = clickPlayerHandlers ?? throw new System.ArgumentNullException(nameof(clickPlayerHandlers));
            MouseEnterPlayerHandlers = mouseEnterPlayerHandlers ?? throw new System.ArgumentNullException(nameof(clickPlayerHandlers));
        }

        public Rectangle SceneBounds { get; }
        public IImmutableList<Player> Players { get; }
        public IImmutableList<PenLine> PenLines { get; }
        public Position MousePosition { get; }
        public IImmutableList<KeyDownHandler> KeyDownHandlers { get; }
        public IImmutableList<ClickPlayerHandler> ClickPlayerHandlers { get; }
        public IImmutableList<MouseEnterPlayerHandler> MouseEnterPlayerHandlers { get; }
    }
}