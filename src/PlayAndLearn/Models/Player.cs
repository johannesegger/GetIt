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
        public Size Size { get; }

        public Position Position { get; set; }

        public Degrees Direction { get; set; }

        public Pen Pen { get; set; }

        public IObservable<Func<Stream>> IdleCostume { get; }

        public Player(Size size, Position position, Degrees direction, Pen pen, IObservable<Func<Stream>> idleCostume)
        {
            Size = size ?? throw new ArgumentNullException(nameof(size));
            Position = position ?? throw new ArgumentNullException(nameof(position));
            Direction = direction ?? throw new ArgumentNullException(nameof(direction));
            Pen = pen ?? throw new ArgumentNullException(nameof(pen));
            IdleCostume = idleCostume ?? throw new ArgumentNullException(nameof(idleCostume));
        }
    }
}