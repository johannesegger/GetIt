namespace PlayAndLearn.Utils
{
    internal static class HSLExtensions
    {
        public static HSL AddHue(this HSL hsl, double value)
        {
            return new HSL(hsl.Hue + value, hsl.Saturation, hsl.Lightness);
        }
    }
}