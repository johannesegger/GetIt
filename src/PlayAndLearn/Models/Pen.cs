namespace PlayAndLearn.Models
{
    [Equals]
    public sealed class Pen
    {
        public Pen(bool isOn, double weight, RGB color)
        {
            IsOn = isOn;
            Weight = weight;
            Color = color ?? throw new System.ArgumentNullException(nameof(color));
        }

        public bool IsOn { get; }

        public double Weight { get; }

        public RGB Color { get; }

        public Pen WithIsOn(bool isOn)
        {
            return new Pen(isOn, Weight, Color);
        }

        public Pen WithWeight(double weight)
        {
            return new Pen(IsOn, weight, Color);
        }

        public Pen WithColor(RGB color)
        {
            return new Pen(IsOn, Weight, color);
        }
    }
}