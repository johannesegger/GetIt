using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Avalonia.Media.Imaging;
using PropertyChanged;

namespace PlayAndLearn.Models
{
    [AddINotifyPropertyChangedInterface]
    public sealed class Player
    {
        public Position Position { get; set; }

        public double Direction { get; set; }

        public Pen Pen { get; set; }

        public Size Size { get; }

        public IObservable<Func<Stream>> IdleCostume { get; }

        public Player(Size size, Position position, IObservable<Func<Stream>> idleCostume)
        {
            Size = size;
            Position = position;
            IdleCostume = idleCostume;
        }
    }
}