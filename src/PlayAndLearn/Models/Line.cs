namespace PlayAndLearn.Models
{
    public class VisualLine
    {
        public VisualLine(Position p1, Position p2, RGB color, double weight)
        {
            P1 = p1;
            P2 = p2;
            Color = color;
            Weight = weight;
        }

        public Position P1 { get; }
        public Position P2 { get; }
        public RGB Color { get; }
        public double Weight { get; }
    }
}