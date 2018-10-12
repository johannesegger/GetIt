using System;
using System.Reactive.Disposables;
using Elmish.Net;
using PlayAndLearn.Models;
using PlayAndLearn.Utils;

namespace PlayAndLearn
{
    public static class PlayerExtensions
    {
        public static void GoTo(this PlayerOnScene player, double x, double y)
        {
            Game.DispatchMessageAndWaitForUpdate(new Message.SetPosition(player.Id, new Position(x, y)));
        }
        public static void GoToCenter(this PlayerOnScene player) => player.GoTo(0, 0);
        public static void Move(this PlayerOnScene player, double x, double y)
        {
            player.GoTo(player.Position.X + x, player.Position.Y + y);
        }

        public static void MoveRight(this PlayerOnScene player, int steps) => player.Move(steps, 0);

        public static void MoveLeft(this PlayerOnScene player, int steps) => player.Move(-steps, 0);

        public static void MoveUp(this PlayerOnScene player, int steps) => player.Move(0, steps);

        public static void MoveDown(this PlayerOnScene player, int steps) => player.Move(0, -steps);

        public static void Go(this PlayerOnScene player, int steps)
        {
            var directionRadians = player.Direction.Value / 180 * Math.PI;
            player.Move(
                Math.Cos(directionRadians) * steps,
                Math.Sin(directionRadians) * steps
            );
        }

        private static Random rand = new Random();
        public static void GoToRandomPosition(this PlayerOnScene player)
        {
            var x = rand.Next((int)Game.State.SceneBounds.Left, (int)Game.State.SceneBounds.Right);
            var y = rand.Next((int)Game.State.SceneBounds.Bottom, (int)Game.State.SceneBounds.Top);
            player.GoTo(x, y);
        }

        public static void SetDirection(this PlayerOnScene player, Degrees angle)
        {
            Game.DispatchMessageAndWaitForUpdate(new Message.SetDirection(player.Id, angle));
        }

        public static void RotateClockwise(this PlayerOnScene player, Degrees angle)
        {
            player.SetDirection(player.Direction - angle);
        }

        public static void RotateCounterClockwise(this PlayerOnScene player, Degrees angle)
        {
            player.SetDirection(player.Direction + angle);
        }

        public static void TurnUp(this PlayerOnScene player) => player.SetDirection(90);

        public static void TurnRight(this PlayerOnScene player) => player.SetDirection(0);

        public static void TurnDown(this PlayerOnScene player) => player.SetDirection(270);

        public static void TurnLeft(this PlayerOnScene player) => player.SetDirection(180);

        public static void Say(this PlayerOnScene player, string text)
        {
            Game.DispatchMessageAndWaitForUpdate(new Message.Say(player.Id, new SpeechBubble(text)));
        }

        public static void ShutUp(this PlayerOnScene player) =>
            Game.DispatchMessageAndWaitForUpdate(new Message.Say(player.Id, SpeechBubble.Empty));

        public static void Say(this PlayerOnScene player, string text, double durationInSeconds)
        {
            player.Say(text);
            Game.Sleep(TimeSpan.FromSeconds(durationInSeconds).TotalMilliseconds);
            player.ShutUp();
        }

        public static void SetPen(this PlayerOnScene player, Pen pen)
        {
            Game.DispatchMessageAndWaitForUpdate(new Message.SetPen(player.Id, pen));
        }

        public static void TurnOnPen(this PlayerOnScene player) => player.SetPen(player.Pen.With(p => p.IsOn, true));

        public static void TurnOffPen(this PlayerOnScene player) => player.SetPen(player.Pen.With(p => p.IsOn, false));

        public static void TogglePenOnOff(this PlayerOnScene player) => player.SetPen(player.Pen.With(p => p.IsOn, !player.Pen.IsOn));

        public static void SetPenColor(this PlayerOnScene player, RGB color) => player.SetPen(player.Pen.With(p => p.Color, color));

        public static void ShiftPenColor(this PlayerOnScene player, double shift) => player.SetPen(player.Pen.WithHueShift(shift));

        public static void SetPenWeight(this PlayerOnScene player, double weight) => player.SetPen(player.Pen.With(p => p.Weight, weight));

        public static void SetSizeFactor(this PlayerOnScene player, double sizeFactor)
        {
            Game.DispatchMessageAndWaitForUpdate(new Message.SetSizeFactor(player.Id, sizeFactor));
        }

        public static void ChangeSizeFactor(this PlayerOnScene player, double change) => player.SetSizeFactor(player.SizeFactor + change);

        public static void ChangePenWeight(this PlayerOnScene player, double change) => player.SetPenWeight(player.Pen.Weight + change);

        public static Degrees GetDirectionToMouse(this PlayerOnScene player) => player.Position.AngleTo(Game.State.MousePosition);

        public static double GetDistanceToMouse(this PlayerOnScene player) => player.Position.DistanceTo(Game.State.MousePosition);

        public static IDisposable OnKeyDown(this PlayerOnScene player, KeyboardKey key, Action<PlayerOnScene> action)
        {
            var handler = new KeyDownHandler(Guid.NewGuid(), key, () => action(player));
            Game.DispatchMessageAndWaitForUpdate(new Message.AddKeyDownHandler(handler));
            return Disposable.Create(() => Game.DispatchMessageAndWaitForUpdate(new Message.RemoveKeyDownHandler(handler)));
        }

        public static IDisposable OnMouseEnter(this PlayerOnScene player, Action<PlayerOnScene> action)
        {
            var handler = new MouseEnterPlayerHandler(Guid.NewGuid(), player.Id, () => action(player));
            Game.DispatchMessageAndWaitForUpdate(new Message.AddMouseEnterPlayerHandler(handler));
            return Disposable.Create(() => Game.DispatchMessageAndWaitForUpdate(new Message.RemoveMouseEnterPlayerHandler(handler)));
        }

        public static IDisposable OnClick(this PlayerOnScene player, Action<PlayerOnScene> action)
        {
            var handler = new ClickPlayerHandler(Guid.NewGuid(), player.Id, () => action(player));
            Game.DispatchMessageAndWaitForUpdate(new Message.AddClickPlayerHandler(handler));
            return Disposable.Create(() => Game.DispatchMessageAndWaitForUpdate(new Message.RemoveClickPlayerHandler(handler)));
        }
    }
}