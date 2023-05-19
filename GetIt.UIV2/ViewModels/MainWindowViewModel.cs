using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SkiaSharp;
using Svg.Model;

namespace GetIt.UIV2.ViewModels;

#if DEBUG
public
#endif
class MainWindowViewModel : ViewModelBase
{
    [Reactive]
    public string Title { get; set; } = "Get It";
    [Reactive]
    public Size SceneSize { get; set; }
    private readonly ObservableAsPropertyHelper<Rectangle> sceneBounds;
    public Rectangle SceneBounds => sceneBounds.Value;
    [Reactive]
    public WindowState WindowState { get; set; }
    [Reactive]
    public SizeToContent SizeToContent { get; set; }
    [Reactive]
    public SvgSource? BackgroundImage { get; set; }
    private readonly ObservableAsPropertyHelper<IBitmap?> backgroundBitmapImage;
    public IBitmap? BackgroundBitmapImage => backgroundBitmapImage.Value;
    private readonly ObservableAsPropertyHelper<bool> showInfoBar;
    public bool ShowInfoBar => showInfoBar.Value;
    public ObservableCollection<PlayerViewModel> Players { get; } = new ObservableCollection<PlayerViewModel>();
    public ObservableCollection<PenLineViewModel> PenLines { get; } = new ObservableCollection<PenLineViewModel>();

    public MainWindowViewModel(Size sceneSize, bool isMaximized)
    {
        SceneSize = sceneSize;
        sceneBounds = this.WhenAnyValue(p => p.SceneSize)
            .Select(p => new Rectangle(new Position(-p.Width / 2, -p.Height / 2), p))
            .ToProperty(this, p => p.SceneBounds);
        WindowState = isMaximized ? WindowState.Maximized : WindowState.Normal;
        SizeToContent = isMaximized ? SizeToContent.Manual : SizeToContent.WidthAndHeight;
        // TODO dispose old BackgroundImages and BackgroundBitmapImages
        backgroundBitmapImage = this.WhenAnyValue(
            p => p.BackgroundImage,
            p => p.SceneSize,
            (background, sceneSize) =>
            {
                if (background?.Picture == null || sceneSize == null)
                {
                    return null;
                }
                using var pngStream = new MemoryStream();
                var scale = Math.Max(
                    (float)sceneSize.Width / background.Picture.CullRect.Width,
                    (float)sceneSize.Height / background.Picture.CullRect.Height
                );
                background.Save(pngStream, background: SKColor.Empty, SKEncodedImageFormat.Png, quality: 100, scale, scale);
                pngStream.Position = 0;
                return new Bitmap(pngStream);
            })
            .ToProperty(this, p => p.BackgroundBitmapImage);

        showInfoBar = Players
            .ToObservableChangeSet()
            .AsObservableList()
            .CountChanged
            .Select(count => count > 0)
            .ToProperty(this, p => p.ShowInfoBar);
    }

    internal void AddPlayer(PlayerId playerId, Action<PlayerViewModel> initialize)
    {
        var player = new PlayerViewModel(this.WhenAnyValue(p => p.SceneBounds), playerId);
        initialize(player);
        Players.Add(player);
    }

    public void AddPenLine(Position from, Position to, double thickness, RGBAColor color)
    {
        var penLine = new PenLineViewModel(this.WhenAnyValue(p => p.SceneBounds), from, to, thickness, color);
        PenLines.Add(penLine);
    }
}

#if DEBUG
public
#endif
class PenLineViewModel : ViewModelBase
{
    private readonly ObservableAsPropertyHelper<Point> startPoint;
    public Point StartPoint => startPoint.Value;

    private readonly ObservableAsPropertyHelper<Point> endPoint;
    public Point EndPoint => endPoint.Value;

    public double Thickness { get; }
    public Avalonia.Media.Color Color { get; }
    public IBrush Brush { get; }
    public int ZIndex => 0;

    public PenLineViewModel(IObservable<Rectangle> sceneBoundsObservable, Position from, Position to, double thickness, RGBAColor color)
    {
        startPoint = sceneBoundsObservable
            .Select(p => new Point(from.X - p.Left, p.Top - from.Y))
            .ToProperty(this, p => p.StartPoint);
        endPoint = sceneBoundsObservable
            .Select(p => new Point(to.X - p.Left, p.Top - to.Y))
            .ToProperty(this, p => p.EndPoint);
        Thickness = thickness;
        Color = Avalonia.Media.Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
        Brush = new SolidColorBrush(Color).ToImmutable();
    }
}

#if DEBUG
public
#endif
class PlayerViewModel : ViewModelBase
{
    internal PlayerId Id { get; }
    [Reactive]
    public IImage? Image { get; set; }
    [Reactive]
    public Size Size { get; set; } = new Size(0, 0);
    [Reactive]
    public Position Position { get; set; } = new Position(0, 0);
    [Reactive]
    public double Angle { get; set; }
    [Reactive]
    public int ZIndex { get; set; }
    [Reactive]
    public bool IsVisible { get; set; } = true;
    [Reactive]
    public SpeechBubbleViewModel? SpeechBubble { get; set; }
    private readonly ObservableAsPropertyHelper<Position> offset;
    public Position Offset => offset.Value;
    private readonly ObservableAsPropertyHelper<double> rotation;
    public double Rotation => rotation.Value;
    private readonly ObservableAsPropertyHelper<string> infoText;
    public string InfoText => infoText.Value;

