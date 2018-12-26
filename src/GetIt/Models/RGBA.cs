using Elmish.Net;

namespace GetIt
{
    /// <summary>
    /// Defines a color using red, green, blue components and an alpha value for transparency.
    /// </summary>
    [Equals]
    public sealed class RGBA
    {
        /// <summary>
        /// Initializes all properties of an instance.
        /// </summary>
        public RGBA(byte red, byte green, byte blue, byte alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        /// <summary>
        /// The amount of red.
        /// </summary>
        public byte Red { get; }

        /// <summary>
        /// The amount of green.
        /// </summary>
        public byte Green { get; }

        /// <summary>
        /// The amount of blue.
        /// </summary>
        public byte Blue { get; }

        /// <summary>
        /// The amount of opacity.
        /// </summary>
        public byte Alpha { get; }

        /// <summary>
        /// Creates a new color based on the current one with a given alpha value.
        /// </summary>
        public RGBA WithAlpha(byte alpha)
        {
            return this.With(p => p.Alpha, alpha);
        }

        /// <inheritdoc />
        public override string ToString() => $"rgba({Red}, {Green}, {Blue}, {Alpha})";
    }
}