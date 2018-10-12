using Avalonia.Input;
using LanguageExt;
using static LanguageExt.Prelude;

namespace PlayAndLearn.Models
{
    public static class KeyboardKeyExtensions
    {
        public static Option<KeyboardKey> TryGetKeyboardKey(this Key key)
        {
            switch (key)
            {
                case Key.Space: return KeyboardKey.Space;
                case Key.Up: return KeyboardKey.Up;
                case Key.Down: return KeyboardKey.Down;
                case Key.Left: return KeyboardKey.Left;
                case Key.Right: return KeyboardKey.Right;
                case Key.A: return KeyboardKey.A;
                case Key.B: return KeyboardKey.B;
                case Key.C: return KeyboardKey.C;
                case Key.D: return KeyboardKey.D;
                case Key.E: return KeyboardKey.E;
                case Key.F: return KeyboardKey.F;
                case Key.G: return KeyboardKey.G;
                case Key.H: return KeyboardKey.H;
                case Key.I: return KeyboardKey.I;
                case Key.J: return KeyboardKey.J;
                case Key.K: return KeyboardKey.K;
                case Key.L: return KeyboardKey.L;
                case Key.M: return KeyboardKey.M;
                case Key.N: return KeyboardKey.N;
                case Key.O: return KeyboardKey.O;
                case Key.P: return KeyboardKey.P;
                case Key.Q: return KeyboardKey.Q;
                case Key.R: return KeyboardKey.R;
                case Key.S: return KeyboardKey.S;
                case Key.T: return KeyboardKey.T;
                case Key.U: return KeyboardKey.U;
                case Key.V: return KeyboardKey.V;
                case Key.W: return KeyboardKey.W;
                case Key.X: return KeyboardKey.X;
                case Key.Y: return KeyboardKey.Y;
                case Key.Z: return KeyboardKey.Z;
                case Key.D0: return KeyboardKey.Digit0;
                case Key.D1: return KeyboardKey.Digit1;
                case Key.D2: return KeyboardKey.Digit2;
                case Key.D3: return KeyboardKey.Digit3;
                case Key.D4: return KeyboardKey.Digit4;
                case Key.D5: return KeyboardKey.Digit5;
                case Key.D6: return KeyboardKey.Digit6;
                case Key.D7: return KeyboardKey.Digit7;
                case Key.D8: return KeyboardKey.Digit8;
                case Key.D9: return KeyboardKey.Digit9;
                default: return None;
            }
        }
    }
}