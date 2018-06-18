namespace PlayAndLearn.Models
{
    public sealed class Player
    {
        public Size Size { get; }

        public Position Position { get; }

        public double Direction { get; }

        public Pen Pen { get; }

        public byte[] IdleCostume { get; }

        public Player(Size size, Position position, double direction, Pen pen, byte[] idleCostume)
        {
            Size = size;
            Position = position;
            Direction = direction;
            Pen = pen;
            IdleCostume = idleCostume;
        }
    }
}