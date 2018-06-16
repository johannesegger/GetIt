using System.Collections.Generic;
using LanguageExt;

namespace PlayAndLearn.Models
{
    public class ExecutionState
    {
        public ExecutionState(
            IEnumerable<object> states,
            Option<IEnumerator<object>> state)
        {
            States = states;
            State = state;
        }

        public IEnumerable<object> States { get; }
        public Option<IEnumerator<object>> State { get; }
    }
}