using System;
using System.Runtime.InteropServices;

namespace GetIt.Windows
{
    // https://www.michaelwda.com/net-core-message-pump/
    public struct WinMsg
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public WinPoint pt;
    }

    public struct WinPoint
    {
        public Int32 x;
        public Int32 Y;
    }

    public static class WinNative
    {
        [DllImport("user32.dll")]
        public static extern int GetMessage(out WinMsg lpMsg, IntPtr hWnd, uint wMsgFilterMin,
            uint wMsgFilterMax);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage([In] ref WinMsg lpMsg);

        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage([In] ref WinMsg lpmsg);
    }
}
