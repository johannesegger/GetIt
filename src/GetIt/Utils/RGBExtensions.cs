using Avalonia.Media;
using GetIt.Models;

namespace GetIt.Utils
{
    internal static class RGBExtensions
    {
        public static Color ToAvaloniaColor(this RGB color)
        {
            return new Color(0xFF, color.Red, color.Green, color.Blue);
        }
    }
}