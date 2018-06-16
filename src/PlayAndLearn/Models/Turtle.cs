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

        public static Player CreateDefault() => new Player(
            new Size(50, 50),
            new Position(0, 0),
            direction: 0,
            pen: null,
            idleCostume: ReadEmbeddedResource("Models.Turtle.default.png")
        );
        
        private static byte[] ReadEmbeddedResource(string name)
        {
            var asm = Assembly.GetExecutingAssembly();
            using (var sourceStream = asm.GetManifestResourceStream($"{asm.GetName().Name}.{name}"))
            using (var targetStream = new MemoryStream())
            {
                sourceStream.CopyTo(targetStream);
                return targetStream.ToArray();
            }
        }
    }
}