using System;
using System.Reactive.Disposables;
using System.Threading;
using Elmish.Net;
using GetIt.Internal;
using LanguageExt;
using static LanguageExt.Prelude;

namespace GetIt
{
    [CodeGeneration.Staticify("Turtle", "Default")]
    public static class PlayerExtensions
    {
        /// <summary>
        /// Moves the player to a position.
        /// </summary>
        /// <param name="player">The player that should be moved.</param>
        /// <param name="position">The absolute destination position.</param>
        public static void MoveTo(this PlayerOnScene player, Position position)
        {
            Game.DispatchMessageAndWaitForUpdate(new Message.SetPosition(player.Id, position));
        }

        /// <summary>
        /// Moves the player to a position.
        /// </summary>
        /// <param name="player">The player that should be moved.</param>
        /// <param name="x">The absolute x coordinate of the destination position.</param>
        /// <param name="y">The absolute y coordinate of the destination position.</param>
        public static void MoveTo(this PlayerOnScene player, double x, double y)
        {
            player.MoveTo(new Position(x, y));
        }

        /// <summary>
        /// Moves the player to the center of the screen.
        /// </summary>
        /// <param name="player">The player that should be moved.</param>
        public static void MoveToCenter(this PlayerOnScene player) => player.MoveTo(Position.Zero);

        /// <summary>
        /// Moves the player by a position.
        /// </summary>
        /// <param name="player">The player that should be moved.</param>
        /// <param name="deltaX">The change of the x coordinate.</param>
        /// <param name="deltaY">The change of the y coordinate.</param>
        public static void MoveBy(this PlayerOnScene player, double deltaX, double deltaY)
        {
            player.MoveTo(player.Position.X + deltaX, player.Position.Y + deltaY);
        }

        /// <summary>
        /// Moves the player horizontally.
        /// </summary>
        /// <param name="player">The player that should be moved.</param>
        /// <param name="steps">The number of steps.</param>
        public static void MoveRight(this PlayerOnScene player, double steps) => player.MoveBy(steps, 0);

        /// <summary>
        /// Moves the player horizontally.
        /// </summary>
        /// <param name="player">The player that should be moved.</param>
        /// <param name="steps">The number of steps.</param>
        public static void MoveLeft(this PlayerOnScene player, double steps) => player.MoveBy(-steps, 0);

        /// <summary>
        /// Moves the player vertically.
        /// </summary>
        /// <param name="player">The player that should be moved.</param>
        /// <param name="steps">The number of steps.</param>
        public static void MoveUp(this PlayerOnScene player, double steps) => player.MoveBy(0, steps);

        /// <summary>
        /// Moves the player vertically.
        /// </summary>
        /// <param name="player">The player that should be moved.</param>
        /// <param name="steps">The number of steps.</param>
        public static void MoveDown(this PlayerOnScene player, double steps) => player.MoveBy(0, -steps);

        /// <summary>
        /// Moves the player forward.
        /// </summary>
        /// <param name="player">The player that should be moved.</param>
        /// <param name="steps">The number of steps.</param>
        public static void MoveInDirection(this PlayerOnScene player, double steps)
        {
            var directionRadians = player.Direction.Value / 180 * Math.PI;
            player.MoveBy(
                Math.Cos(directionRadians) * steps,
                Math.Sin(directionRadians) * steps
            );
        }

        private static Random rand = new Random();
        /// <summary>
        /// Moves the player to a random position on the scene.
        /// </summary>
        /// <param name="player">The player that should be moved.</param>
        public static void MoveToRandomPosition(this PlayerOnScene player)
        {
            var x = rand.Next((int)Game.State.SceneBounds.Left, (int)Game.State.SceneBounds.Right);
            var y = rand.Next((int)Game.State.SceneBounds.Bottom, (int)Game.State.SceneBounds.Top);
            player.MoveTo(x, y);
        }

        /// <summary>
        /// Sets the rotation of the player to a specific angle.
        /// </summary>
        /// <param name="player">The player that should be moved.</param>
        /// <param name="angle">The absolute angle.</param>
        public static void SetDirection(this PlayerOnScene player, Degrees angle)
        {
            Game.DispatchMessageAndWaitForUpdate(new Message.SetDirection(player.Id, angle));
        }

