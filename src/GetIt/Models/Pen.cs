namespace GetIt.Models
{
    [Equals]
    public sealed class Pen
    {
        public static readonly Pen Default = new Pen(false, 1, RGBColor.Black);

        public Pen(bool isOn, double weight, RGB color)
        {
            IsOn = isOn;
            Weight = weight;
            Color = color ?? throw new System.ArgumentNullException(nameof(color));
        }

        public bool IsOn { get; }
        public double Weight { get; }
        public RGB Color { get; }
    }
}