    internal PlayerViewModel(IObservable<Rectangle> sceneBoundsObservable, PlayerId playerId)
    {
        Id = playerId;
        offset = sceneBoundsObservable.CombineLatest(
                this.WhenAnyValue(p => p.Position),
                this.WhenAnyValue(p => p.Size),
                (sceneBounds, position, size) => new Position(position.X - sceneBounds.Left - size.Width / 2, sceneBounds.Top - position.Y - size.Height / 2))
            .ToProperty(this, p => p.Offset);
        rotation = this
            .WhenAnyValue(p => p.Angle, v => 360 - v)
            .ToProperty(this, p => p.Rotation);
        infoText = this
            .WhenAnyValue(
                p => p.Position,
                p => p.Angle,
                (position, angle) => $"X: {position.X:F2} | Y: {position.Y:F2} | ↻ {angle:F2}°")
            .ToProperty(this, p => p.InfoText);
        this.WhenAnyValue(p => p.SpeechBubble)
            .Where(p => p != null).Select(p => p!)
            .Select(speechBubble =>
                sceneBoundsObservable.CombineLatest(
                        speechBubble.WhenAnyValue(p => p.Size),
                        speechBubble.WhenAnyValue(p => p.Position),
                        this.WhenAnyValue(p => p.Position),
                        this.WhenAnyValue(p => p.Size),
                        (sceneBounds, speechBubbleSize, speechBubblePosition, position, size) => new Action(() =>
                        {
                            var playerLeft = position.X - sceneBounds.Left - size.Width / 2;
                            var offsetXRight = playerLeft + size.Width * 0.8;
                            var offsetXLeft = playerLeft - speechBubbleSize.Width + size.Width * 0.2;
                            double offsetX;
                            SpeechBubblePosition newSpeechBubblePosition;
                            var canBeAtRightSide = offsetXRight + speechBubbleSize.Width <= sceneBounds.Size.Width;
                            var canBeAtLeftSide = offsetXLeft >= 0;
                            if (speechBubblePosition == SpeechBubblePosition.Right)
                            {
                                if (!canBeAtRightSide && canBeAtLeftSide)
                                {
                                    offsetX = offsetXLeft;
                                    newSpeechBubblePosition = SpeechBubblePosition.Left;
                                }
                                else
                                {
                                    offsetX = offsetXRight;
                                    newSpeechBubblePosition = SpeechBubblePosition.Right;
                                }
                            }
                            else
                            {
                                if (!canBeAtLeftSide && canBeAtRightSide)
                                {
                                    offsetX = offsetXRight;
                                    newSpeechBubblePosition = SpeechBubblePosition.Right;
                                }
                                else
                                {
                                    offsetX = offsetXLeft;
                                    newSpeechBubblePosition = SpeechBubblePosition.Left;
                                }
                            }
                            var offsetY = Math.Max(0, sceneBounds.Top - position.Y - size.Height / 2 - speechBubbleSize.Height);
                            speechBubble.Offset = new Position(offsetX, offsetY);
                            speechBubble.Position = newSpeechBubblePosition;
                        }))
            )
            .Switch()
            .Subscribe(action => action());
    }
}

public enum SpeechBubblePosition
{
    Left,
    Right
}

#if DEBUG
public
#endif
abstract class SpeechBubbleViewModel : ViewModelBase
{
    [Reactive]
    public string Text { get; set; } = "";
    [Reactive]
    public SpeechBubblePosition Position { get; set; } = SpeechBubblePosition.Right;
    private readonly ObservableAsPropertyHelper<double> scaleX;
    public double ScaleX => scaleX.Value;
    [Reactive]
    public Position Offset { get; set; } = new Position(0, 0);
    [Reactive]
    public Size Size { get; set; } = new Size(0, 0);
    private readonly ObservableAsPropertyHelper<Geometry?> geometry;
    public Geometry? Geometry => geometry.Value;
    public SpeechBubbleViewModel()
    {
        geometry = this.WhenAnyValue(p => p.Size)
            .Select(size =>
            {
                var bubbleWidth = size.Width - 2 * 10;
                var bubbleHeight = size.Height - 2 * 5 - 15;
                if (bubbleWidth <= 0 && bubbleHeight <= 0)
                {
                    return null;
                }
                var data = FormattableString.Invariant($"M 10,5 h {bubbleWidth} c 10,0 10,{bubbleHeight} 0,{bubbleHeight} h -{bubbleWidth - 40} c 0,7 -5,13 -15,15 s 3,-6 0,-15 h -25 c -10,0 -10,-{bubbleHeight} 0,-{bubbleHeight}");
                return Geometry.Parse(data);
            })
            .ToProperty(this, p => p.Geometry);
        scaleX = this.WhenAnyValue(p => p.Position)
            .Select(p => p == SpeechBubblePosition.Right ? 1.0 : -1.0)
            .ToProperty(this, p => p.ScaleX);
    }
}

#if DEBUG
public
#endif
class SaySpeechBubbleViewModel : SpeechBubbleViewModel
{
}

#if DEBUG
public
#endif
class AskBoolSpeechBubbleViewModel : SpeechBubbleViewModel
{
    public ReactiveCommand<bool, bool> ConfirmCommand { get; }

    public AskBoolSpeechBubbleViewModel()
    {
        ConfirmCommand = ReactiveCommand.Create<bool, bool>(answer => answer);
    }
}

#if DEBUG
public
#endif
class AskTextSpeechBubbleViewModel : SpeechBubbleViewModel
{
    [Reactive]
    public string Answer { get; set; } = "";
    public ReactiveCommand<Unit, string> ConfirmCommand { get; }

    public AskTextSpeechBubbleViewModel()
    {
        ConfirmCommand = ReactiveCommand.Create(() => Answer);
    }
}
