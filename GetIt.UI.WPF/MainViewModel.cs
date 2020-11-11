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
                .Select(count => count == 0 ? Visibility.Hidden : Visibility.Visible)
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
        public double OriginalWidth { get; set; }
        [Reactive]
        public double OriginalHeight { get; set; }
        [Reactive]
        public double X { get; set; }
        [Reactive]
        public double Y { get; set; }
        [Reactive]
        public double ScaleFactor { get; set; }
        [Reactive]
        public double Angle { get; set; }
        [Reactive]
        public SpeechBubbleViewModel SpeechBubble { get; set; }
        private readonly ObservableAsPropertyHelper<double> offsetX;
        public double OffsetX => offsetX.Value;
        private readonly ObservableAsPropertyHelper<double> offsetY;
        public double OffsetY => offsetY.Value;
        private readonly ObservableAsPropertyHelper<double> rotation;
        public double Rotation => rotation.Value;
        private readonly ObservableAsPropertyHelper<string> infoText;
        public string InfoText => infoText.Value;

        public PlayerViewModel(IObservable<Rectangle> sceneBoundsObservable)
        {
            offsetX = Observable
                .CombineLatest(
                    sceneBoundsObservable,
                    this.WhenAnyValue(p => p.X),
                    this.WhenAnyValue(p => p.OriginalWidth),
                    this.WhenAnyValue(p => p.ScaleFactor),
                    (sceneBounds, x, width, scaleFactor) => x - sceneBounds.Left - width * scaleFactor / 2)
                .ToProperty(this, p => p.OffsetX);
            offsetY = Observable
                .CombineLatest(
                    sceneBoundsObservable,
                    this.WhenAnyValue(p => p.Y),
                    this.WhenAnyValue(p => p.OriginalHeight),
                    this.WhenAnyValue(p => p.ScaleFactor),
                    (sceneBounds, y, height, scaleFactor) => sceneBounds.Top - y + height * scaleFactor / 2)
                .ToProperty(this, p => p.OffsetY);
            rotation = this
                .WhenAnyValue(p => p.Angle, v => 360 - v)
                .ToProperty(this, p => p.Rotation);
            infoText = this
                .WhenAnyValue(
                    p => p.X,
                    p => p.Y,
                    p => p.Angle,
                    (x, y, angle) => $"X: {x:F2} | Y: {y:F2} | ↻ {angle:F2}°")
                .ToProperty(this, p => p.InfoText);

        }
    }

    public abstract class SpeechBubbleViewModel : ReactiveObject
    {
        [Reactive]
        public string Text { get; set; }
        [Reactive]
        public double ScaleX { get; set; } = 1;
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
