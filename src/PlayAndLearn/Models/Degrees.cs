namespace PlayAndLearn.Models
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
    }
}