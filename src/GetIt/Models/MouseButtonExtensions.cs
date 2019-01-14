using System;

namespace GetIt
{
    internal static class MouseButtonExtensions
    {
        public static MouseButton ToMouseButton(this Avalonia.Input.MouseButton mouseButton)
        {
            switch (mouseButton)
            {
                case Avalonia.Input.MouseButton.Left: return MouseButton.Left;
                case Avalonia.Input.MouseButton.Middle: return MouseButton.Middle;
                case Avalonia.Input.MouseButton.Right: return MouseButton.Right;
                default: throw new ArgumentException($"Can't convert to {typeof(MouseButton)} from {mouseButton}.");
            }
        }
    }
}