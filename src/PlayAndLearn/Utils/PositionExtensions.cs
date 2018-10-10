using System;
using Avalonia;
using PlayAndLearn.Models;

namespace PlayAndLearn.Utils
{
    internal static class PositionExtensions
    {
        public static Position ToPosition(this Point point)
        {
            return new Position(point.X, point.Y);
        }

        public static Degrees AngleTo(this Position from, Position to)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var atan2 = Math.Atan2(dy, dx) * 180 / Math.PI;
            return new Degrees(atan2 < 0 ? atan2 + 360 : atan2);
        }

        public static double DistanceTo(this Position from, Position to)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}