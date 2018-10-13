using Elmish.Net;
using GetIt.Utils;

namespace GetIt.Models
{
    public static class PenExtensions
    {
        public static Pen WithHueShift(this Pen pen, double shift)
        {
            return pen.With(p => p.Color, pen.Color.ToHSL().AddHue(shift).ToRGB());
        }
    }
}