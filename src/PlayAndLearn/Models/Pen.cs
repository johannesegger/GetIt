using PlayAndLearn.Utils;

namespace PlayAndLearn.Models
{
    public sealed class Pen
    {
        public Pen(double weight, RGB color)
        {
            Weight = weight;
            Color = color;
        }

        public double Weight { get; }

        public RGB Color { get; }

        public Pen WithHueShift(double shift)
        {
            return new Pen(Weight, Color.ToHSL().AddHue(shift).ToRGB());
        }
    }
}