namespace GetIt.Windows

open System
open System.Runtime.InteropServices

module internal Win32 =
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

    [<DllImport("user32.dll")>]
    extern bool IsWindow(IntPtr hWnd);

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

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type Rect =
        val mutable Left: int
        val mutable Top: int
        val mutable Right: int
        val mutable Bottom: int

    type WindowPlacementFlags =
        | WPF_ASYNCWINDOWPLACEMENT = 0x04u
        | WPF_RESTORETOMAXIMIZED = 0x02u
        | WPF_SETMINPOSITION = 0x01u

    type WindowPlacementShowCommand =
        | SW_HIDE = 0u
        | SW_MAXIMIZE = 3u
        | SW_MINIMIZE = 6u
        | SW_RESTORE = 9u
        | SW_SHOW = 5u
        | SW_SHOWMAXIMIZED = 3u
        | SW_SHOWMINIMIZED = 2u
        | SW_SHOWMINNOACTIVE = 7u
        | SW_SHOWNA = 8u
        | SW_SHOWNOACTIVATE = 4u
        | SW_SHOWNORMAL = 1u

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type WINDOWPLACEMENT =
        val mutable Length: uint32
        val mutable Flags: WindowPlacementFlags
        val mutable ShowCmd: WindowPlacementShowCommand
        val mutable PtMinPosition: WinPoint
        val mutable PtMaxPosition: WinPoint
        val mutable RcNormalPosition: Rect
        val mutable RcDevice: Rect

    [<DllImport("user32.dll", SetLastError = true)>]
    extern bool GetWindowPlacement(IntPtr hWnd, WINDOWPLACEMENT& lpwndpl)
