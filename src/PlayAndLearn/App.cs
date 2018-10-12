using System;
using System.Linq;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;

namespace PlayAndLearn
{
    public class App : Application
    {
        public override void Initialize()
        {
            var baseUri = new Uri("resm:PlayAndLearn.App.xaml?assembly=PlayAndLearn");
            var styles =
                new[]
                {
                    new Uri("resm:Avalonia.Themes.Default.DefaultTheme.xaml?assembly=Avalonia.Themes.Default"),
                    new Uri("resm:Avalonia.Themes.Default.Accents.BaseLight.xaml?assembly=Avalonia.Themes.Default")
                }
                .Select(source => new StyleInclude(baseUri) { Source = source });
            foreach (var style in styles)
            {
                Styles.Add(style);
            }
        }
   }
}