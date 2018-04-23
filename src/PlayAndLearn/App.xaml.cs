using Avalonia;
using Avalonia.Markup.Xaml;

namespace PlayAndLearn
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
   }
}