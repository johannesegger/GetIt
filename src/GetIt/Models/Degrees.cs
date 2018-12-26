namespace GetIt
{
    /// <summary>
    /// A floating-point value between 0 and 360.
    /// </summary>
    [Equals]
    public sealed class Degrees
    {
        /// <summary>
        /// 0 degrees.
        /// </summary>
        public static readonly Degrees Zero = new Degrees(0);

        /// <summary>
        /// Initializes all properties of an instance.
        /// </summary>
        /// <param name="value">The angle in degrees. Doesn't have to be between 0 and 360.</param>
        public Degrees(double value)
        {
            Value = (value % 360 + 360) % 360;
        }

        /// <summary>
        /// The number of degrees.
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// Implicit cast from `double`.
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator Degrees(double value)
        {
            return new Degrees(value);
        }

        /// <summary>
        /// Overloaded `+` operator.
        /// </summary>
        public static Degrees operator +(Degrees v1, Degrees v2)
        {
            return new Degrees(v1.Value + v2.Value);
        }

        /// <summary>
        /// Overloaded `-` operator. 
        /// </summary>
        public static Degrees operator -(Degrees v1, Degrees v2)
        {
            return new Degrees(v1.Value - v2.Value);
        }
    }
}