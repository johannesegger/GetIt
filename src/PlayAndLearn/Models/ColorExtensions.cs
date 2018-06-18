using Avalonia.Media;

namespace PlayAndLearn.Models
{
    public static class ColorExtensions
    {
        public static Color ToColor(this RGB color)
        {
            return Color.FromRgb(color.Red, color.Green, color.Blue);
        }
    }
}