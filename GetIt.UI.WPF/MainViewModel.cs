using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GetIt.UI
{
    internal class MainViewModel : ReactiveObject
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
        public ImageSource BackgroundImage { get; set; }
        private readonly ObservableAsPropertyHelper<Visibility> infoBarVisibility;
        public Visibility InfoBarVisibility => infoBarVisibility.Value;
        public ObservableCollection<PlayerViewModel> Players { get; } = new ObservableCollection<PlayerViewModel>();
        public ObservableCollection<PenLineViewModel> PenLines { get; } = new ObservableCollection<PenLineViewModel>();
        private readonly ObservableAsPropertyHelper<ImageSource> penLineBitmap;
        public ImageSource PenLineBitmap => penLineBitmap.Value;

        public MainViewModel(Size sceneSize, bool isMaximized)
        {
            SceneSize = sceneSize;
            sceneBounds = this.WhenAnyValue(p => p.SceneSize)
                .Select(p => new Rectangle(new Position(-p.Width / 2, -p.Height / 2), p))
                .ToProperty(this, p => p.SceneBounds);
            WindowState = isMaximized ? WindowState.Maximized : WindowState.Normal;
            infoBarVisibility = Players
                .ToObservableChangeSet()
                .AsObservableList()
                .CountChanged
                .Select(count => count > 0 ? Visibility.Visible : Visibility.Collapsed)
                .ToProperty(this, p => p.InfoBarVisibility);
            var drawPenLinesObservable = PenLines
                .ObserveCollectionChanges()
                .Select(ev =>
                {
                    if (ev.EventArgs.Action == NotifyCollectionChangedAction.Add)
                    {
                        return new Action<WriteableBitmap>(bitmap =>
                        {
                            foreach (var penLine in ev.EventArgs.NewItems.OfType<PenLineViewModel>())
                            {
                                var thickness = (int)Math.Round(penLine.Thickness);
                                var pen = BitmapFactory.New(thickness, thickness);
                                pen.Clear(System.Windows.Media.Color.FromArgb(penLine.Color.Alpha, penLine.Color.Red, penLine.Color.Green, penLine.Color.Blue));
                                bitmap.DrawLinePenned(
                                    (int)Math.Round(penLine.X1),
                                    (int)Math.Round(penLine.Y1),
                                    (int)Math.Round(penLine.X2),
                                    (int)Math.Round(penLine.Y2),
                                    pen
                                );
                            }
                        });
                    }
                    else if (ev.EventArgs.Action == NotifyCollectionChangedAction.Reset)
                    {
                        return (WriteableBitmap bitmap) => bitmap.Clear();
                    }
                    return (WriteableBitmap bitmap) => {};
                })
                .StartWith((bitmap) =>
                {
                    foreach (var penLine in PenLines)
                    {
                        bitmap.DrawLineAa(
                            (int)penLine.X1,
                            (int)penLine.Y1,
                            (int)penLine.X2,
                            (int)penLine.Y2,
                            System.Windows.Media.Color.FromArgb(penLine.Color.Alpha, penLine.Color.Red, penLine.Color.Green, penLine.Color.Blue),
                            (int)penLine.Thickness
                        );
                    }
                });
            penLineBitmap = this.WhenAnyValue(p => p.SceneSize)
                .Select(size =>
                {
                    var seed = BitmapFactory.New((int)size.Width, (int)size.Height);
                    return drawPenLinesObservable.Scan(seed, (bitmap, fn) => { fn(bitmap); return bitmap; });
                })
                .Switch()
                .ToProperty(this, p => p.PenLineBitmap);
        }

        public void AddPlayer(PlayerId playerId, Action<PlayerViewModel> initialize)
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

    internal class PenLineViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<double> x1;
        public double X1 => x1.Value;
        private readonly ObservableAsPropertyHelper<double> y1;
        public double Y1 => y1.Value;
        private readonly ObservableAsPropertyHelper<double> x2;
        public double X2 => x2.Value;
        private readonly ObservableAsPropertyHelper<double> y2;
        public double Y2 => y2.Value;
        public double Thickness { get; }
        public RGBAColor Color { get; }
        public int ZIndex => 0;

        public PenLineViewModel(IObservable<Rectangle> sceneBoundsObservable, Position from, Position to, double thickness, RGBAColor color)
        {
            x1 = sceneBoundsObservable.Select(p => from.X - p.Left).ToProperty(this, p => p.X1);
            y1 = sceneBoundsObservable.Select(p => p.Top - from.Y).ToProperty(this, p => p.Y1);
            x2 = sceneBoundsObservable.Select(p => to.X - p.Left).ToProperty(this, p => p.X2);
            y2 = sceneBoundsObservable.Select(p => p.Top - to.Y).ToProperty(this, p => p.Y2);
            Thickness = thickness;
            Color = color;
        }
    }

    internal class PlayerViewModel : ReactiveObject
    {
        public PlayerId Id { get; }
        [Reactive]
        public ImageSource Image { get; set; }
        [Reactive]
        public Size Size { get; set; } = new Size(0, 0);
        [Reactive]
        public Position Position { get; set; } = new Position(0, 0);
        [Reactive]
        public double Angle { get; set; }
        [Reactive]
        public int ZIndex { get; set; }
        [Reactive]
        public Visibility Visibility { get; set; }
        [Reactive]
        public SpeechBubbleViewModel SpeechBubble { get; set; }
        private readonly ObservableAsPropertyHelper<Visibility> speechBubbleVisibility;
        public Visibility SpeechBubbleVisibility => speechBubbleVisibility.Value;
        private readonly ObservableAsPropertyHelper<Position> offset;
        public Position Offset => offset.Value;
        private readonly ObservableAsPropertyHelper<double> rotation;
        public double Rotation => rotation.Value;
        private readonly ObservableAsPropertyHelper<string> infoText;
        public string InfoText => infoText.Value;

        public PlayerViewModel(IObservable<Rectangle> sceneBoundsObservable, PlayerId playerId)
        {
            Id = playerId;
            offset = Observable
                .CombineLatest(
                    sceneBoundsObservable,
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
            speechBubbleVisibility = this.WhenAnyValue(p => p.SpeechBubble)
                .Select(p => p != null ? Visibility.Visible : Visibility.Collapsed)
                .ToProperty(this, p => p.SpeechBubbleVisibility);
            this.WhenAnyValue(p => p.SpeechBubble)
                .Where(p => p != null)
                .Select(speechBubble =>
                    Observable
                        .CombineLatest(
                            sceneBoundsObservable,
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

    internal abstract class SpeechBubbleViewModel : ReactiveObject
    {
        [Reactive]
        public string Text { get; set; }
        [Reactive]
        public SpeechBubblePosition Position { get; set; } = SpeechBubblePosition.Right;
        private readonly ObservableAsPropertyHelper<double> scaleX;
        public double ScaleX => scaleX.Value;
        [Reactive]
        public Position Offset { get; set; }
        [Reactive]
        public Size Size { get; set; } = new Size(0, 0);
        private readonly ObservableAsPropertyHelper<string> geometry;
        public string Geometry => geometry.Value;
        public SpeechBubbleViewModel()
        {
            geometry = this.WhenAnyValue(p => p.Size)
                .Select(size =>
                {
                    double bubbleWidth = size.Width - 2 * 10;
                    double bubbleHeight = size.Height - 2 * 5 - 15;
                    return $"M 10,5 h {bubbleWidth} c 10,0 10,{bubbleHeight} 0,{bubbleHeight} h -{bubbleWidth - 40} c 0,7 -5,13 -15,15 s 3,-6 0,-15 h -25 c -10,0 -10,-{bubbleHeight} 0,-{bubbleHeight}";
                })
                .ToProperty(this, p => p.Geometry);
            scaleX = this.WhenAnyValue(p => p.Position)
                .Select(p => p == SpeechBubblePosition.Right ? 1.0 : -1.0)
                .ToProperty(this, p => p.ScaleX);
        }
    }

    internal class SaySpeechBubbleViewModel : SpeechBubbleViewModel
    {
    }

    internal class AskBoolSpeechBubbleViewModel : SpeechBubbleViewModel
    {
        public ReactiveCommand<bool, bool> ConfirmCommand { get; }

        public AskBoolSpeechBubbleViewModel()
        {
            ConfirmCommand = ReactiveCommand.Create<bool, bool>(answer => answer);
        }
    }

    internal class AskTextSpeechBubbleViewModel : SpeechBubbleViewModel
    {
        [Reactive]
        public string Answer { get; set; }
        public ReactiveCommand<Unit, string> ConfirmCommand { get; }

        public AskTextSpeechBubbleViewModel()
        {
            ConfirmCommand = ReactiveCommand.Create(() => Answer);
        }
    }
}
