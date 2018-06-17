using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Avalonia.Media.Imaging;

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
            Direction = (direction % 360 + 360) % 360;
            Pen = pen;
            IdleCostume = idleCostume;
        }
    }
}