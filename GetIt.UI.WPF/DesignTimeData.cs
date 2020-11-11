using System.IO;
using System.Reactive.Linq;
using System.Windows.Media;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace GetIt.UI
{
    internal static class DesignTimeData
    {
        static DesignTimeData()
        {
            var sceneBoundsObservable = Observable.Return(new Rectangle(new Position(-300, -240), new Size(600, 480)));
            Main = new MainViewModel
            {
                Title = "Get It",
                SceneWidth = 600,
                SceneHeight = 480,
                PenLines =
                {
                    new PenLineViewModel(sceneBoundsObservable, new Position(0, 0), new Position(-100, 100), 1, Brushes.SteelBlue),
                    new PenLineViewModel(sceneBoundsObservable, new Position(-100, 100), new Position(-200, 0), 5, Brushes.Crimson)
                },
                Players =
                {
                    new PlayerViewModel(sceneBoundsObservable)
                    {
                        Image = LoadTurtleImage(),
                        OriginalWidth = 50,
                        OriginalHeight = 50,
                        X = 0,
                        Y = 0,
                        ScaleFactor = 1,
                        Angle = 0
                    },
                    new PlayerViewModel(sceneBoundsObservable)
                    {
                        Image = LoadTurtleImage(),
                        OriginalWidth = 50,
                        OriginalHeight = 50,
                        X = 100,
                        Y = 100,
                        ScaleFactor = 2.5,
                        Angle = 225,
                        SpeechBubble = new SaySpeechBubbleViewModel()
                        {
                            Text = "Hey there! I'm Oscar, the turtle. Nice to meet you.",
                        }
                    }
                }
            };
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
