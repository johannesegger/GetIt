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

        private void Path_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var speechBubble = ((PlayerViewModel)((Path)sender).DataContext).SpeechBubble;
            if (speechBubble != null)
            {
                speechBubble.Size = new Size(e.NewSize.Width, e.NewSize.Height);
            }
        }
    }
}
