using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using PlayAndLearn.Models;

namespace PlayAndLearn.Utils
{
    internal static class CostumeExtensions
    {
        public static IControl ToAvaloniaControl(this Costume costume, Models.Size size)
        {
            var canvas = new Canvas()
                .Do(p => p.Width = size.Width)
                .Do(p => p.Height = size.Height);
            canvas.RenderTransform = new ScaleTransform(size.Width / costume.Size.Width, size.Height / costume.Size.Height);
            canvas.RenderTransformOrigin = new Avalonia.RelativePoint(0, 0, RelativeUnit.Relative);
            var children = costume.Paths
                .Select(path =>
                    new Path
                    {
                        Fill = path.Fill.ToAvaloniaBrush(),
                        Data = PathGeometry.Parse(path.Data)
                    }
                )
                .ToList();
            canvas.Children.AddRange(children);
            return canvas;
        }
    }
}