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
            InitializeComponent();
            using (var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PlayAndLearn.Icon.ico"))
            {
                Icon = new WindowIcon(iconStream);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public Canvas Scene => this.FindControl<Canvas>("Scene");
    }
}