using System;
using System.IO;
using System.Reactive.Linq;
using System.Reflection;

namespace PlayAndLearn.Models
{
    public sealed class Knight
    {
        private static string AssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); 

        public IObservable<string> Costume { get; } =
            Observable
                .Interval(TimeSpan.FromSeconds(1.0 / 8))
                .Select(i => Path.Combine(AssemblyDir, $@"Models\Knight\Idle\{i % 7}.png"));

        public static Knight Create() => new Knight();
    }
}