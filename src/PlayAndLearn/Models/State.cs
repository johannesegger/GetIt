using System.Collections.Immutable;
using LanguageExt;

namespace PlayAndLearn.Models
{
    public class State
    {
        public State(
            Size sceneSize,
            string code,
            Option<UserScript> script,
            ExecutionState executionState,
            Player player,
            Option<Position> previousDragPosition,
            IImmutableList<VisualLine> lines)
        {
            SceneSize = sceneSize;
            Code = code;
            Script = script;
            ExecutionState = executionState;
            Player = player;
            PreviousDragPosition = previousDragPosition;
            Lines = lines;
        }

        public Size SceneSize { get; }
        public string Code { get; }
        public Option<UserScript> Script { get; }
        public ExecutionState ExecutionState { get; }
        public Player Player { get; }
        public Option<Position> PreviousDragPosition { get; }
        public IImmutableList<VisualLine> Lines { get; }
    }

    public static class StateExtensions
    {
        public static bool CanExecuteScript(this State state)
        {
            return state.Script.Match(s => s.CanExecute, () => false);
        }
    }
}