        /// <summary>
        /// Rotates the player clockwise by a specific angle.
        /// </summary>
        /// <param name="player">The player that should be moved.</param>
        /// <param name="angle">The relative angle.</param>
        public static void RotateClockwise(this PlayerOnScene player, Degrees angle)
        {
            player.SetDirection(player.Direction - angle);
        }

        /// <summary>
        /// Rotates the player counter-clockwise by a specific angle.
        /// </summary>
        /// <param name="player">The player that should be moved.</param>
        /// <param name="angle">The relative angle.</param>
        public static void RotateCounterClockwise(this PlayerOnScene player, Degrees angle)
        {
            player.SetDirection(player.Direction + angle);
        }

        /// <summary>
        /// Rotates the player so that it looks up.
        /// </summary>
        /// <param name="player">The player that should be rotated.</param>
        public static void TurnUp(this PlayerOnScene player) => player.SetDirection(90);

        /// <summary>
        /// Rotates the player so that it looks to the right.
        /// </summary>
        /// <param name="player">The player that should be rotated.</param>
        public static void TurnRight(this PlayerOnScene player) => player.SetDirection(0);

        /// <summary>
        /// Rotates the player so that it looks down.
        /// </summary>
        /// <param name="player">The player that should be rotated.</param>
        public static void TurnDown(this PlayerOnScene player) => player.SetDirection(270);

        /// <summary>
        /// Rotates the player so that it looks to the left.
        /// </summary>
        /// <param name="player">The player that should be rotated.</param>
        public static void TurnLeft(this PlayerOnScene player) => player.SetDirection(180);

        /// <summary>
        /// Bounces the player off the wall if it currently touches it.
        /// </summary>
        /// <param name="player">The player that should bounce off the wall.</param>
        public static void BounceOffWall(this PlayerOnScene player)
        {
            if(player.Bounds.Top > Game.State.SceneBounds.Top
                || player.Bounds.Bottom < Game.State.SceneBounds.Bottom)
            {
                player.SetDirection(360 - player.Direction);
            }
            else if(player.Bounds.Right > Game.State.SceneBounds.Right
                || player.Bounds.Left < Game.State.SceneBounds.Left)
            {
                player.SetDirection(180 - player.Direction);
            }
        }

        /// <summary>
        /// Shows a speech bubble next to the player.
        /// You can remove the speech bubble with <see cref="ShutUp"/>.
        /// </summary>
        /// <param name="player">The player that the speech bubble should belong to.</param>
        /// <param name="text">The content of the speech bubble.</param>
        public static void Say(this PlayerOnScene player, string text)
        {
            Game.DispatchMessageAndWaitForUpdate(new Message.SetSpeechBubble(player.Id, new SpeechBubble.Say(text)));
        }

        /// <summary>
        /// Removes the speech bubble of the player.
        /// </summary>
        /// <param name="player">The player that the speech bubble belongs to.</param>
        public static void ShutUp(this PlayerOnScene player) =>
            Game.DispatchMessageAndWaitForUpdate(new Message.SetSpeechBubble(player.Id, None));

        /// <summary>
        /// Shows a speech bubble next to the player for a specific time.
        /// </summary>
        /// <param name="player">The player that the speech bubble should belong to.</param>
        /// <param name="text">The content of the speech bubble.</param>
        /// <param name="durationInSeconds">The number of seconds how long the speech bubble should be visible.</param>
        public static void Say(this PlayerOnScene player, string text, double durationInSeconds)
        {
            player.Say(text);
            Game.Sleep(TimeSpan.FromSeconds(durationInSeconds).TotalMilliseconds);
            player.ShutUp();
        }

        /// <summary>
        /// Shows a speech bubble with a text box next to the player and waits for the user to fill in the text box.
        /// </summary>
        /// <param name="player">The player that the speech bubble should belong to.</param>
        /// <param name="question">The content of the speech bubble.</param>
        public static string Ask(this PlayerOnScene player, string question)
        {
            using (var signal = new ManualResetEventSlim())
            {
                string answer = null;
                Game.DispatchMessageAndWaitForUpdate(
                    new Message.SetSpeechBubble(
                        player.Id,
                        new SpeechBubble.Ask(question, "", a => { answer = a; signal.Set(); })));
                signal.Wait();
                return answer;
            }
        }

