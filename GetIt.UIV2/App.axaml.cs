using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GetIt.UIV2.ViewModels;
using GetIt.UIV2.Views;

namespace GetIt.UIV2;

internal partial class App : Application
{
    public MainWindowViewModel? ViewModel { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = ViewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
