using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Input;
using Avalonia.Interactivity;
using PlayAndLearn.Models;
using PlayAndLearn.Utils;

namespace PlayAndLearn
{
    public static class PlayerExtensions
    {
        public static void GoTo(this Player player, double x, double y)
        {
            player.Position = new Position(x, y);
        }
        public static void GoToCenter(this Player player) => player.GoTo(0, 0);
        public static void Move(this Player player, double x, double y)
        {
            player.GoTo(player.Position.X + x, player.Position.Y + y);
        }

        public static void MoveRight(this Player player, int steps) => player.Move(steps, 0);

        public static void MoveLeft(this Player player, int steps) => player.Move(-steps, 0);

        public static void MoveUp(this Player player, int steps) => player.Move(0, steps);

        public static void MoveDown(this Player player, int steps) => player.Move(0, -steps);

        public static void Go(this Player player, int steps)
        {
            var directionRadians = player.Direction.Value / 180 * Math.PI;
            player.Move(
                Math.Cos(directionRadians) * steps,
                Math.Sin(directionRadians) * steps
            );
        }

        public static void SetDirection(this Player player, Degrees angle)
        {
            player.Direction = angle;
        }

        public static void RotateClockwise(this Player player, Degrees angle)
        {
            player.SetDirection(player.Direction - angle);
        }

        public static void RotateCounterClockwise(this Player player, Degrees angle)
        {
            player.SetDirection(player.Direction + angle);
        }

        public static void TurnUp(this Player player) => player.Direction = 90;

        public static void TurnRight(this Player player) => player.Direction = 0;

        public static void TurnDown(this Player player) => player.Direction = 270;

        public static void TurnLeft(this Player player) => player.Direction = 180;

        public static void Say(this Player player, string text) =>
            player.SpeechBubble = new SpeechBubble(text);

        public static void Say(this Player player, string text, double durationInSeconds)
        {
            player.SpeechBubble = new SpeechBubble(text);
            Game.Sleep(TimeSpan.FromSeconds(durationInSeconds).TotalMilliseconds);
            player.SpeechBubble = SpeechBubble.Empty;
        }

        public static void ShutUp(this Player player) =>
            player.SpeechBubble = SpeechBubble.Empty;

        public static void TurnOnPen(this Player player) => player.Pen = player.Pen.WithIsOn(true);

        public static void TurnOffPen(this Player player) => player.Pen = player.Pen.WithIsOn(false);

        public static void TogglePenOnOff(this Player player) => player.Pen = player.Pen.WithIsOn(!player.Pen.IsOn);

        public static void SetPenColor(this Player player, RGB color) => player.Pen = player.Pen.WithColor(color);

        public static void ShiftPenColor(this Player player, double shift) => player.Pen = player.Pen.WithHueShift(shift);

        public static void SetPenWeight(this Player player, double weight) => player.Pen = player.Pen.WithWeight(weight);

        public static void ChangePenWeight(this Player player, double change) => player.SetPenWeight(player.Pen.Weight + change);

        public static Degrees GetDirectionToMouse(this Player player) => player.Position.AngleTo(Game.MousePosition);

        public static double GetDistanceToMouse(this Player player) => player.Position.DistanceTo(Game.MousePosition);

        public static IDisposable OnKeyDown(this Player player, KeyboardKey key, Action<Player> action)
        {
            return Observable
                .Create<KeyEventArgs>(observer =>
                    Game.MainWindow.AddHandler(
                        InputElement.KeyDownEvent,
                        new EventHandler<KeyEventArgs>((s, e) => observer.OnNext(e)),
                        handledEventsToo: true)
                )
                .Where(eventArgs => eventArgs.Key == key.ToAvaloniaKey())
                .Subscribe(_ => action(player));
        }

        private static IObservable<TEventArgs> Observe<TEventArgs>(this Player player, RoutedEvent<TEventArgs> routedEvent)
            where TEventArgs : RoutedEventArgs
        {
            var control = Game.TryFindPlayerControl(player);
            if (control == null)
            {
                return Observable.Empty<TEventArgs>();
            }
            return Observable
                .Create<TEventArgs>(observer =>
                    control.AddHandler(
                        routedEvent,
                        new EventHandler<TEventArgs>((s, e) => observer.OnNext(e)),
                        handledEventsToo: true)
                )
                .ObserveOn(System.Reactive.Concurrency.TaskPoolScheduler.Default);
        }

        public static IDisposable OnMouseEnter(this Player player, Action<Player> action)
        {
            return player
                .Observe(InputElement.PointerEnterEvent)
                .Subscribe(_ => action(player));
        }

        public static IDisposable OnClick(this Player player, Action<Player> action)
        {
            return player
                .Observe(InputElement.TappedEvent)
                .Subscribe(_ => action(player));
        }
    }
}