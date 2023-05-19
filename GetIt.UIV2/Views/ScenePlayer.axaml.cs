using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using GetIt.UIV2.ViewModels;
using System;

namespace GetIt.UIV2.Views;

public partial class ScenePlayer : UserControl
{
    public ScenePlayer()
    {
        InitializeComponent();
    }

    private void SpeechBubbleBorder_DataContextChanged(object sender, EventArgs e)
    {
        if (((IDataContextProvider)sender).DataContext is SpeechBubbleViewModel speechBubble)
        {
            var element = (IVisual)sender;
            speechBubble.Size = new Size(element.Bounds.Width, element.Bounds.Height);
        }
    }

    public void SpeechBubbleBorder_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == nameof(Visual.Bounds))
        {
            if (sender is Visual element && element.DataContext is SpeechBubbleViewModel speechBubble)
            {
                speechBubble.Size = new Size(element.Bounds.Width, element.Bounds.Height);
            }
        }
    }

    private void SpeechBubbleTextAnswer_Initialized(object sender, EventArgs e)
    {
        ((IInputElement)sender).Focus();
    }

    private void SpeechBubbleTextAnswer_DataContextChanged(object sender, EventArgs e)
    {
        ((IInputElement)sender).Focus();
    }
}
