using AForge.Imaging;
using Avalonia.Media;

namespace PlayAndLearn.Models
{
    public sealed class Pen
    {
        public Pen(double weight, Color color)
        {
            Weight = weight;
            Color = color;
        }

        public double Weight { get; }

        public Color Color { get; }

        public Pen WithHueShift(int shift)
        {
            var hsl = HSL.FromRGB(new RGB(Color.R, Color.G, Color.B, Color.A));
            hsl.Hue += shift;
            var rgb = hsl.ToRGB();
            return new Pen(Weight, new Color(rgb.Alpha, rgb.Red, rgb.Green, rgb.Blue));
        }
    }
}