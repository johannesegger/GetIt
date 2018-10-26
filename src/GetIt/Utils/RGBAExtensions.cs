using Avalonia.Media;
using GetIt.Models;

namespace GetIt.Utils
{
    internal static class RGBAExtensions
    {
        public static Color ToAvaloniaColor(this RGBA color)
        {
            return new Color(color.Alpha, color.Red, color.Green, color.Blue);
        }
    }
}