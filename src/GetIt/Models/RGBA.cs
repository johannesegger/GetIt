using Elmish.Net;

namespace GetIt
{
    [Equals]
    public sealed class RGBA
    {
        public RGBA(byte red, byte green, byte blue, byte alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        public byte Red { get; }
        public byte Green { get; }
        public byte Blue { get; }
        public byte Alpha { get; }

        public RGBA WithAlpha(byte alpha)
        {
            return this.With(p => p.Alpha, alpha);
        }

        public override string ToString() => $"rgba({Red}, {Green}, {Blue}, {Alpha})";
    }
}