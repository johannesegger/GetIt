namespace PlayAndLearn.Models
{
    public static class PlayerExtensions
    {
        public static void MoveRight(this Player player, int steps)
        {
            player.MoveTo(new Position(player.Position.X + steps, player.Position.Y));
        }

        public static void MoveLeft(this Player player, int steps)
        {
            player.MoveRight(-steps);
        }

        public static void MoveUp(this Player player, int steps)
        {
            player.MoveTo(new Position(player.Position.X, player.Position.Y + steps));
        }

        public static void MoveDown(this Player player, int steps)
        {
            player.MoveUp(-steps);
        }
    }
}