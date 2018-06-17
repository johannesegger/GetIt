using Elmish.Net;
using PlayAndLearn.Utils;

namespace PlayAndLearn.Models
{
    public static class PenExtensions
    {
        public static Pen WithHueShift(this Pen pen, double shift)
        {
            return pen.With(p => p.Color, pen.Color.ToHSL().AddHue(shift).ToRGB());
        }
    }
}