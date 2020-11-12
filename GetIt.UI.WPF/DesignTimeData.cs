using System;
using System.IO;
using System.Windows.Media;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace GetIt.UI
{
    internal static class DesignTimeData
    {
        static DesignTimeData()
        {
            Main = new MainViewModel(new Size(600, 480), isMaximized: false);
            Main.AddPenLine(new Position(0, 0), new Position(-100, 100), 1, Brushes.SteelBlue);
            Main.AddPenLine(new Position(-100, 100), new Position(-200, 0), 5, Brushes.Crimson);
            Main.AddPlayer(
                new PlayerId(Guid.NewGuid()),
                player =>
                {
                    player.Image = LoadTurtleImage();
                    player.Size = new Size(50, 50);
                    player.Position = new Position(0, 0);
                    player.Angle = 0;
                    player.SpeechBubble = new SaySpeechBubbleViewModel()
                    {
                        Text = "Hey there! I'm Oscar, the turtle. Nice to meet you.",
                    };
                });
            Main.AddPlayer(
                new PlayerId(Guid.NewGuid()),
                player =>
                {
                    player.Image = LoadTurtleImage();
                    player.Size = new Size(125, 125);
                    player.Position = new Position(100, 100);
                    player.Angle = 225;
                    player.SpeechBubble = new SaySpeechBubbleViewModel()
                    {
                        Text = "Hey there! I'm Oscar, the turtle. Nice to meet you.",
                    };
                });
        }

        public static ImageSource LoadTurtleImage()
        {
            var settings = new WpfDrawingSettings
            {
                IncludeRuntime = true,
                TextAsGeometry = false
            };

            var converter = new FileSvgReader(settings);
            var drawing = converter.Read(Path.Combine(GetProjectDir(), "assets", "Turtle1.svg"));
            return new DrawingImage(drawing);
        }

        private static string GetProjectDir([System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "")
        {
            return new FileInfo(sourceFilePath).Directory.Parent.FullName;
        }

        public static MainViewModel Main { get; }
    }
}
