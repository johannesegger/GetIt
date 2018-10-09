using Avalonia.Media;
using PlayAndLearn.Models;

namespace PlayAndLearn.Utils
{
    internal static class RGBExtensions
    {
        public static IBrush ToAvaloniaBrush(this RGB color)
        {
            return new SolidColorBrush(new Color(0xFF, color.Red, color.Green, color.Blue));
        }
    }
}