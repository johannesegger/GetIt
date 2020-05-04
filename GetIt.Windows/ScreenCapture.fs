namespace GetIt.Windows

open GetIt
open System.ComponentModel
open System.Drawing
open System.IO
open System.Runtime.InteropServices
open System.Threading

type CaptureRegion = FullWindow | WindowContent

module ScreenCapture =
    let captureWindow handle region =
        if not <| Win32.ShowWindow(handle, Win32.ShowWindowCommand.SW_RESTORE) then
            printfn "Warning: ShowWindow returned false"
        if not <| Win32.SetForegroundWindow(handle) then
            printfn "Warning: SetForegroundWindow returned false"

        Thread.Sleep(500) // Restoring from minimized state takes some time


        let (left, top, right, bottom) =
            match region with
            | FullWindow ->
                let mutable rect = Unchecked.defaultof<Win32.Rect>
                let status = Win32.DwmGetWindowAttribute(handle, Win32.DWMWINDOWATTRIBUTE.ExtendedFrameBounds, &rect, Marshal.SizeOf<Win32.Rect>())
                if status <> 0 then
                    if not <| Win32.GetWindowRect(handle, &rect)
                    then raise (Win32Exception(sprintf "Failed to get window size (DwmGetWindowAttribute returned status code %d, GetWindowRect returned false)" status))
                (rect.Left, rect.Top, rect.Right, rect.Bottom)
            | WindowContent ->
                let mutable rect = Unchecked.defaultof<Win32.Rect>
                if not <| Win32.GetClientRect(handle, &rect)
                then raise (Win32Exception "Failed to get client size (GetClientRect returned false)")
                let mutable topLeft = Win32.WinPoint(x = rect.Left, y = rect.Top)
                if not <| Win32.ClientToScreen(handle, &topLeft)
                then raise (Win32Exception "Failed to translate top left coordinate (ClientToScreen returned false)")
                let mutable bottomRight = Win32.WinPoint(x = rect.Right, y = rect.Bottom)
                if not <| Win32.ClientToScreen(handle, &bottomRight)
                then raise (Win32Exception "Failed to translate bottom right coordinate (ClientToScreen returned false)")
                (topLeft.x, topLeft.y, bottomRight.x, bottomRight.y)

        let bounds = Rectangle(left, top, right - left, bottom - top)
        use bitmap = new Bitmap(bounds.Width, bounds.Height)

        do
            use graphics = Graphics.FromImage bitmap
            graphics.CopyFromScreen(Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size)

        use stream = new MemoryStream()
        bitmap.Save(stream, Imaging.ImageFormat.Png)
        stream.ToArray()
        |> PngImage
