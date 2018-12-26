namespace GetIt
{
    /// <summary>
    /// A pen of a player.
    /// </summary>
    [Equals]
    public sealed class Pen
    {
        internal static readonly Pen Default = new Pen(false, 1, RGBAColor.Black);

        internal Pen(bool isOn, double weight, RGBA color)
        {
            IsOn = isOn;
            Weight = weight;
            Color = color ?? throw new System.ArgumentNullException(nameof(color));
        }

        /// <summary>
        /// The state of the pen.
        /// </summary>
        public bool IsOn { get; }

        /// <summary>
        /// The weight of the pen.
        /// </summary>
        public double Weight { get; }

        /// <summary>
        /// The color of the pen.
        /// </summary>
        public RGBA Color { get; }
    }
}