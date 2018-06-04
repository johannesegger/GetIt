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
        public Position Position { get; }

        public double Direction { get; }

        public Pen Pen { get; }

        public Size Size { get; }

        public byte[] IdleCostume { get; }

        public Player(Size size, Position position, byte[] idleCostume)
        {
            Size = size;
            Position = position;
            IdleCostume = idleCostume;
        }
    }
}