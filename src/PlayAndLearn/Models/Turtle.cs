using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Portable.Xaml;

namespace PlayAndLearn.Models
{
    public static class Turtle
    {
        private static string AssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static readonly Player Default = CreateDefault();

        public static Player CreateDefault() => new Player(
            new Size(50, 50),
            new Position(0, 0),
            new Degrees(0),
            new Pen(false, 1, new RGB(0x00, 0x00, 0x00)),
            SpeechBubble.Empty,
            Observable.Return<Func<Stream>>(
                () => Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceStream("PlayAndLearn.Models.Turtle.default.png")
            )
        );
    }
}