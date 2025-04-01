module GetIt.UI.Container.InputEvents

open System
open System.Runtime.InteropServices

module MacOSNative =
    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type CGPoint =
        val mutable x: double
        val mutable y: double
    [<DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")>]
    extern IntPtr CGEventCreate(IntPtr source)
    [<DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")>]
    extern CGPoint CGEventGetLocation(IntPtr event)
    [<DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")>]
    extern void CFRelease(IntPtr cf)

    let getCurrentMousePosition () =
        let cgEvent = CGEventCreate IntPtr.Zero
        let cgPoint = CGEventGetLocation cgEvent
        CFRelease cgEvent
        cgPoint.x, cgPoint.y


module WindowsNative =
    open System.ComponentModel

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type WinPoint =
        val mutable x: int
        val mutable y: int

    [<DllImport("user32.dll", SetLastError = true)>]
    extern bool GetCursorPos(WinPoint& lpPoint)

    let getCurrentMousePosition () =
        let mutable point = Unchecked.defaultof<WinPoint>
        if not <| GetCursorPos(&point) then
            let errorCode = Marshal.GetLastWin32Error()
            raise (Win32Exception (sprintf "GetCursorPos failed. Error code 0x%08x" errorCode))

        float point.x, float point.y

let getCurrentMousePosition () =
    if RuntimeInformation.IsOSPlatform OSPlatform.Windows then WindowsNative.getCurrentMousePosition ()
    elif RuntimeInformation.IsOSPlatform OSPlatform.OSX then MacOSNative.getCurrentMousePosition ()
    else failwith $"Platform not supported: %s{RuntimeInformation.OSDescription}"
