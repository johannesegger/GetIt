using Avalonia;
using Avalonia.Controls;

namespace PlayAndLearn.Models
{
    public static class PointExtensions
    {
        public static Position ToPosition(this Point point, IControl parent)
        {
            return new Position(point.X, parent.Bounds.Height - point.Y);
        }

        public static Position Add(this Position a, Position b)
        {
            return new Position(a.X + b.X, a.Y + b.Y);
        }

        public static Position Subtract(this Position a, Position b)
        {
            return new Position(a.X - b.X, a.Y - b.Y);
        }
    }
}