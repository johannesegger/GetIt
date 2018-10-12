using System;

namespace PlayAndLearn.Utils
{
    [Equals]
    internal class HSL
    {
        public HSL(double hue, double saturation, double lightness)
        {
            Hue = hue;
            Saturation = saturation;
            Lightness = lightness;
        }

        [IgnoreDuringEquals]
        public double Hue { get; }
        private double HueRounded => Math.Round(Hue, 5);

        [IgnoreDuringEquals]
        public double Saturation { get; }
        private double SaturationRounded => Math.Round(Saturation, 5);
        
        [IgnoreDuringEquals]
        public double Lightness { get; }
        private double LightnessRounded => Math.Round(Lightness, 5);

        public override string ToString() => $"hsl({Hue * 360:F2}°, {Saturation:P2}, {Lightness:P2})";
    }
}