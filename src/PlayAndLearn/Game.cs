using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
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
                var spriteControl = new Image();
                mainWindow.Scene.Children.Add(spriteControl);
                var d = new CompositeDisposable();

                sprite
                    .IdleCostume
                    .ObserveOn(AvaloniaScheduler.Instance)
                    .Subscribe(path =>
                    {
                        spriteControl.Source = new Bitmap(path);
                        spriteControl.Width = sprite.Size.Width;
                        spriteControl.Height = sprite.Size.Height;
                    })
                    .DisposeWith(d);

                sprite
                    .Changed(p => p.Position)
                    .ObserveOn(AvaloniaScheduler.Instance)
                    .Subscribe(position =>
                    {
                        var center = new Position(
                            (int)(mainWindow.Scene.Bounds.Width / 2 - sprite.Size.Width / 2),
                            (int)(mainWindow.Scene.Bounds.Height / 2 - sprite.Size.Height / 2));
                        Canvas.SetLeft(spriteControl, center.X + position.X);
                        Canvas.SetBottom(spriteControl, center.Y + position.Y);
                    })
                    .DisposeWith(d);

                addedSprite.Disposable = d;

            }).Wait();

            return addedSprite;
        }

        public static void SleepSeconds(double seconds)
        {
            Thread.Sleep(TimeSpan.FromSeconds(seconds));
        }
    }
}
