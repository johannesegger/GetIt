using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Logging.Serilog;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PlayAndLearn.Models;
using PlayAndLearn.Utils;

namespace PlayAndLearn
{
    public static class Game
    {
        private static MainWindow mainWindow;

        public static void ShowScene()
        {
            using (var signal = new ManualResetEventSlim())
            {
                var uiThread = new Thread(() =>
                {
                    var builder = AppBuilder
                        .Configure<App>()
                        .UsePlatformDetect()
                        .LogToDebug()
                        .SetupWithoutStarting();
                    mainWindow = new MainWindow();
                    mainWindow.Show();
                    signal.Set();
                    builder.Instance.Run(mainWindow);
                });
                uiThread.IsBackground = false;
                uiThread.Start();
                signal.Wait();
            }
        }

        public static IDisposable AddSprite(Player sprite)
        {
            var addedSprite = new SingleAssignmentDisposable();
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var spriteControl = new Image { ZIndex = 10 };
                mainWindow.Scene.Children.Add(spriteControl);
                var d = new CompositeDisposable();

                var center = new Position(
                    mainWindow.Scene.Bounds.Width / 2 - sprite.Size.Width / 2,
                    mainWindow.Scene.Bounds.Height / 2 - sprite.Size.Height / 2
                );

                sprite
                    .IdleCostume
                    .ObserveOn(AvaloniaScheduler.Instance)
                    .Subscribe(costumeStreamFactory =>
                    {
                        using (var costume = costumeStreamFactory())
                        {
                            spriteControl.Source = new Bitmap(costume);
                        }
                        spriteControl.Width = sprite.Size.Width;
                        spriteControl.Height = sprite.Size.Height;
                    })
                    .DisposeWith(d);

                Observable
                    .CombineLatest(
                        sprite
                            .Changed(p => p.Position)
                            .Select(p => new Position(p.X + center.X, p.Y + center.Y)),
                        sprite.Changed(p => p.Pen),
                        (position, pen) => (position, pen)
                    )
                    .ObserveOn(AvaloniaScheduler.Instance)
                    .Subscribe(((Position position, Models.Pen pen) p) =>
                    {
                        if (p.pen != null)
                        {
                            var currentPosition = new Position(
                                Canvas.GetLeft(spriteControl),
                                Canvas.GetBottom(spriteControl));
                            var line = new Line
                            {
                                StartPoint = new Point(
                                    currentPosition.X + sprite.Size.Width / 2,
                                    mainWindow.Scene.Bounds.Height - currentPosition.Y - sprite.Size.Height / 2),
                                EndPoint = new Point(
                                    p.position.X + sprite.Size.Width / 2,
                                    mainWindow.Scene.Bounds.Height - p.position.Y - sprite.Size.Height / 2),
                                Stroke = new SolidColorBrush(new Color(0xFF, p.pen.Color.Red, p.pen.Color.Green, p.pen.Color.Blue)),
                                StrokeThickness = p.pen.Weight,
                                ZIndex = 5
                            };
                            mainWindow.Scene.Children.Add(line);
                        }

                        Canvas.SetLeft(spriteControl, p.position.X);
                        Canvas.SetBottom(spriteControl, p.position.Y);
                    })
                    .DisposeWith(d);

                sprite
                    .Changed(p => p.Direction)
                    .ObserveOn(AvaloniaScheduler.Instance)
                    .Subscribe(direction =>
                    {
                        spriteControl.RenderTransform = new RotateTransform(360 - direction);
                    })
                    .DisposeWith(d);

                addedSprite.Disposable = d;

            }).Wait();

            return addedSprite;
        }

        public static void SleepMilliseconds(double value)
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(value));
        }
    }
}
