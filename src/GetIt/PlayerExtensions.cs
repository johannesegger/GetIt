using System;
using System.Reactive.Disposables;
using System.Threading;
using Elmish.Net;
using GetIt.Internal;
using LanguageExt;
using static LanguageExt.Prelude;

namespace GetIt
{
    /// <summary>
    /// Defines extension methods for `PlayerOnScene`.
    /// </summary>
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
            var x = rand.Next((int)Game.SceneBounds.Left, (int)Game.SceneBounds.Right);
            var y = rand.Next((int)Game.SceneBounds.Bottom, (int)Game.SceneBounds.Top);
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

        private static bool TouchesTopOrBottomEdge(this PlayerOnScene player)
        {
            return player.Bounds.Top > Game.SceneBounds.Top
                || player.Bounds.Bottom < Game.SceneBounds.Bottom;
        }

        private static bool TouchesLeftOrRightEdge(this PlayerOnScene player)
        {
            return player.Bounds.Right > Game.State.SceneBounds.Right
                || player.Bounds.Left < Game.State.SceneBounds.Left;
        }

        /// <summary>
        /// Checks whether a given player touches an edge of the scene.
        /// </summary>
        /// <param name="player">The player that might touch an edge of the scene.</param>
        /// <returns>True, if the player touches an edge, otherwise false.</returns>
        public static bool TouchesEdge(this PlayerOnScene player)
        {
            return player.TouchesLeftOrRightEdge() || player.TouchesTopOrBottomEdge();
        }

        /// <summary>
        /// Checks whether a given player touches another player.
        /// </summary>
        /// <param name="player">The first player that might be touched.</param>
        /// <param name="other">The second player that might be touched.</param>
        /// <returns>True, if the two players touch each other, otherwise false.</returns>
        public static bool TouchesPlayer(this PlayerOnScene player, PlayerOnScene other)
        {
            var maxLeftX = Math.Max(player.Bounds.Left, other.Bounds.Left);
            var minRightX = Math.Min(player.Bounds.Right, other.Bounds.Right);
            var maxBottomY = Math.Max(player.Bounds.Bottom, other.Bounds.Bottom);
            var minTopY = Math.Min(player.Bounds.Top, other.Bounds.Top);
            return maxLeftX < minRightX && maxBottomY < minTopY;
        }

        /// <summary>
        /// Bounces the player off the wall if it currently touches it.
        /// </summary>
        /// <param name="player">The player that should bounce off the wall.</param>
        public static void BounceOffWall(this PlayerOnScene player)
        {
            if(player.TouchesTopOrBottomEdge())
            {
                player.SetDirection(360 - player.Direction);
            }
            else if(player.TouchesLeftOrRightEdge())
            {
                player.SetDirection(180 - player.Direction);
            }
        }

        /// <summary>
        /// Pauses execution of the player for a given time.
        /// </summary>
        /// <param name="player">The player that pauses execution.</param>
        /// <param name="durationInMilliseconds">The length of the pause in milliseconds.</param>
        public static void Sleep(this PlayerOnScene player, double durationInMilliseconds)
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(durationInMilliseconds));
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
            player.Sleep(TimeSpan.FromSeconds(durationInSeconds).TotalMilliseconds);
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

        /// <summary>
        /// Sets the weight of the pen.
        /// </summary>
        /// <param name="player">The player that gets its pen weight set.</param>
        /// <param name="weight">The new weight of the pen.</param>
        public static void SetPenWeight(this PlayerOnScene player, double weight) => player.SetPen(player.Pen.With(p => p.Weight, weight));

        /// <summary>
        /// Changes the weight of the pen.
        /// </summary>
        /// <param name="player">The player that gets its pen weight changed.</param>
        /// <param name="change">The change of the pen weight.</param>
        public static void ChangePenWeight(this PlayerOnScene player, double change) => player.SetPenWeight(player.Pen.Weight + change);

        /// <summary>
        /// Sets the size of the player by multiplying the original size with a factor.
        /// </summary>
        /// <param name="player">The player that gets its size changed.</param>
        /// <param name="sizeFactor">The factor the original size should be multiplied by.</param>
        public static void SetSizeFactor(this PlayerOnScene player, double sizeFactor)
        {
            Game.DispatchMessageAndWaitForUpdate(new Message.SetSizeFactor(player.Id, sizeFactor));
        }

        /// <summary>
        /// Changes the size factor of the player that the original size is multiplied by.
        /// </summary>
        /// <param name="player">The player that gets its size changed.</param>
        /// <param name="change">The change of the size factor.</param>
        public static void ChangeSizeFactor(this PlayerOnScene player, double change) => player.SetSizeFactor(player.SizeFactor + change);

        /// <summary>
        /// Changes the costume of the player.
        /// </summary>
        /// <param name="player">The player that gets its costume changed.</param>
        public static void NextCostume(this PlayerOnScene player) => Game.DispatchMessageAndWaitForUpdate(new Message.NextCostume(player.Id));

        /// <summary>
        /// Calculates the direction from the player to the mouse pointer.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>The direction from the player to the mouse pointer.</returns>
        public static Degrees GetDirectionToMouse(this PlayerOnScene player) => player.Position.AngleTo(Game.State.Mouse.Position);

        /// <summary>
        /// Calculates the distance from the player to the mouse pointer.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>The distance from the player to the mouse pointer.</returns>
        public static double GetDistanceToMouse(this PlayerOnScene player) => player.Position.DistanceTo(Game.State.Mouse.Position);

        private static IDisposable OnKeyDown(this PlayerOnScene player, Option<KeyboardKey> key, Action<KeyboardKey> action)
        {
            var handler = new EventHandler.KeyDown(key, action);
            return Game.AddEventHandler(handler);
        }

        /// <summary>
        /// Registers an event handler that is called when a specific keyboard key is pressed.
        /// </summary>
        /// <param name="player">The player that gets passed to the event handler.</param>
        /// <param name="key">The keyboard key that should be listened to.</param>
        /// <param name="action">The event handler that should be called.</param>
        /// <returns>The disposable subscription.</returns>
        public static IDisposable OnKeyDown(this PlayerOnScene player, KeyboardKey key, Action<PlayerOnScene> action)
        {
            return player.OnKeyDown(Some(key), _ => action(player));
        }

        /// <summary>
        /// Registers an event handler that is called when any keyboard key is pressed.
        /// </summary>
        /// <param name="player">The player that gets passed to the event handler.</param>
        /// <param name="action">The event handler that should be called.</param>
        /// <returns>The disposable subscription.</returns>
        public static IDisposable OnAnyKeyDown(this PlayerOnScene player, Action<PlayerOnScene, KeyboardKey> action)
        {
            return player.OnKeyDown(None, key => action(player, key));
        }

        /// <summary>
        /// Registers an event handler that is called when the mouse enters the player area.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="action">The event handler that should be called.</param>
        /// <returns>The disposable subscription.</returns>
        public static IDisposable OnMouseEnter(this PlayerOnScene player, Action<PlayerOnScene> action)
        {
            var handler = new EventHandler.MouseEnterPlayer(player.Id, () => action(player));
            return Game.AddEventHandler(handler);
        }

        /// <summary>
        /// Registers an event handler that is called when the mouse is clicked on the player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="action">The event handler that should be called.</param>
        /// <returns>The disposable subscription.</returns>
        public static IDisposable OnClick(this PlayerOnScene player, Action<PlayerOnScene> action)
        {
            var handler = new EventHandler.ClickPlayer(player.Id, () => action(player));
            return Game.AddEventHandler(handler);
        }
    }
}