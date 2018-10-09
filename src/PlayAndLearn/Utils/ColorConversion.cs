using System;
using PlayAndLearn.Models;

namespace PlayAndLearn.Utils
{
    // No idea what this does, got this from http://www.easyrgb.com/en/math.php
    internal static class ColorConversion
    {
        public static HSL ToHSL(this RGB rgb)
        {
            var r = rgb.Red / 255.0;
            var g = rgb.Green / 255.0;
            var b = rgb.Blue / 255.0;

            var min = Math.Min(r, Math.Min(g, b));
            var max = Math.Max(r, Math.Max(g, b));
            var delta = max - min;

            var lightness = (max + min) / 2;

            double hue, saturation;
            if (delta == 0) // Gray, no chroma
            {
                hue = 0;
                saturation = 0;
            }
            else // Chromatic data...
            {
                if (lightness < 0.5) saturation = delta / (max + min);
                else saturation = delta / (2 - max - min);

                var del_R = ( ( ( max - r ) / 6 ) + ( max / 2 ) ) / max;
                var del_G = ( ( ( max - g ) / 6 ) + ( max / 2 ) ) / max;
                var del_B = ( ( ( max - b ) / 6 ) + ( max / 2 ) ) / max;

                if      ( r == max ) hue = del_B - del_G;
                else if ( g == max ) hue = ( 1.0 / 3.0 ) + del_R - del_B;
                else /*if ( b == max )*/ hue = ( 2.0 / 3.0 ) + del_G - del_R;

                if ( hue < 0 ) hue += 1;
                if ( hue > 1 ) hue -= 1;
            }
            return new HSL(hue, saturation, lightness);
        }

        public static RGB ToRGB(this HSL hsl)
        {
            byte r, g, b;
            if (hsl.Saturation == 0)
            {
                r = (byte)Math.Round(hsl.Lightness * 255);
                g = (byte)Math.Round(hsl.Lightness * 255);
                b = (byte)Math.Round(hsl.Lightness * 255);
            }
            else
            {
                double var2;
                if (hsl.Lightness < 0.5) var2 = hsl.Lightness * (1 + hsl.Saturation);
                else var2 = (hsl.Lightness + hsl.Saturation) - (hsl.Saturation * hsl.Lightness);

                var var1 = 2 * hsl.Lightness - var2;

                double Hue_2_RGB(double v1, double v2, double vH)
                {
                    if ( vH < 0 ) vH += 1;
                    if( vH > 1 ) vH -= 1;
                    if ( ( 6 * vH ) < 1 ) return ( v1 + ( v2 - v1 ) * 6 * vH );
                    if ( ( 2 * vH ) < 1 ) return ( v2 );
                    if ( ( 3 * vH ) < 2 ) return ( v1 + ( v2 - v1 ) * ( ( 2.0 / 3.0 ) - vH ) * 6 );
                    return ( v1 );
                }
                r = (byte)Math.Round(255 * Hue_2_RGB( var1, var2, hsl.Hue + ( 1.0 / 3.0 ) ));
                g = (byte)Math.Round(255 * Hue_2_RGB( var1, var2, hsl.Hue ));
                b = (byte)Math.Round(255 * Hue_2_RGB( var1, var2, hsl.Hue - ( 1.0 / 3.0 ) ));
            }
            return new RGB(r, g, b);
        }
    }
}