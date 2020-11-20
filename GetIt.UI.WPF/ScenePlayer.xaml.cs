using System;
using System.Collections.Generic;
using System.Text;
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
    /// Interaction logic for ScenePlayer.xaml
    /// </summary>
    public partial class ScenePlayer : UserControl
    {
        public ScenePlayer()
        {
            InitializeComponent();
        }

        private void SpeechBubbleBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is SpeechBubbleViewModel speechBubble)
            {
                speechBubble.Size = new Size(e.NewSize.Width, e.NewSize.Height);
            }
        }

        private void SpeechBubbleBorder_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is SpeechBubbleViewModel speechBubble)
            {
                var element = (Path)sender;
                speechBubble.Size = new Size(element.ActualWidth, element.ActualHeight);
            }
        }
    }
}
