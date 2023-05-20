using Avalonia;
using Avalonia.Controls;
using GetIt.UIV2.ViewModels;

namespace GetIt.UIV2.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.Opened += (s, e) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.IsLoaded = true;
            }
        };
    }

    public void Scene_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == nameof(Border.Bounds))
        {
            if (sender is Visual element && element.DataContext is MainWindowViewModel mainViewModel)
            {
                mainViewModel.SceneSize = new Size(element.Bounds.Width, element.Bounds.Height);
            }
        }
    }
}
