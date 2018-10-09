using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using PlayAndLearn.Models;
using Portable.Xaml;

namespace PlayAndLearn
{
    public static class Turtle
    {
        private static string AssemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static readonly Player Default = CreateDefault();

        public static Player CreateDefault() => new Player(
            new Size(50, 50),
            new Position(0, 0),
            new Degrees(0),
            new Pen(false, 1, new RGB(0x00, 0x00, 0x00)),
            SpeechBubble.Empty,
            () => Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream("PlayAndLearn.Models.Turtle.default.png")
        );

        public static void GoTo(double x, double y) => Default.GoTo(x, y);
        public static void GoToCenter() => Default.GoToCenter();
        public static void Move(double x, double y) => Default.Move(x, y);
        public static void MoveRight(int steps) => Default.MoveRight(steps);
        public static void MoveLeft(int steps) => Default.MoveLeft(steps);
        public static void MoveUp(int steps) => Default.MoveUp(steps);
        public static void MoveDown(int steps) => Default.MoveDown(steps);
        public static void Go(int steps) => Default.Go(steps);
        public static void SetDirection(double angleInDegrees) => Default.SetDirection(angleInDegrees);
        public static void RotateClockwise(double angleInDegrees) => Default.RotateClockwise(angleInDegrees);
        public static void RotateCounterClockwise(double angleInDegrees) => Default.RotateCounterClockwise(angleInDegrees);
        public static void TurnUp() => Default.TurnUp();
        public static void TurnRight() => Default.TurnRight();
        public static void TurnDown() => Default.TurnDown();
        public static void TurnLeft() => Default.TurnLeft();
        public static void Say(string text) => Default.Say(text);
        public static void Say(string text, double durationInSeconds) => Default.Say(text, durationInSeconds);
        public static void ShutUp() => Default.ShutUp();
        public static void TurnOnPen() => Default.TurnOnPen();
        public static void TurnOffPen() => Default.TurnOffPen();
        public static void TogglePenOnOff() => Default.TogglePenOnOff();
        public static void SetPenColor(RGB color) => Default.SetPenColor(color);
        public static void ShiftPenColor(double shift) => Default.ShiftPenColor(shift);
        public static void SetPenWeight(double weight) => Default.SetPenWeight(weight);
        public static void ChangePenWeight(double change) => Default.ChangePenWeight(change);
        public static IDisposable OnKeyDown(KeyboardKey key, Action<Player> action) => Default.OnKeyDown(key, action);
        public static IDisposable OnMouseEnter(Action<Player> action) => Default.OnMouseEnter(action);
        public static IDisposable OnClick(Action<Player> action) => Default.OnClick(action);
    }
}