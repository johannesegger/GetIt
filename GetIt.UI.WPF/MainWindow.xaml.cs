using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            Loaded += (s, e) => DataContext = DesignTimeData.Main;
        }

        private void SpeechBubble_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var container = (FrameworkElement)sender;
            container.RenderTransform = new TranslateTransform(container.ActualWidth, -container.ActualHeight);
            var path = (Path)VisualTreeHelper.GetChild(container, 0);
            path.Width = container.ActualWidth;
            path.Height = container.ActualHeight;
            double bubbleWidth = container.ActualWidth - 2 * 10;
            double bubbleHeight = container.ActualHeight - 2 * 5 - 15;
            path.Data = Geometry.Parse($"M 10,5 h {bubbleWidth} c 10,0 10,{bubbleHeight} 0,{bubbleHeight} h -{bubbleWidth - 40} c 0,7 -5,13 -15,15 s 3,-6 0,-15 h -25 c -10,0 -10,-{bubbleHeight} 0,-{bubbleHeight}");
        }
    }
}
