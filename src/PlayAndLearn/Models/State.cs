using LanguageExt;

namespace PlayAndLearn.Models
{
    public class State
    {
        public State(
            Size sceneSize,
            string code,
            Option<UserScript> script,
            Option<ExecutionState> executionState,
            Player player,
            Option<Position> previousDragPosition)
        {
            SceneSize = sceneSize;
            Code = code;
            Script = script;
            ExecutionState = executionState;
            Player = player;
            PreviousDragPosition = previousDragPosition;
        }

        public Size SceneSize { get; }
        public string Code { get; }
        public Option<UserScript> Script { get; }
        public Option<ExecutionState> ExecutionState { get; }
        public Player Player { get; }
        public Option<Position> PreviousDragPosition { get; }
    }

    public static class StateExtensions
    {
        public static bool CanExecuteScript(this State state)
        {
            return state.Script.Match(s => s.CanExecute, () => false);
        }
    }
}