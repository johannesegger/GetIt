namespace PlayAndLearn.Models
{
    public class State
    {
        public State(Size sceneSize, Code code, Player player)
        {
            SceneSize = sceneSize;
            Code = code;
            Player = player;
        }

        public Size SceneSize { get; }
        public Code Code { get; }
        public Player Player { get; }
    }
}