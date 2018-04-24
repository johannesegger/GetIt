using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using PropertyChanged;

namespace PlayAndLearn.Models
{
    [AddINotifyPropertyChangedInterface]
    public sealed class Player
    {
        public Position Position { get; private set; }

        public Size Size { get; }

        public IObservable<string> IdleCostume { get; }

        public Player(Size size, Position position, IObservable<string> idleCostume)
        {
            Size = size;
            Position = position;
            IdleCostume = idleCostume;
        }

        public void MoveTo(Position position)
        {
            Position = position;
        }
    }
}