using Elmish.Net;
using GetIt.Internal;

namespace GetIt
{
    public static class PenExtensions
    {
        public static Pen WithHueShift(this Pen pen, double shift)
        {
            var hslaColor = pen.Color.ToHSLA();
            return pen.With(p => p.Color, hslaColor.With(p => p.Hue, hslaColor.Hue + shift).ToRGBA());
        }
    }
}