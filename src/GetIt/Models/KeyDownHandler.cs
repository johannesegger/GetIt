using System;

namespace GetIt.Models
{
    [Equals]
    public sealed class KeyDownHandler
    {
        public KeyDownHandler(Guid id, KeyboardKey key, Action handler)
        {
            Id = id;
            Key = key;
            Handler = handler;
        }

        public Guid Id { get; }
        [IgnoreDuringEquals] public KeyboardKey Key { get; }
        [IgnoreDuringEquals] public Action Handler { get; }
    }
}