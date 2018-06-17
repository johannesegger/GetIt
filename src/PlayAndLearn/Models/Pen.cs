using System;

namespace PlayAndLearn.Models
{
    public sealed class Pen
    {
        public static Pen CreateDefault() =>
            new Pen(1, new RGB(0, 0, 0), false);

        public Pen(double weight, RGB color, bool isOn)
        {
            Weight = weight;
            Color = color;
            IsOn = isOn;
        }

        public double Weight { get; }
        public RGB Color { get; }
        public bool IsOn { get; }
    }
}