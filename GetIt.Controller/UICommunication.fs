namespace GetIt

open System
open System.Diagnostics
open System.IO
open System.Runtime.InteropServices
open System.Threading
open FSharp.Control.Reactive
open Google.Protobuf.WellKnownTypes
open Grpc.Core

module internal UICommunication =
    let mutable private connection = None

    let setupLocalConnectionToUIProcess() =
        if Option.isSome connection then raise (GetItException "Connection to UI already set up. Do you call `Game.ShowSceneAndAddTurtle()` multiple times?")
        let localConnection =
            if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
                Process.GetProcessesByName("GetIt.WPF")
                |> Seq.iter (fun p ->
                    if not <| p.CloseMainWindow() then p.Kill()
                    p.WaitForExit()
                )

                let startInfo =
#if DEBUG
                    let path =
                        let rec parentPaths path acc =
                            if isNull path then List.rev acc
                            else parentPaths (Path.GetDirectoryName path) (path :: acc)
                        parentPaths (Path.GetFullPath ".") []
                        |> Seq.choose (fun p ->
                            let projectDir = Path.Combine(p, "GetIt.WPF")
                            if Directory.Exists projectDir
                            then Some projectDir
                            else None
                        )
                        |> Seq.head
                    ProcessStartInfo("dotnet", sprintf "run --project %s" path)
#else
                    let baseDir =
                        System.Reflection.Assembly.GetExecutingAssembly().Location
                        |> Path.GetDirectoryName
                        |> Path.GetDirectoryName
                        |> Path.GetDirectoryName
                    let path = Path.Combine(baseDir, "runtimes", "win-x64", "native", "GetIt.UI", "GetIt.WPF.exe")
                    ProcessStartInfo(path)
#endif

                let proc = Process.Start(startInfo)

                let channel = Channel("127.0.0.1:1503", ChannelCredentials.Insecure)
                channel.ConnectAsync(Nullable<_>(DateTime.UtcNow.Add(TimeSpan.FromSeconds 30.)))
                |> Async.AwaitTask
                |> Async.Catch
                |> Async.RunSynchronously
                |> function
                | Choice1Of2 () -> ()
                | Choice2Of2 e -> raise (GetItException ("Can't connect to UI.", e))
                async {
                    while true do
                        let before = channel.State
                        do! channel.WaitForStateChangedAsync(before, Nullable<_>()) |> Async.AwaitTask
                        let after = channel.State
                        if before = ChannelState.Ready && after = ChannelState.Idle then // TODO not sure if this is the preferred way to detect server shutdown
                            // Close the application if the UI has been closed (throwing an exception might be confusing)
                            // TODO dispose subscriptions etc. ?
                            Environment.Exit 0
                }
                |> Async.Start
                Ui.UI.UIClient(channel)
            else
                raise (GetItException (sprintf "Operating system \"%s\" is not supported." RuntimeInformation.OSDescription))

        connection <- Some localConnection

    let private runWithConnection fn arg =
        match connection with
        | Some connection ->
            try
                fn connection arg
            with e ->
                // TODO verify it's the connection that failed 
                // TODO dispose subscriptions etc. ?
#if !DEBUG
                Environment.Exit 0
