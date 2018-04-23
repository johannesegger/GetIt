using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PlayAndLearn
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public Canvas Scene => this.FindControl<Canvas>("Scene");
    }
}