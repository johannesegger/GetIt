using System;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;
using GetIt.UIV2.ViewModels;
using SkiaSharp;

namespace GetIt.UIV2;

#if DEBUG
public
#endif
static class DesignTimeData
{
    static DesignTimeData()
    {
        Main = new MainWindowViewModel(new Size(600, 480), isMaximized: false);
        Main.AddPenLine(new Position(0, 0), new Position(-100, 100), 1, RGBAColors.SteelBlue);
        Main.AddPenLine(new Position(-100, 100), new Position(-200, 0), 5, RGBAColors.Crimson);
        Main.AddPlayer(
            new PlayerId(Guid.NewGuid()),
            player =>
            {
                player.Image = LoadTurtleImage();
                player.Size = new Size(50, 50);
                player.Position = new Position(0, 0);
                player.Angle = 0;
                player.SpeechBubble = new SaySpeechBubbleViewModel()
                {
                    Text = "Hey there! I'm Oscar, the turtle. Nice to meet you.",
                };
                player.ZIndex = 2;
            });
        Main.AddPlayer(
            new PlayerId(Guid.NewGuid()),
            player =>
            {
                player.Image = LoadTurtleImage();
                player.Size = new Size(125, 125);
                player.Position = new Position(100, 100);
                player.Angle = 225;
                player.SpeechBubble = new SaySpeechBubbleViewModel()
                {
                    Text = "Hey there! I'm Oscar, the turtle. Nice to meet you.",
                };
                player.ZIndex = 1;
            });
        Main.BackgroundImage = LoadBackground();
    }

    public static IImage LoadTurtleImage()
    {
        return new Avalonia.Svg.Skia.SvgImage
        {
            Source = SvgSource.Load<SvgSource>(Path.Combine(GetProjectDir(), "assets", "Turtle1.svg"), baseUri: null)
        };
    }

    private static SvgSource LoadBackground()
    {
        var svg = new SvgSource();
        svg.FromSvg(Background.Baseball1.SvgData);
        return svg;
    }

    private static string GetProjectDir([System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "")
    {
        return new FileInfo(sourceFilePath).Directory?.Parent?.FullName ?? "";
    }

    public static MainWindowViewModel Main { get; }
    public static PlayerViewModel Player => Main.Players[1];
}
