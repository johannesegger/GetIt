namespace GetIt.Models
{
    [Equals]
    public sealed class Degrees
    {
        public Degrees(double value)
        {
            Value = (value % 360 + 360) % 360;
        }

        public double Value { get; }

        public override string ToString()
        {
            return $"{Value:F2}";
        }

        public static implicit operator Degrees(double value)
        {
            return new Degrees(value);
        }

        public static Degrees operator +(Degrees v1, Degrees v2)
        {
            return new Degrees(v1.Value + v2.Value);
        }

        public static Degrees operator -(Degrees v1, Degrees v2)
        {
            return new Degrees(v1.Value - v2.Value);
        }
    }
}