namespace GetIt.Windows

open System
open System.Runtime.InteropServices

module Win32 =
    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type WinPoint =
        val mutable x: int
        val mutable y: int

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type WinMsg =
        val mutable hwnd: IntPtr
        val mutable message: uint32
        val mutable wParam: IntPtr
        val mutable lParam: IntPtr
        val mutable time: uint32
        val mutable pt: WinPoint

    [<DllImport("user32.dll")>]
    extern bool PeekMessage([<Out>] WinMsg& lpMsg, IntPtr hWnd, uint32 wMsgFilterMin, uint32 wMsgFilterMax, uint32 wRemoveMsg)

    [<DllImport("user32.dll")>]
    extern int GetMessage(WinMsg& lpMsg, IntPtr hWnd, uint32 wMsgFilterMin, uint32 wMsgFilterMax)

    [<DllImport("user32.dll")>]
    extern bool TranslateMessage([<In>] WinMsg& lpMsg)

    [<DllImport("user32.dll")>]
    extern IntPtr DispatchMessage([<In>] WinMsg& lpmsg)

    [<DllImport("user32.dll")>]
    extern IntPtr DefWindowProc(IntPtr hWnd, uint32 uMsg, IntPtr wParam, IntPtr lParam)

    [<DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)>]
    extern bool SendMessage(IntPtr hWnd, uint32 Msg, UIntPtr wParam, IntPtr lParam)

    [<DllImport("user32.dll")>]
    extern void PostQuitMessage(int nExitCode)

    [<DllImport("kernel32.dll", CharSet = CharSet.Auto)>]
    extern IntPtr GetModuleHandle(string lpModuleName)

    [<DllImport("user32.dll", SetLastError = true)>]
    extern bool GetCursorPos(WinPoint& lpPoint)

    [<DllImport("user32.dll")>]
    extern bool ScreenToClient(IntPtr hWnd, WinPoint& lpPoint)

    [<DllImport("user32.dll", SetLastError=true)>]
    extern IntPtr CreateWindowEx(
        uint32 dwExStyle,
        uint16 classAtom,
        [<MarshalAs(UnmanagedType.LPStr)>] string lpWindowName,
        uint32 dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam)

    [<DllImport("user32.dll", SetLastError=true)>]
    extern bool DestroyWindow(IntPtr hwnd)

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type WNDCLASSEX =
        val mutable cbSize: int
        val mutable style: int
        val mutable lpfnWndProc: IntPtr
        val mutable cbClsExtra: int
        val mutable cbWndExtra: int
        val mutable hInstance: IntPtr
        val mutable hIcon: IntPtr
        val mutable hCursor: IntPtr
        val mutable hbrBackground: IntPtr
        val mutable lpszMenuName: string
        val mutable lpszClassName: string
        val mutable hIconSm: IntPtr

    [<DllImport("user32.dll")>]
    extern uint16 RegisterClassEx(WNDCLASSEX& lpwcx)

    [<DllImport("user32.dll")>]
    extern bool UnregisterClass(uint16 classAtom, IntPtr hInstance)

    [<DllImport("user32.dll")>]
    extern bool SetForegroundWindow(IntPtr hWnd)

    type ShowWindowCommand =
        | SW_HIDE = 0
        | SW_SHOWNORMAL = 1
        | SW_SHOWMINIMIZED = 2
        | SW_SHOWMAXIMIZED = 3
        | SW_SHOWNOACTIVATE = 4
        | SW_SHOW = 5
        | SW_MINIMIZE = 6
        | SW_SHOWMINNOACTIVE = 7
        | SW_SHOWNA = 8
        | SW_RESTORE = 9
        | SW_SHOWDEFAULT = 10
        | SW_FORCEMINIMIZE = 11

    [<DllImport("user32.dll")>]
    extern bool ShowWindow(IntPtr hWnd, ShowWindowCommand nCmdShow)

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type Rect =
        val mutable Left: int
        val mutable Top: int
        val mutable Right: int
        val mutable Bottom: int

    [<DllImport("user32.dll", SetLastError=true)>]
    extern bool GetWindowRect(IntPtr hWnd, Rect& rect)

    type DWMWINDOWATTRIBUTE =
        | NCRenderingEnabled = 1u
        | NCRenderingPolicy = 2u
        | TransitionsForceDisabled = 3u
        | AllowNCPaint = 4u
        | CaptionButtonBounds = 5u
        | NonClientRtlLayout = 6u
        | ForceIconicRepresentation = 7u
        | Flip3DPolicy = 8u
        | ExtendedFrameBounds = 9u
        | HasIconicBitmap = 10u
        | DisallowPeek = 11u
        | ExcludedFromPeek = 12u
        | Cloak = 13u
        | Cloaked = 14u
        | FreezeRepresentation = 15u

    [<DllImport("dwmapi.dll")>]
    extern int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, Rect& pvAttribute, int cbAttribute)

    [<DllImport("user32.dll", SetLastError=true)>]
    extern bool GetClientRect(IntPtr hWnd, Rect& rect)

    [<DllImport("user32.dll", SetLastError=true)>]
    extern bool ClientToScreen(IntPtr hWnd, WinPoint& point)
