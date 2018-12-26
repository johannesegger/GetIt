using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace GetIt
{
    /// <summary>
    /// A player outfit.
    /// </summary>
    public class Costume
    {
        /// <summary>
        /// Size of the costume.
        /// </summary>
        public Size Size { get; }
        
        /// <summary>
        /// List of paths that define the costume.
        /// </summary>
        public IImmutableList<GeometryPath> Paths { get; }

        /// <summary>
        /// Initializes all properties of an instance.
        /// </summary>
        public Costume(Size size, IImmutableList<GeometryPath> paths)
        {
            Size = size;
            Paths = paths;
        }

        /// <summary>
        /// Creates a costume that has the shape of a polygon.
        /// </summary>
        /// <param name="fillColor">The color that is used to fill the polygon.</param>
        /// <param name="points">The points that define the polygon.</param>
        /// <returns>The created costume.</returns>
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

        /// <summary>
        /// Creates a costume that has the shape of a polygon.
        /// </summary>
        /// <param name="fillColor">The color that is used to fill the polygon.</param>
        /// <param name="points">The points that define the polygon.</param>
        /// <returns>The created costume.</returns>
        public static Costume CreatePolygon(RGBA fillColor, params Position[] points)
        {
            return CreatePolygon(fillColor, points.AsEnumerable());
        }

        /// <summary>
        /// Creates a costume that has the shape of a rectangle.
        /// The left bottom corner is at (0,0).
        /// </summary>
        /// <param name="fillColor">The color that is used to fill the rectangle.</param>
        /// <param name="size">The size of the rectangle.</param>
        /// <returns>The created costume.</returns>
        public static Costume CreateRectangle(RGBA fillColor, Size size)
        {
            return CreatePolygon(
                fillColor,
                new Position(0, 0),
                new Position(0, size.Height),
                new Position(size.Width, size.Height),
                new Position(size.Width, 0));
        }

        /// <summary>
        /// Creates a costume that has the shape of a circle.
        /// The center point is at (radius,radius).
        /// </summary>
        /// <param name="fillColor">The color that is used to fill the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <returns>The created costume.</returns>
        public static Costume CreateCircle(RGBA fillColor, double radius)
        {
            return new Costume(
                new Size(2 * radius, 2 * radius),
                ImmutableList<GeometryPath>.Empty
                    .Add(new GeometryPath(fillColor, $"M 0,{radius} A {radius},{radius} 0 1 0 {2 * radius},{radius} A {radius},{radius} 0 1 0 0,{radius}"))
            );
        }
    }
}