using Elmish.Net;
using GetIt.Internal;

namespace GetIt
{
    /// <summary>
    /// Defines extension methods for `Pen`.
    /// </summary>
    public static class PenExtensions
    {
        /// <summary>
        /// Shifts the hue value of the given pen. 
        /// </summary>
        /// <param name="pen">The pen which should have its color shifted.</param>
        /// <param name="degrees">The shift of the hue value.</param>
        public static Pen WithHueShift(this Pen pen, Degrees degrees)
        {
            var hslaColor = pen.Color.ToHSLA();
            var shiftedValue = hslaColor.Hue + (degrees.Value / 360);
            var shiftedColor = hslaColor.With(p => p.Hue, shiftedValue).ToRGBA();
            return pen.With(p => p.Color, shiftedColor);
        }
    }
}