#endif
                raise (GetItException ("Error while executing command", e))
        | None ->
            raise (GetItException "Connection to UI not set up. Consider calling `Game.ShowSceneAndAddTurtle()` at the beginning.")

    let showScene sceneSize =
        async {
            use sceneBoundsSubscription = runWithConnection (fun c -> c.SceneBoundsChanged) (Empty())
            use enumerator = sceneBoundsSubscription.ResponseStream
            let rec iterate () = async {
                let! hasMore = enumerator.MoveNext(CancellationToken.None) |> Async.AwaitTask
                if hasMore then
                    let sceneBounds = Message.Rectangle.ToDomain enumerator.Current
                    Model.updateCurrent (fun model -> { model with SceneBounds = sceneBounds })
                    do! iterate ()
                else ()
            }
            do! iterate ()
        }
        |> Async.Start

        let subject = new System.Reactive.Subjects.Subject<_>()

        let waitHandle = new ManualResetEventSlim()
        async {
            use mouseMovedSubscription = runWithConnection (fun c () -> c.MouseMoved ()) ()
            use enumerator = mouseMovedSubscription.ResponseStream
            let rec iterate () = async {
                let! hasMore = enumerator.MoveNext(CancellationToken.None) |> Async.AwaitTask
                if hasMore then
                    let position = Message.Position.ToDomain enumerator.Current
                    Model.updateCurrent (fun model -> { model with MouseState = { model.MouseState with Position = position } })
                    waitHandle.Set()
                    do! iterate ()
                else
                    printfn "MouseMoved responses ended"
            }

            let d1 =
                subject
                |> Observable.choose (function | MouseMove position -> Some position | _ -> None)
                |> Observable.sample (TimeSpan.FromMilliseconds 50.)
                |> Observable.map (fun position ->
                    Observable.defer (fun () ->
                        position
                        |> Message.Position.FromDomain
                        |> mouseMovedSubscription.RequestStream.WriteAsync
                        |> Async.AwaitTask
                        |> Observable.ofAsync
                    )
                )
                |> Observable.concatInner
                |> Observable.subscribe ignore

            do! iterate ()
        }
        |> Async.Start

        let d2 =
            subject
            |> Observable.choose (function | MouseClick data -> Some data | _ -> None)
            |> Observable.map (fun mouseClick ->
                async {
                    let! ct = Async.CancellationToken
                    let! mouseClick =
                        Message.VirtualScreenMouseClick.FromDomain mouseClick
                        |> runWithConnection (fun c request -> c.MouseClickedAsync(request, cancellationToken = ct))
                        |> fun p -> p.ResponseAsync
                        |> Async.AwaitTask
                        |> Async.map Message.MouseClick.ToDomain
                    Model.updateCurrent (fun m -> { m with MouseState = { m.MouseState with LastClick = Some (Guid.NewGuid(), mouseClick) } })
                }
                |> Observable.ofAsync
            )
            |> Observable.switch
            |> Observable.subscribe ignore

        let d3 =
            subject
            |> Observable.subscribe (function
                | MouseMove _ | MouseClick _ -> ()
                | KeyDown key ->
                    Model.updateCurrent (fun m -> { m with KeyboardState = { m.KeyboardState with KeysPressed = Set.add key m.KeyboardState.KeysPressed } })
                | KeyUp key ->
                    Model.updateCurrent (fun m -> { m with KeyboardState = { m.KeyboardState with KeysPressed = Set.remove key m.KeyboardState.KeysPressed } })
            )

        let d4 =
            if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
                GetIt.Windows.DeviceEvents.register subject
            else
                raise (GetItException (sprintf "Operating system \"%s\" is not supported." RuntimeInformation.OSDescription))

        sceneSize
        |> Message.SceneSize.FromDomain
        |> runWithConnection (fun c -> c.ShowScene)
        |> ignore

        waitHandle.Wait()

    let addPlayer playerData =
        let playerId = PlayerId.create ()
        Message.Player.FromDomain (playerId, playerData)
        |> runWithConnection (fun c -> c.AddPlayer)
        |> ignore
        Model.updateCurrent (fun m -> { m with Players = Map.add playerId playerData m.Players |> Player.sendToBack playerId })
        playerId

    let removePlayer playerId =
        Message.PlayerId.FromDomain playerId
        |> runWithConnection (fun c -> c.RemovePlayer)
        |> ignore
        Model.updateCurrent (fun m -> { m with Players = Map.remove playerId m.Players })

    let setPosition playerId position =
        Message.PlayerPosition.FromDomain (playerId, position)
        |> runWithConnection (fun c -> c.SetPosition)
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Position = position })

    let changePosition playerId relativePosition =
        Message.PlayerPosition.FromDomain (playerId, relativePosition)
        |> runWithConnection (fun c -> c.ChangePosition)
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Position = p.Position + relativePosition })

    let setDirection playerId direction =
        Message.PlayerDirection.FromDomain (playerId, direction)
        |> runWithConnection (fun c -> c.SetDirection)
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Direction = direction })

    let changeDirection playerId relativeDirection =
        Message.PlayerDirection.FromDomain (playerId, relativeDirection)
        |> runWithConnection (fun c -> c.ChangeDirection)
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Direction = p.Direction + relativeDirection })

    let say playerId text =
        Message.PlayerText.FromDomain (playerId, text)
        |> runWithConnection (fun c -> c.Say)
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with SpeechBubble = Some (Say text) })

    let ask playerId text =
        Model.updatePlayer playerId (fun p -> { p with SpeechBubble = Some (Ask text) })
        try
            Message.PlayerText.FromDomain (playerId, text)
            |> runWithConnection (fun c -> c.Ask)
            |> Message.Answer.ToDomain
        finally
            Model.updatePlayer playerId (fun p -> { p with SpeechBubble = None })

    let shutUp playerId =
        let answer =
            Message.PlayerId.FromDomain playerId
            |> runWithConnection (fun c -> c.ShutUp)
            |> ignore
        Model.updatePlayer playerId (fun p -> { p with SpeechBubble = None })
        answer

    let setPenState playerId isOn =
        Message.PlayerPenState.FromDomain (playerId, isOn)
        |> runWithConnection (fun c -> c.SetPenState)
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Pen = { p.Pen with IsOn = isOn } })

    let togglePenState playerId =
        Message.PlayerId.FromDomain playerId
        |> runWithConnection (fun c -> c.TogglePenState)
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Pen = { p.Pen with IsOn = not p.Pen.IsOn } })

    let setPenColor playerId color =
        Message.PlayerPenColor.FromDomain (playerId, color)
        |> runWithConnection (fun c -> c.SetPenColor)
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Color = color } })

    let shiftPenColor playerId angle =
        Message.PlayerPenColorShift.FromDomain (playerId, angle)
        |> runWithConnection (fun c -> c.ShiftPenColor)
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Color = Color.hueShift angle p.Pen.Color } })

    let setPenWeight playerId weight =
        Message.PlayerPenWeight.FromDomain (playerId, weight)
        |> runWithConnection (fun c -> c.SetPenWeight)
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Weight = weight } })

    let changePenWeight playerId weight =
        Message.PlayerPenWeight.FromDomain (playerId, weight)
        |> runWithConnection (fun c -> c.ChangePenWeight)
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Weight = p.Pen.Weight + weight } })

    let setSizeFactor playerId sizeFactor =
        Message.PlayerSizeFactor.FromDomain (playerId, sizeFactor)
        |> runWithConnection (fun c -> c.SetSizeFactor)
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with SizeFactor = sizeFactor })

    let changeSizeFactor playerId sizeFactor =
        Message.PlayerSizeFactor.FromDomain (playerId, sizeFactor)
        |> runWithConnection (fun c -> c.ChangeSizeFactor)
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with SizeFactor = p.SizeFactor + sizeFactor })

    let setNextCostume playerId =
        Message.PlayerId.FromDomain playerId
        |> runWithConnection (fun c -> c.SetNextCostume)
        |> ignore
        Model.updatePlayer playerId Player.nextCostume

    let sendToBack playerId =
        Message.PlayerId.FromDomain playerId
        |> runWithConnection (fun c -> c.SendToBack)
        |> ignore
        Model.updateCurrent (fun m -> { m with Players = Player.sendToBack playerId m.Players })

    let bringToFront playerId =
        Message.PlayerId.FromDomain playerId
        |> runWithConnection (fun c -> c.BringToFront)
        |> ignore
        Model.updateCurrent (fun m -> { m with Players = Player.bringToFront playerId m.Players })

    let setWindowTitle text =
        text
        |> Message.WindowTitle.FromDomain
        |> runWithConnection (fun c -> c.SetWindowTitle)
        |> ignore

    let setBackground image =
        image
        |> Message.SvgImage.FromDomain
        |> runWithConnection (fun c -> c.SetBackground)
        |> ignore

    let clearScene () =
        Empty()
        |> runWithConnection (fun c -> c.ClearScene)
        |> ignore

    let makeScreenshot () =
        Empty()
        |> runWithConnection (fun c -> c.MakeScreenshot)
        |> Message.PngImage.ToDomain

    let startBatch () =
        Empty()
        |> runWithConnection (fun c -> c.StartBatch)
        |> ignore

    let applyBatch () =
        Empty()
        |> runWithConnection (fun c -> c.ApplyBatch)
        |> ignore
