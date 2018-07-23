using System;
using Elmish.Net;
using PlayAndLearn.Utils;

namespace PlayAndLearn.Models
{
    public static class PlayerExtensions
    {
        public static Player GoTo(this Player player, double x, double y) =>
            player
                .With(p => p.Position.X, x)
                .With(p => p.Position.Y, y);

        public static Player Move(this Player player, double x, double y) =>
            player
                .With(p => p.Position.X, player.Position.X + x)
                .With(p => p.Position.Y, player.Position.Y + y);

        public static Player MoveRight(this Player player, int steps) =>
            player.Move(steps, 0);

        public static Player MoveLeft(this Player player, int steps) =>
            player.Move(-steps, 0);

        public static Player MoveUp(this Player player, int steps) =>
            player.Move(0, steps);

        public static Player MoveDown(this Player player, int steps) =>
            player.Move(0, -steps);

        public static Player Go(this Player player, int steps)
        {
            var directionRadians = player.Direction / 180 * Math.PI;
            return player.Move(
                Math.Cos(directionRadians) * steps,
                Math.Sin(directionRadians) * steps);
        }

        public static Player Rotate(this Player player, double angleInDegrees)
        {
            var direction = ((player.Direction + angleInDegrees) % 360 + 360) % 360;
            return player.With(p => p.Direction, direction);
        }

        public static Player TurnUp(this Player player) =>
            player.With(p => p.Direction, 90);

        public static Player TurnRight(this Player player) =>
            player.With(p => p.Direction, 0);

        public static Player TurnDown(this Player player) =>
            player.With(p => p.Direction, 270);

        public static Player TurnLeft(this Player player) =>
            player.With(p => p.Direction, 180);

        public static Player TurnOnPen(this Player player) =>
            player.With(p => p.Pen.IsOn, true);

        public static Player TurnOffPen(this Player player) =>
            player.With(p => p.Pen.IsOn, false);

        public static Player TogglePen(this Player player) =>
            player.With(p => p.Pen.IsOn, !player.Pen.IsOn);

        public static Player SetPenColor(this Player player, RGB color) =>
            player.With(p => p.Pen.Color, color);

        public static Player SetPenWeight(this Player player, double weight) =>
            player.With(p => p.Pen.Weight, weight);

        public static Player ShiftPenColor(this Player player, double shift) =>
            player.With(p => p.Pen.Color, player.Pen.Color.ToHSL().AddHue(shift).ToRGB());
    }
}