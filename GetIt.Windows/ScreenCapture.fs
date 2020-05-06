namespace GetIt.Windows

open FSharp.Control.Reactive
open GetIt
open System
open System.ComponentModel
open System.Drawing
open System.Drawing.Imaging
open System.Runtime.InteropServices
open System.IO

type CaptureRegion = FullWindow | WindowContent

module ScreenCapture =
    let private getFullWindowRect handle =
        let mutable rect = Win32.Rect()
        if not <| Win32.GetWindowRect(handle, &rect) then raise (Win32Exception("Failed to get window size (GetWindowRect returned false)"))
        (rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top)

    let private tryGetOuterWindowRect handle =
        let mutable rect = Win32.Rect()
        let status = Win32.DwmGetWindowAttribute(handle, Win32.DWMWINDOWATTRIBUTE.ExtendedFrameBounds, &rect, Marshal.SizeOf<Win32.Rect>())
        if status = 0 then Some (rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top)
        else None

    let private getInnerWindowRect handle =
        let mutable rect = Win32.Rect()
        if not <| Win32.GetClientRect(handle, &rect) then raise (Win32Exception("Failed to get client size (GetClientRect returned false)"))
        let mutable topLeft = Win32.WinPoint(x = rect.Left, y = rect.Top)
        if not <| Win32.ClientToScreen(handle, &topLeft) then raise (Win32Exception("Failed to translate top left coordinate (ClientToScreen returned false)"))
        let mutable bottomRight = Win32.WinPoint(x = rect.Right, y = rect.Bottom)
        if not <| Win32.ClientToScreen(handle, &bottomRight) then raise (Win32Exception("Failed to translate bottom right coordinate (ClientToScreen returned false)"))
        (topLeft.x, topLeft.y, bottomRight.x - topLeft.x, bottomRight.y - topLeft.y)

    let private relativeTo (refLeft, refTop, _refWidth, _refHeight) (srcLeft, srcTop, srcWidth, srcHeight) : (int * int * int * int) =
        (srcLeft - refLeft, srcTop - refTop, srcWidth, srcHeight)

    let captureWindow handle region =
        let windowDeviceContext = Win32.GetWindowDC(handle)
        if windowDeviceContext = IntPtr.Zero then raise (Win32Exception("Failed to get window device context (GetWindowDC returned null pointer)"))

        use __ = Disposable.create (fun () ->
            if not <| Win32.ReleaseDC(handle, windowDeviceContext)
            then raise (Win32Exception("Failed to release window device context (ReleaseDC returned false)"))
        )

        let (left, top, width, height) =
            match region with
            | FullWindow ->
                let fullWindowRect = getFullWindowRect handle
                match tryGetOuterWindowRect handle with
                | Some outerWindowRect -> outerWindowRect |> relativeTo fullWindowRect
                | None -> fullWindowRect
            | WindowContent ->
                let fullWindowRect = getFullWindowRect handle
                getInnerWindowRect handle |> relativeTo fullWindowRect

        let memoryDeviceContext = Win32.CreateCompatibleDC(windowDeviceContext)
        if memoryDeviceContext = IntPtr.Zero then raise (Win32Exception("Failed to create memory device context (CreateCompatibleDC returned null pointer)"))

        use __ = Disposable.create (fun () ->
            if not <| Win32.DeleteDC(memoryDeviceContext)
            then raise (Win32Exception("Deleting memory device context failed (DeleteDC returned false)"))
        )

        let memoryBitmap = Win32.CreateCompatibleBitmap(windowDeviceContext, width, height);
        if memoryBitmap = IntPtr.Zero then raise (Win32Exception("Failed to create memory bitmap (CreateCompatibleBitmap returned null pointer)"))

        use __ = Disposable.create (fun () ->
            if not <| Win32.DeleteObject(memoryBitmap)
            then raise (Win32Exception("Deleting memory bitmap failed (DeleteObject returned false)"))
        )

        let previousObject = Win32.SelectObject(memoryDeviceContext, memoryBitmap)
        if previousObject = IntPtr.Zero
        then raise (Win32Exception("Failed to select memory bitmap into memory device context (SelectObject returned null pointer)"))

        use __ = Disposable.create (fun () ->
            if Win32.SelectObject(memoryDeviceContext, previousObject) = IntPtr.Zero
            then raise (Win32Exception("Failed to unselect memory bitmap (SelectObject returned null pointer)"))
        )
        if not <| Win32.BitBlt(memoryDeviceContext, 0, 0, width, height, windowDeviceContext, left, top, 13369376 (*SRCCOPY*))
        then raise (Win32Exception("Failed to copy color data from window device context into memory device context (BitBlt returned false)"))

        let image = Image.FromHbitmap(memoryBitmap)
        use stream = new MemoryStream()
        image.Save(stream, ImageFormat.Png)
        stream.ToArray()
        |> PngImage
