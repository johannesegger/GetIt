using System.Windows;

namespace GetIt.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Loaded += (s, e) => DataContext = DesignTimeData.Main;
        }

        private void Scene_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ((MainViewModel)((FrameworkElement)sender).DataContext).SceneSize = new Size(e.NewSize.Width, e.NewSize.Height);
        }
    }
}
