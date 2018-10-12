namespace PlayAndLearn.Models
{
    [Equals]
    public sealed class Size
    {
        public Size(double width, double height)
        {
            Width = width;
            Height = height;
        }

        public double Width { get; }
        public double Height { get; }

        public static Size operator*(Size size, double factor)
        {
            return new Size(size.Width * factor, size.Height * factor);
        }
    }
}