using System;

namespace PlayAndLearn.Models
{
    [Equals]
    public sealed class ClickPlayerHandler
    {
        public ClickPlayerHandler(Guid id, Guid playerId, Action handler)
        {
            Id = id;
            PlayerId = playerId;
            Handler = handler;
        }

        public Guid Id { get; }
        [IgnoreDuringEquals] public Guid PlayerId { get; }
        [IgnoreDuringEquals] public Action Handler { get; }
    }
}