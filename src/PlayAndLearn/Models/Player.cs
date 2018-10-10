using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Avalonia.Media.Imaging;
using PlayAndLearn.Utils;
using PropertyChanged;

namespace PlayAndLearn.Models
{
    [AddINotifyPropertyChangedInterface]
    public sealed class Player
    {
        public Size Size { get; }

        public Position Position { get; internal set; }

        public Degrees Direction { get; internal set; }

        public Pen Pen { get; internal set; }

        internal SpeechBubble SpeechBubble { get; set; }

        internal Costume Costume { get; }

        public Player(Size size, Position position, Degrees direction, Pen pen, SpeechBubble speechBubble, Costume costume)
        {
            Size = size ?? throw new ArgumentNullException(nameof(size));
            Position = position ?? throw new ArgumentNullException(nameof(position));
            Direction = direction ?? throw new ArgumentNullException(nameof(direction));
            Pen = pen ?? throw new ArgumentNullException(nameof(pen));
            SpeechBubble = speechBubble ?? throw new ArgumentNullException(nameof(speechBubble));
            Costume = costume ?? throw new ArgumentNullException(nameof(costume));
        }
    }
}