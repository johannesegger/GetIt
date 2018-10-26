using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

        public static Costume CreatePolygon(RGBA fillColor, IEnumerable<Position> points)
        {
            points = points
                .Select(p => new Position(p.X, -p.Y))
                .ToList();

            var minX = points.Min(p => p.X);
            var maxX = points.Max(p => p.X);
            var minY = points.Min(p => p.Y);
            var maxY = points.Max(p => p.Y);

            var transformedPoints = points
                .Select(p => new Position(p.X - minX, p.Y - minY))
                .Select((p, i) => $"{(i == 0 ? "M" : "L")} {p.X},{p.Y}");
            var path = $"{string.Join(" ", transformedPoints)} Z";

            return new Costume(
                new Size(maxX - minX, maxY - minY),
                ImmutableList<GeometryPath>.Empty
                    .Add(new GeometryPath(fillColor, path))
            );
        }

        public static Costume CreatePolygon(RGBA fillColor, params Position[] points)
        {
            return CreatePolygon(fillColor, points.AsEnumerable());
        }
    }
}