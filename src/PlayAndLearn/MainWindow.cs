using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
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
            Scene = new Canvas();
            Content = Scene;
        }

        public Canvas Scene { get; }
    }
}