using System;

namespace GetIt.Internal
{
    [Equals]
    internal class HSLA
    {
        public HSLA(double hue, double saturation, double lightness, double alpha)
        {
            Hue = hue;
            Saturation = saturation;
            Lightness = lightness;
            Alpha = alpha;
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

        [IgnoreDuringEquals]
        public double Alpha { get; }
        private double AlphaRounded => Math.Round(Alpha, 5);

        public override string ToString() => $"hsla({Hue * 360}°, {Saturation:P}, {Lightness:P}, {Alpha})";
    }
}