        /// <summary>
        /// Sets the pen of the player.
        /// </summary>
        /// <param name="player">The player that should get the pen.</param>
        /// <param name="pen">The pen that should be assigned to the player.</param>
        public static void SetPen(this PlayerOnScene player, Pen pen)
        {
            Game.DispatchMessageAndWaitForUpdate(new Message.SetPen(player.Id, pen));
        }

        /// <summary>
        /// Turns on the pen of the player.
        /// </summary>
        /// <param name="player">The player that should get its pen turned on.</param>
        public static void TurnOnPen(this PlayerOnScene player) => player.SetPen(player.Pen.With(p => p.IsOn, true));

        /// <summary>
        /// Turns off the pen of the player.
        /// </summary>
        /// <param name="player">The player that should get its pen turned off.</param>
        public static void TurnOffPen(this PlayerOnScene player) => player.SetPen(player.Pen.With(p => p.IsOn, false));

        /// <summary>
        /// Turns on the pen of the player if it is turned off. Turns off the pen of the player if it is turned on.
        /// </summary>
        /// <param name="player">The player that should get its pen toggled.</param>
        public static void TogglePenOnOff(this PlayerOnScene player) => player.SetPen(player.Pen.With(p => p.IsOn, !player.Pen.IsOn));

        /// <summary>
        /// Sets the pen color of the player.
        /// </summary>
        /// <param name="player">The player that should get its pen color set.</param>
        /// <param name="color">The new color of the pen.</param>
        public static void SetPenColor(this PlayerOnScene player, RGBA color) => player.SetPen(player.Pen.With(p => p.Color, color));

        /// <summary>
        /// Shifts the HUE value of the pen color.
        /// </summary>
        /// <param name="player">The player that should get its pen color shifted.</param>
        /// <param name="value">The angle that the HUE value should be shifted by.</param>
        public static void ShiftPenColor(this PlayerOnScene player, Degrees value) => player.SetPen(player.Pen.WithHueShift(value));

        public static void SetPenWeight(this PlayerOnScene player, double weight) => player.SetPen(player.Pen.With(p => p.Weight, weight));

        public static void ChangePenWeight(this PlayerOnScene player, double change) => player.SetPenWeight(player.Pen.Weight + change);

        public static void SetSizeFactor(this PlayerOnScene player, double sizeFactor)
        {
            Game.DispatchMessageAndWaitForUpdate(new Message.SetSizeFactor(player.Id, sizeFactor));
        }

        public static void ChangeSizeFactor(this PlayerOnScene player, double change) => player.SetSizeFactor(player.SizeFactor + change);

        public static void NextCostume(this PlayerOnScene player) => Game.DispatchMessageAndWaitForUpdate(new Message.NextCostume(player.Id));

        public static Degrees GetDirectionToMouse(this PlayerOnScene player) => player.Position.AngleTo(Game.State.Mouse.Position);

        public static double GetDistanceToMouse(this PlayerOnScene player) => player.Position.DistanceTo(Game.State.Mouse.Position);

        private static IDisposable OnKeyDown(this PlayerOnScene player, Option<KeyboardKey> key, Action<KeyboardKey> action)
        {
            var handler = new EventHandler.KeyDown(key, action);
            return Game.AddEventHandler(handler);
        }

        public static IDisposable OnKeyDown(this PlayerOnScene player, KeyboardKey key, Action<PlayerOnScene> action)
        {
            return player.OnKeyDown(Some(key), _ => action(player));
        }

        public static IDisposable OnAnyKeyDown(this PlayerOnScene player, Action<PlayerOnScene, KeyboardKey> action)
        {
            return player.OnKeyDown(None, key => action(player, key));
        }

        public static IDisposable OnMouseEnter(this PlayerOnScene player, Action<PlayerOnScene> action)
        {
            var handler = new EventHandler.MouseEnterPlayer(player.Id, () => action(player));
            return Game.AddEventHandler(handler);
        }

        public static IDisposable OnClick(this PlayerOnScene player, Action<PlayerOnScene> action)
        {
            var handler = new EventHandler.ClickPlayer(player.Id, () => action(player));
            return Game.AddEventHandler(handler);
        }
    }
}