using System.Collections.Immutable;

namespace GetIt.Models
{
    [Equals]
    public class KeyboardState
    {
        public static readonly KeyboardState Empty = new KeyboardState(ImmutableHashSet<KeyboardKey>.Empty);

        public KeyboardState(IImmutableSet<KeyboardKey> keysPressed)
        {
            KeysPressed = keysPressed;
        }

        public IImmutableSet<KeyboardKey> KeysPressed { get; }
    }
}