using System;
using System.Linq;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;

namespace GetIt
{
    internal class App : Application
    {
        public override void Initialize()
        {
            var baseUri = new Uri("resm:GetIt.App.xaml?assembly=GetIt");
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