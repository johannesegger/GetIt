namespace GetIt
{
    /// <summary>
    /// Defines a size of a 2D object.
    /// </summary>
    [Equals]
    public sealed class Size
    {
        /// <summary>
        /// Initializes all properties of an instance.
        /// </summary>
        public Size(double width, double height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Width of the size.
        /// </summary>
        public double Width { get; }

        /// <summary>
        /// Height of the size.
        /// </summary>
        public double Height { get; }

        /// <summary>
        /// Overloaded `*` operator.
        /// </summary>
        public static Size operator*(Size size, double factor)
        {
            return new Size(size.Width * factor, size.Height * factor);
        }
    }
}