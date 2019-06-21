namespace GetIt.Windows

open System
open System.ComponentModel
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Runtime.InteropServices
open System.Threading
open FSharp.Control.Reactive
open GetIt

module DeviceEvents =
    type private WndProc = delegate of (* hWnd *)IntPtr * (* msg *)uint32 * (* wParam *)IntPtr * (* lParam *)IntPtr -> IntPtr

    let private getRawInputData (rawInputHandle: IntPtr) =
        let mutable dwSize = 0u
        let sizeOfHeader = uint32 (Marshal.SizeOf(typeof<SharpLib.Win32.RAWINPUTHEADER>))

        let getRawInputDataResult1 =
            SharpLib.Win32.Function.GetRawInputData(
                rawInputHandle,
                uint32 SharpLib.Win32.Const.RID_INPUT,
                IntPtr.Zero,
                &dwSize,
                sizeOfHeader)
        if getRawInputDataResult1 <> 0u then
            let errorCode = Marshal.GetLastWin32Error()
            raise (Win32Exception(sprintf "GetRawInputData failed. Error code 0x%08x" errorCode))

        //Allocate a large enough buffer
        let rawInputBuffer = Marshal.AllocHGlobal(int dwSize)

        //Now read our RAWINPUT data
        let getRawInputDataResult2 =
            SharpLib.Win32.Function.GetRawInputData(
                rawInputHandle,
                uint32 SharpLib.Win32.Const.RID_INPUT,
                rawInputBuffer,
                &dwSize,
                sizeOfHeader)
        if getRawInputDataResult2 <> dwSize then
            let errorCode = Marshal.GetLastWin32Error()
            raise (Win32Exception(sprintf "GetRawInputData failed. Error code 0x%08x" errorCode))

        Marshal.PtrToStructure(rawInputBuffer, typeof<SharpLib.Win32.RAWINPUT>) :?> SharpLib.Win32.RAWINPUT

    let private getCurrentMousePosition () =
        let virtualDesktopLeft = Win32.GetSystemMetrics(Win32.SystemMetric.SM_XVIRTUALSCREEN)
        let virtualDesktopTop = Win32.GetSystemMetrics(Win32.SystemMetric.SM_YVIRTUALSCREEN)
        let virtualDesktopWidth = Win32.GetSystemMetrics(Win32.SystemMetric.SM_CXVIRTUALSCREEN)
        let virtualDesktopHeight = Win32.GetSystemMetrics(Win32.SystemMetric.SM_CYVIRTUALSCREEN)

        let mutable point = Unchecked.defaultof<Win32.WinPoint>
        if not <| Win32.GetCursorPos(&point) then
            let errorCode = Marshal.GetLastWin32Error()
            raise (Win32Exception (sprintf "GetCursorPos failed. Error code 0x%08x" errorCode))

        // Use percent for position to support different screen sizes for controller and UI
        {
            X = float (point.x - virtualDesktopLeft) / float virtualDesktopWidth
            Y = float (point.y - virtualDesktopTop) / float virtualDesktopHeight
        }

    let private processMessage messageLParam =
        let rawInput = getRawInputData messageLParam

        if rawInput.header.dwType = SharpLib.Win32.RawInputDeviceType.RIM_TYPEKEYBOARD then
            // https://docs.microsoft.com/de-de/windows/desktop/inputdev/virtual-key-codes
            let keyMap =
                [
                    0x20us, Space
                    0x1Bus, Escape
                    0x0Dus, Enter
                    0x26us, Up
                    0x28us, Down
                    0x25us, Left
                    0x27us, Right
                    0x41us, A
                    0x42us, B
                    0x43us, C
                    0x44us, D
                    0x45us, E
                    0x46us, F
                    0x47us, G
                    0x48us, H
                    0x49us, I
                    0x4Aus, J
                    0x4Bus, K
                    0x4Cus, L
                    0x4Dus, M
                    0x4Eus, N
                    0x4Fus, O
                    0x50us, P
                    0x51us, Q
                    0x52us, R
                    0x53us, S
                    0x54us, T
                    0x55us, U
                    0x56us, V
                    0x57us, W
                    0x58us, X
                    0x59us, Y
                    0x5Aus, Z
                    0x30us, Digit0
                    0x31us, Digit1
                    0x32us, Digit2
                    0x33us, Digit3
                    0x34us, Digit4
                    0x35us, Digit5
                    0x36us, Digit6
                    0x37us, Digit7
                    0x38us, Digit8
                    0x39us, Digit9
                ]
                |> Map.ofList

            Map.tryFind rawInput.data.keyboard.VKey keyMap
            |> Option.map (fun key ->
                let isKeyDown = rawInput.data.keyboard.Message = uint32 SharpLib.Win32.Const.WM_KEYDOWN
                // let isKeyDown = rawInput.data.keyboard.Flags.HasFlag(SharpLib.Win32.RawInputKeyFlags.RI_KEY_MAKE)
                if isKeyDown then KeyDown key
                else KeyUp key
            )
            |> Option.toList
        elif rawInput.header.dwType = SharpLib.Win32.RawInputDeviceType.RIM_TYPEMOUSE then
            let hasbuttonFlag flag =
                rawInput.data.mouse.mouseData.buttonsStr.usButtonFlags.HasFlag(flag)
            [
                let position = getCurrentMousePosition ()

                if rawInput.data.mouse.lLastX <> 0 || rawInput.data.mouse.lLastY <> 0 then
                    yield MouseMove position
                if hasbuttonFlag SharpLib.Win32.RawInputMouseButtonFlags.RI_MOUSE_LEFT_BUTTON_UP then
                    yield MouseClick { Button = Primary; VirtualScreenPosition = position }
                if hasbuttonFlag SharpLib.Win32.RawInputMouseButtonFlags.RI_MOUSE_RIGHT_BUTTON_UP then
                    yield MouseClick {Button = Secondary; VirtualScreenPosition = position }
            ]
        else
            []

    let observable =
        Observable.Create (fun (obs: IObserver<_>) ->
            let mutable windowHandle = IntPtr.Zero
            let shutDownMessage = uint32 SharpLib.Win32.Const.WM_USER + 1u
            use waitHandle = new ManualResetEventSlim()

            let run () =
                let moduleHandle = Win32.GetModuleHandle(null)

                let wndProc = WndProc(fun hWnd msg wParam lParam ->
                    if msg = shutDownMessage then
                        Win32.PostQuitMessage(0)
                        IntPtr.Zero
                    elif msg = uint32 SharpLib.Win32.Const.WM_INPUT then
                        try
                            processMessage lParam
                            |> Seq.iter obs.OnNext
                        with e ->
                            printfn "Error while processing message: %O" e
                        IntPtr.Zero
                        // case WM_DESTROY:
                        //     DestroyWindow(hWnd);

                        //     //If you want to shutdown the application, call the next function instead of DestroyWindow
                        //     //PostQuitMessage(0);
                        //     break;
                    else
                        Win32.DefWindowProc(hWnd, msg, wParam, lParam)
                )

                // Prevent GC'ing the allocated delegate (see https://github.com/johannesegger/GetIt/issues/8 and https://stackoverflow.com/a/16544880/1293659)
                let wndProcHandle = GCHandle.Alloc wndProc
                use disposableWndProcHandle = Disposable.create wndProcHandle.Free

                let mutable windowClass =
                    Win32.WNDCLASSEX(
                        cbSize = Marshal.SizeOf(typeof<Win32.WNDCLASSEX>),
                        style = 0,
                        lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProc),
                        cbClsExtra = 0,
                        cbWndExtra = 0,
                        hInstance = moduleHandle,
                        hIcon = IntPtr.Zero,
                        hCursor = IntPtr.Zero,
                        hbrBackground = IntPtr.Zero,
                        lpszMenuName = null,
                        lpszClassName = "GetItWindowClass",
                        hIconSm = IntPtr.Zero
                    )

                let windowClassAtom = Win32.RegisterClassEx(&windowClass)
                if windowClassAtom = 0us then
                    let errorCode = Marshal.GetLastWin32Error()
                    raise (Win32Exception(sprintf "Failed to register window class. Error code: 0x%08x" errorCode))

                windowHandle <-
                    Win32.CreateWindowEx(
                        0u,
                        windowClassAtom,
                        null,
                        0u,
                        0,
                        0,
                        0,
                        0,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        moduleHandle,
                        IntPtr.Zero)

                if windowHandle = IntPtr.Zero then
                    let errorCode = Marshal.GetLastWin32Error()
                    raise(Win32Exception(sprintf "Failed to create window. Error code: 0x%08x" errorCode))

                // https://docs.microsoft.com/en-us/windows-hardware/drivers/hid/top-level-collections-opened-by-windows-for-system-use
                let keyboardUsagePage = 0x01us
                let keyboardUsageId = 0x06us
                let mouseUsagePage = 0x01us
                let mouseUsageId = 0x02us

                let rawInputDevices =
                    [|
                        SharpLib.Win32.RAWINPUTDEVICE(
                            usUsagePage = keyboardUsagePage,
                            usUsage = keyboardUsageId,
                            dwFlags = SharpLib.Win32.RawInputDeviceFlags.RIDEV_INPUTSINK,
                            hwndTarget = windowHandle
                        )
                        SharpLib.Win32.RAWINPUTDEVICE(
                            usUsagePage = mouseUsagePage,
                            usUsage = mouseUsageId,
                            dwFlags = SharpLib.Win32.RawInputDeviceFlags.RIDEV_INPUTSINK,
                            hwndTarget = windowHandle
                        )
                    |]

                let isRegistered =
                    SharpLib.Win32.Function.RegisterRawInputDevices(
                        rawInputDevices,
                        uint32 rawInputDevices.Length,
                        uint32 (Marshal.SizeOf(rawInputDevices.[0])))
                if not isRegistered then
                    let errorCode = Marshal.GetLastWin32Error();
                    raise(Win32Exception(sprintf "Failed to register raw input devices. Error code: 0x%08x" errorCode))

                let mutable message: Win32.WinMsg = Unchecked.defaultof<Win32.WinMsg>

                // Ensure queue is created
                Win32.PeekMessage(&message, IntPtr.Zero, 0u, 0u, 0u) |> ignore

                waitHandle.Set()

                while Win32.GetMessage(&message, IntPtr.Zero, 0u, 0u) > 0 do
                    Win32.TranslateMessage(&message) |> ignore
                    Win32.DispatchMessage(&message) |> ignore

            let messageLoopThread = Thread(run)
            messageLoopThread.Name <- "Win32 message loop"
            messageLoopThread.IsBackground <- false
            messageLoopThread.Start()

            waitHandle.Wait()

            let position = getCurrentMousePosition ()
            obs.OnNext (MouseMove position)

            Disposable.create (fun () ->
                Win32.SendMessage(windowHandle, shutDownMessage, UIntPtr.Zero, IntPtr.Zero) |> ignore
            )
        )
