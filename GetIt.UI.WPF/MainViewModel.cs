using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace GetIt.UI
{
    public class MainViewModel : ReactiveObject
    {
        [Reactive]
        public string Title { get; set; }
        [Reactive]
        public double SceneWidth { get; set; }
        [Reactive]
        public double SceneHeight { get; set; }
        private readonly ObservableAsPropertyHelper<Visibility> infoBarVisibility;
        public Visibility InfoBarVisibility => infoBarVisibility.Value;
        public ObservableCollection<PlayerViewModel> Players { get; } = new ObservableCollection<PlayerViewModel>();
        public ObservableCollection<PenLineViewModel> PenLines { get; } = new ObservableCollection<PenLineViewModel>();
        private readonly ReadOnlyObservableCollection<object> sceneObjects;
        public ReadOnlyObservableCollection<object> SceneObjects => sceneObjects;

        public MainViewModel()
        {
            Players.ToObservableChangeSet().CastToObject()
                .Or(PenLines.ToObservableChangeSet().CastToObject())
                .Bind(out sceneObjects)
                .Subscribe();
            infoBarVisibility = Players
                .ToObservableChangeSet()
                .AsObservableList()
                .CountChanged
                .Select(count => count > 0 ? Visibility.Visible : Visibility.Collapsed)
                .ToProperty(this, p => p.InfoBarVisibility);
        }
    }

    public class PenLineViewModel : ReactiveObject
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
        public Brush Brush { get; }

        public PenLineViewModel(IObservable<Rectangle> sceneBoundsObservable, Position from, Position to, double thickness, Brush brush)
        {
            x1 = sceneBoundsObservable.Select(p => from.X - p.Left).ToProperty(this, p => p.X1);
            y1 = sceneBoundsObservable.Select(p => p.Top - from.Y).ToProperty(this, p => p.Y1);
            x2 = sceneBoundsObservable.Select(p => to.X - p.Left).ToProperty(this, p => p.X2);
            y2 = sceneBoundsObservable.Select(p => p.Top - to.Y).ToProperty(this, p => p.Y2);
            Thickness = thickness;
            Brush = brush;
        }
    }

    public class PlayerViewModel : ReactiveObject
    {
        [Reactive]
        public ImageSource Image { get; set; }
        [Reactive]
        public Size Size { get; set; } = new Size(0, 0);
        [Reactive]
        public Position Position { get; set; } = new Position(0, 0);
        [Reactive]
        public double Angle { get; set; }
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

        public PlayerViewModel(IObservable<Rectangle> sceneBoundsObservable)
        {
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
                            this.WhenAnyValue(p => p.Position),
                            this.WhenAnyValue(p => p.Size),
                            (sceneBounds, speechBubbleSize, position, size) => new Action(() =>
                            {
                                speechBubble.Offset = new Position(
                                    position.X - sceneBounds.Left - size.Width / 2 + size.Width * 0.8,
                                    Math.Max(0, sceneBounds.Top - position.Y - size.Height / 2 - speechBubbleSize.Height));
                            }))
                )
                .Switch()
                .Subscribe(action => action());
        }
    }

    public abstract class SpeechBubbleViewModel : ReactiveObject
    {
        [Reactive]
        public string Text { get; set; }
        [Reactive]
        public double ScaleX { get; set; } = 1;
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
        }
    }

    public class SaySpeechBubbleViewModel : SpeechBubbleViewModel
    {
    }

    public class AskBoolSpeechBubbleViewModel : SpeechBubbleViewModel
    {
        public ICommand TrueCommand { get; }
        public ICommand FalseCommand { get; }

        public AskBoolSpeechBubbleViewModel(Action<bool> answer)
        {
            TrueCommand = ReactiveCommand.Create(() => answer(true));
            FalseCommand = ReactiveCommand.Create(() => answer(false));
        }
    }

    public class AskTextSpeechBubbleViewModel : SpeechBubbleViewModel
    {
        [Reactive]
        public string Answer { get; set; }
        public ICommand ConfirmCommand { get; }

        public AskTextSpeechBubbleViewModel(Action<string> answer)
        {
            ConfirmCommand = ReactiveCommand.Create(() => answer(Answer));
        }
    }
}
