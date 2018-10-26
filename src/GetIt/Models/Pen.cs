namespace GetIt.Models
{
    [Equals]
    public sealed class Pen
    {
        public static readonly Pen Default = new Pen(false, 1, RGBAColor.Black);

        public Pen(bool isOn, double weight, RGBA color)
        {
            IsOn = isOn;
            Weight = weight;
            Color = color ?? throw new System.ArgumentNullException(nameof(color));
        }

        public bool IsOn { get; }
        public double Weight { get; }
        public RGBA Color { get; }
    }
}