using System.Collections.Immutable;
using GetIt.Models;

namespace GetIt
{
    public static class Costumes
    {
        public static Costume CreateRectangle(Size size, RGBA fillColor)
        {
            return new Costume(
                size,
                ImmutableList<GeometryPath>.Empty
                    .Add(new GeometryPath(fillColor, $"M 0,0 L 0,{size.Height} L {size.Width},{size.Height} L {size.Width},0 Z"))
            );
        }

        public static Costume CreateCircle(double radius, RGBA fillColor)
        {
            return new Costume(
                new Size(2 * radius, 2 * radius),
                ImmutableList<GeometryPath>.Empty
                    .Add(new GeometryPath(fillColor, $"M 0,{radius} A {radius},{radius} 0 1 0 {2 * radius},{radius} A {radius},{radius} 0 1 0 0,{radius}"))
            );
        }
    }
}