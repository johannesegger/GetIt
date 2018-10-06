using PlayAndLearn.Utils;

namespace PlayAndLearn.Models
{
    public static class PenExtensions
    {
        public static Pen WithHueShift(this Pen pen, double shift)
        {
            return pen.WithColor(pen.Color.ToHSL().AddHue(shift).ToRGB());
        }
    }
}