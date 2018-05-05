using System;

namespace PlayAndLearn.Models
{
    public static class PlayerExtensions
    {
        public static void Move(this Player player, double x, double y)
        {
            player.Position = new Position(player.Position.X + x, player.Position.Y + y);
        }

        public static void MoveRight(this Player player, int steps) => player.Move(steps, 0);

        public static void MoveLeft(this Player player, int steps) => player.Move(-steps, 0);

        public static void MoveUp(this Player player, int steps) => player.Move(0, steps);

        public static void MoveDown(this Player player, int steps) => player.Move(0, -steps);

        public static void Go(this Player player, int steps)
        {
            var directionRadians = player.Direction / 180 * Math.PI;
            player.Move(
                Math.Cos(directionRadians) * steps,
                Math.Sin(directionRadians) * steps
            );
        }

        public static void Rotate(this Player player, double angleInDegrees)
        {
            player.Direction += angleInDegrees;
        }

        public static void TurnUp(this Player player) => player.Direction = 90;

        public static void TurnRight(this Player player) => player.Direction = 0;

        public static void TurnDown(this Player player) => player.Direction = 270;

        public static void TurnLeft(this Player player) => player.Direction = 180;
    }
}