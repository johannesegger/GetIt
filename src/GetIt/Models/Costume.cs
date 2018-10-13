using System.Collections.Immutable;

namespace GetIt.Models
{
    public class Costume
    {
        public Size Size { get; }
        public IImmutableList<GeometryPath> Paths { get; }

        public Costume(Size size, IImmutableList<GeometryPath> paths)
        {
            Size = size;
            Paths = paths;
        }
    }
}