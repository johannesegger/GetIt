using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PlayAndLearn.Utils;
using PropertyChanged;

namespace PlayAndLearn
{
    [DoNotNotify]
    public class MainWindow : Window
    {
        public MainWindow()
        {
            using (var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PlayAndLearn.Icon.ico"))
            {
                Icon = new WindowIcon(iconStream);
            }
            Title = "Play and Learn";
            Content = Scene = new Canvas();
            PlayerPanel = new WrapPanel()
                .AttachProperty(Canvas.LeftProperty, 0)
                .AttachProperty(Canvas.BottomProperty, 0);
            Scene.Children.Add(PlayerPanel);
        }

        public Canvas Scene { get; }
        public Panel PlayerPanel { get; }
    }
}