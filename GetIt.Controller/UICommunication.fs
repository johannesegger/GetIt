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
    let setupConnectionToUI host port = async {
        let channel = Channel(sprintf "%s:%d" host port, ChannelCredentials.Insecure)
        let! connectionResult =
#if DEBUG
            channel.ConnectAsync()
#else
            channel.ConnectAsync(Nullable<_>(DateTime.UtcNow.Add(TimeSpan.FromSeconds 30.)))
#endif
            |> Async.AwaitTask
            |> Async.Catch

        match connectionResult with
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

        return channel
    }

    let showScene (connection: Ui.UI.UIClient) sceneSize =
        let sceneBounds =
            sceneSize
            |> Message.SceneSize.FromDomain
            |> connection.ShowScene
            |> Message.Rectangle.ToDomain
        Model.updateCurrent (fun m -> { m with SceneBounds = sceneBounds })

        async {
            use sceneBoundsSubscription = connection.SceneBoundsChanged (Empty())
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
            use mouseMovedSubscription = connection.MouseMoved ()
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
                        |> fun request -> connection.MouseClickedAsync(request, cancellationToken = ct)
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

        waitHandle.Wait()

    let addPlayer (connection: Ui.UI.UIClient) playerData =
        let playerId = PlayerId.create ()
        Message.Player.FromDomain (playerId, playerData)
        |> connection.AddPlayer
        |> ignore
        Model.updateCurrent (fun m -> { m with Players = Map.add playerId playerData m.Players |> Player.sendToBack playerId })
        playerId

    let removePlayer (connection: Ui.UI.UIClient) playerId =
        Message.PlayerId.FromDomain playerId
        |> connection.RemovePlayer
        |> ignore
        Model.updateCurrent (fun m -> { m with Players = Map.remove playerId m.Players })

    let setPosition (connection: Ui.UI.UIClient) playerId position =
        Message.PlayerPosition.FromDomain (playerId, position)
        |> connection.SetPosition
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Position = position })

    let changePosition (connection: Ui.UI.UIClient) playerId relativePosition =
        Message.PlayerPosition.FromDomain (playerId, relativePosition)
        |> connection.ChangePosition
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Position = p.Position + relativePosition })

    let setDirection (connection: Ui.UI.UIClient) playerId direction =
        Message.PlayerDirection.FromDomain (playerId, direction)
        |> connection.SetDirection
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Direction = direction })

    let changeDirection (connection: Ui.UI.UIClient) playerId relativeDirection =
        Message.PlayerDirection.FromDomain (playerId, relativeDirection)
        |> connection.ChangeDirection
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Direction = p.Direction + relativeDirection })

    let say (connection: Ui.UI.UIClient) playerId text =
        Message.PlayerText.FromDomain (playerId, text)
        |> connection.Say
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with SpeechBubble = Some (Say text) })

    let ask (connection: Ui.UI.UIClient) playerId text =
        Model.updatePlayer playerId (fun p -> { p with SpeechBubble = Some (Ask text) })
        try
            Message.PlayerText.FromDomain (playerId, text)
            |> connection.Ask
            |> Message.Answer.ToDomain
        finally
            Model.updatePlayer playerId (fun p -> { p with SpeechBubble = None })

    let shutUp (connection: Ui.UI.UIClient) playerId =
        let answer =
            Message.PlayerId.FromDomain playerId
            |> connection.ShutUp
            |> ignore
        Model.updatePlayer playerId (fun p -> { p with SpeechBubble = None })
        answer

    let setPenState (connection: Ui.UI.UIClient) playerId isOn =
        Message.PlayerPenState.FromDomain (playerId, isOn)
        |> connection.SetPenState
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Pen = { p.Pen with IsOn = isOn } })

    let togglePenState (connection: Ui.UI.UIClient) playerId =
        Message.PlayerId.FromDomain playerId
        |> connection.TogglePenState
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Pen = { p.Pen with IsOn = not p.Pen.IsOn } })

    let setPenColor (connection: Ui.UI.UIClient) playerId color =
        Message.PlayerPenColor.FromDomain (playerId, color)
        |> connection.SetPenColor
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Color = color } })

    let shiftPenColor (connection: Ui.UI.UIClient) playerId angle =
        Message.PlayerPenColorShift.FromDomain (playerId, angle)
        |> connection.ShiftPenColor
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Color = Color.hueShift angle p.Pen.Color } })

    let setPenWeight (connection: Ui.UI.UIClient) playerId weight =
        Message.PlayerPenWeight.FromDomain (playerId, weight)
        |> connection.SetPenWeight
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Weight = weight } })

    let changePenWeight (connection: Ui.UI.UIClient) playerId weight =
        Message.PlayerPenWeight.FromDomain (playerId, weight)
        |> connection.ChangePenWeight
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with Pen = { p.Pen with Weight = p.Pen.Weight + weight } })

    let setSizeFactor (connection: Ui.UI.UIClient) playerId sizeFactor =
        Message.PlayerSizeFactor.FromDomain (playerId, sizeFactor)
        |> connection.SetSizeFactor
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with SizeFactor = sizeFactor })

    let changeSizeFactor (connection: Ui.UI.UIClient) playerId sizeFactor =
        Message.PlayerSizeFactor.FromDomain (playerId, sizeFactor)
        |> connection.ChangeSizeFactor
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with SizeFactor = p.SizeFactor + sizeFactor })

    let setNextCostume (connection: Ui.UI.UIClient) playerId =
        Message.PlayerId.FromDomain playerId
        |> connection.SetNextCostume
        |> ignore
        Model.updatePlayer playerId Player.nextCostume

    let sendToBack (connection: Ui.UI.UIClient) playerId =
        Message.PlayerId.FromDomain playerId
        |> connection.SendToBack
        |> ignore
        Model.updateCurrent (fun m -> { m with Players = Player.sendToBack playerId m.Players })

    let bringToFront (connection: Ui.UI.UIClient) playerId =
        Message.PlayerId.FromDomain playerId
        |> connection.BringToFront
        |> ignore
        Model.updateCurrent (fun m -> { m with Players = Player.bringToFront playerId m.Players })

    let setVisibility (connection: Ui.UI.UIClient) playerId isVisible =
        Message.PlayerVisibility.FromDomain (playerId, isVisible)
        |> connection.SetVisibility
        |> ignore
        Model.updatePlayer playerId (fun p -> { p with IsVisible = isVisible })

    let setWindowTitle (connection: Ui.UI.UIClient) text =
        text
        |> Message.WindowTitle.FromDomain
        |> connection.SetWindowTitle
        |> ignore

    let setBackground (connection: Ui.UI.UIClient) image =
        image
        |> Message.SvgImage.FromDomain
        |> connection.SetBackground
        |> ignore

    let clearScene (connection: Ui.UI.UIClient) () =
        Empty()
        |> connection.ClearScene
        |> ignore

    let makeScreenshot (connection: Ui.UI.UIClient) () =
        Empty()
        |> connection.MakeScreenshot
        |> Message.PngImage.ToDomain

    let startBatch (connection: Ui.UI.UIClient) () =
        Empty()
        |> connection.StartBatch
        |> ignore

    let applyBatch (connection: Ui.UI.UIClient) () =
        Empty()
        |> connection.ApplyBatch
        |> ignore
