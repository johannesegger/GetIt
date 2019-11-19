namespace GetIt.Windows

open GetIt
open System.ComponentModel
open System.Drawing
open System.IO
open System.Runtime.InteropServices
open System.Threading

module ScreenCapture =
    let captureWindow handle =
        if not <| Win32.ShowWindow(handle, Win32.ShowWindowCommand.SW_RESTORE) then
            printfn "Warning: ShowWindow returned false"
        if not <| Win32.SetForegroundWindow(handle) then
            printfn "Warning: SetForegroundWindow returned false"

        Thread.Sleep(500) // Restoring from minimized state takes some time

        let mutable rect = Unchecked.defaultof<Win32.Rect>
        let status = Win32.DwmGetWindowAttribute(handle, Win32.DWMWINDOWATTRIBUTE.ExtendedFrameBounds, &rect, Marshal.SizeOf<Win32.Rect>())
        if status <> 0 then
            if not <| Win32.GetWindowRect(handle, &rect)
            then raise (Win32Exception(sprintf "Failed to get window size (DwmGetWindowAttribute returned status code %d, GetWindowRect returned false)" status))
        let bounds = Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top)
        use bitmap = new Bitmap(bounds.Width, bounds.Height)

        do
            use graphics = Graphics.FromImage bitmap
            graphics.CopyFromScreen(Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size)

        use stream = new MemoryStream()
        bitmap.Save(stream, Imaging.ImageFormat.Png)
        stream.ToArray()
        |> PngImage
