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
    public static class Zombie
    {
        private static string AssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static Player CreateDefault() => new Player(
            new Size(107, 169),
            new Position(0, 0),
            Observable
                .Interval(TimeSpan.FromSeconds(1.0 / 4))
                .ObserveOn(AvaloniaScheduler.Instance)
                .Select(i =>
                {
                    string getImage(int index) =>
                        Path.Combine(AssemblyDir, $@"Models\Zombie\Idle\idle_{i % 4}.png");
                    switch (i % 7)
                    {
                        case 0: case 6: return getImage(0);
                        case 1: case 5: return getImage(1);
                        case 2: case 4: return getImage(2);
                        case 3: default: return getImage(3);
                    }
                })
        );
    }
}