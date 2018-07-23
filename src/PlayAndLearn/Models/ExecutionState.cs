using System.Collections.Generic;
using LanguageExt;
using OneOf;

namespace PlayAndLearn.Models
{
    public class ExecutionState : OneOfBase<
        ExecutionState.Stopped,
        ExecutionState.Started,
        ExecutionState.Paused>
    {
        public bool IsStopped => IsT0;
        public bool IsStarted => IsT1;
        public bool IsPaused => IsT2;

        public class Stopped : ExecutionState
        {
            public Stopped(IEnumerable<object> states)
            {
                States = states;
            }

            public IEnumerable<object> States { get; }
        }

        public class Started : ExecutionState
        {
            public Started(IEnumerable<object> states, IEnumerator<object> state)
            {
                States = states;
                State = state;
            }

            public IEnumerable<object> States { get; }
            public IEnumerator<object> State { get; }
        }

        public class Paused : ExecutionState
        {
            public Paused(IEnumerable<object> states, IEnumerator<object> state)
            {
                States = states;
                State = state;
            }

            public IEnumerable<object> States { get; }
            public IEnumerator<object> State { get; }
        }
    }
}