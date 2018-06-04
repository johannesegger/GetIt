namespace PlayAndLearn
{
    [Equals]
    public sealed class RGB
    {
        public RGB(byte red, byte green, byte blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }

        public byte Red { get; }
        public byte Green { get; }
        public byte Blue { get; }

        public override string ToString() => $"rgb({Red}, {Green}, {Blue})";
    }
}