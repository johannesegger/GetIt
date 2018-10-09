using System.Collections.Immutable;
using PlayAndLearn.Models;

namespace PlayAndLearn.Utils
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