using Avalonia.Input;

namespace PlayAndLearn.Models
{
    public enum KeyboardKey
    {
        Space,
        Up, Down, Left, Right,
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Digit0, Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9
    }

    public static class KeyboardKeyExtensions
    {
        public static Key ToAvaloniaKey(this KeyboardKey key)
        {
            switch (key)
            {
                case KeyboardKey.Space: return Key.Space;
                case KeyboardKey.Up: return Key.Up;
                case KeyboardKey.Down: return Key.Down;
                case KeyboardKey.Left: return Key.Left;
                case KeyboardKey.Right: return Key.Right;
                case KeyboardKey.A: return Key.A;
                case KeyboardKey.B: return Key.B;
                case KeyboardKey.C: return Key.C;
                case KeyboardKey.D: return Key.D;
                case KeyboardKey.E: return Key.E;
                case KeyboardKey.F: return Key.F;
                case KeyboardKey.G: return Key.G;
                case KeyboardKey.H: return Key.H;
                case KeyboardKey.I: return Key.I;
                case KeyboardKey.J: return Key.J;
                case KeyboardKey.K: return Key.K;
                case KeyboardKey.L: return Key.L;
                case KeyboardKey.M: return Key.M;
                case KeyboardKey.N: return Key.N;
                case KeyboardKey.O: return Key.O;
                case KeyboardKey.P: return Key.P;
                case KeyboardKey.Q: return Key.Q;
                case KeyboardKey.R: return Key.R;
                case KeyboardKey.S: return Key.S;
                case KeyboardKey.T: return Key.T;
                case KeyboardKey.U: return Key.U;
                case KeyboardKey.V: return Key.V;
                case KeyboardKey.W: return Key.W;
                case KeyboardKey.X: return Key.X;
                case KeyboardKey.Y: return Key.Y;
                case KeyboardKey.Z: return Key.Z;
                case KeyboardKey.Digit0: return Key.D0;
                case KeyboardKey.Digit1: return Key.D1;
                case KeyboardKey.Digit2: return Key.D2;
                case KeyboardKey.Digit3: return Key.D3;
                case KeyboardKey.Digit4: return Key.D4;
                case KeyboardKey.Digit5: return Key.D5;
                case KeyboardKey.Digit6: return Key.D6;
                case KeyboardKey.Digit7: return Key.D7;
                case KeyboardKey.Digit8: return Key.D8;
                case KeyboardKey.Digit9: return Key.D9;
                default: return Key.None;
            }
        }
    }
}