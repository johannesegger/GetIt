using Avalonia.Media;
using PlayAndLearn.Models;

namespace PlayAndLearn.Utils
{
    internal static class RGBExtensions
    {
        public static Color ToAvaloniaColor(this RGB color)
        {
            return new Color(0xFF, color.Red, color.Green, color.Blue);
        }
    }
}