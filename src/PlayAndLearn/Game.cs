using System;
using System.Reactive.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging.Serilog;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PlayAndLearn.Models;

namespace PlayAndLearn
{
    public static class Game
    {
        private static MainWindow mainWindow;

        private static void ShowScene()
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

        public static void ShowDefaultScene()
        {
            ShowScene();

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var knightControl = new Image();
                mainWindow.Scene.Children.Add(knightControl);
                Canvas.SetLeft(knightControl, mainWindow.Scene.Bounds.Width / 2 - knightControl.Width / 2);
                Canvas.SetTop(knightControl, mainWindow.Scene.Bounds.Height / 2 - knightControl.Height / 2);
                var knight = Knight.Create();
                var d = knight.Costume
                    .ObserveOn(AvaloniaScheduler.Instance)
                    .Subscribe(path =>
                    {
                        knightControl.Source = new Bitmap(path);
                        knightControl.Width = knightControl.Source.PixelWidth;
                        knightControl.Height = knightControl.Source.PixelHeight;
                        knightControl.RenderTransform = new ScaleTransform(0.1, 0.1);
                    });

            }).Wait();
        }
    }
}
