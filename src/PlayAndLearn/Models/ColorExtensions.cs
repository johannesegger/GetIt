using System;
using Avalonia.Media;
using Elmish.Net;

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