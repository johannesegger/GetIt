using System;
using Elmish.Net;

namespace PlayAndLearn.Models
{
    public static class PlayerExtensions
    {
        public static Player Move(this Player player, double x, double y)
        {
            return player.With(p => p.Position, new Position(player.Position.X + x, player.Position.Y + y));
        }

        public static Player MoveRight(this Player player, int steps) => player.Move(steps, 0);

        public static Player MoveLeft(this Player player, int steps) => player.Move(-steps, 0);

        public static Player MoveUp(this Player player, int steps) => player.Move(0, steps);

        public static Player MoveDown(this Player player, int steps) => player.Move(0, -steps);

        public static Player Go(this Player player, int steps)
        {
            var directionRadians = player.Direction / 180 * Math.PI;
            return player.Move(
                Math.Cos(directionRadians) * steps,
                Math.Sin(directionRadians) * steps
            );
        }

        public static Player Rotate(this Player player, double angleInDegrees)
        {
            return player.With(p => p.Direction, player.Direction + angleInDegrees);
        }

        public static Player TurnUp(this Player player) => player.With(p => p.Direction, 90);

        public static Player TurnRight(this Player player) => player.With(p => p.Direction, 0);

        public static Player TurnDown(this Player player) => player.With(p => p.Direction, 270);

        public static Player TurnLeft(this Player player) => player.With(p => p.Direction, 180);
    }
}