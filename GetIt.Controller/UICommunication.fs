namespace GetIt

open System
open System.Reactive.Linq
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
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

        return channel
    }

    let showScene (connection: Ui.UI.UIClient) windowSize =
        let sceneBounds =
            windowSize
            |> Message.WindowSize.FromDomain
            |> connection.ShowScene
            |> Message.Rectangle.ToDomain
        Model.updateCurrent (fun m -> { m with SceneBounds = sceneBounds })

        let d0 =
            Observable.Create(fun (obs: IObserver<Rectangle>) ct ->
                async {
                    use sceneBoundsSubscription = connection.SceneBoundsChanged(Empty())
                    use enumerator = sceneBoundsSubscription.ResponseStream
                    let rec iterate () = async {
                        let! ct = Async.CancellationToken
                        let! hasMore = enumerator.MoveNext(ct) |> Async.AwaitTask
                        if hasMore then
                            Message.Rectangle.ToDomain enumerator.Current
                            |> obs.OnNext
                            do! iterate()
                        else
                            obs.OnCompleted()
                    }
                    do! iterate()
                }
                |> fun a -> Async.StartAsTask(a, cancellationToken = ct)
                :> Task
            )
            |> Observable.subscribe (fun sceneBounds ->
                Model.updateCurrent (fun model -> { model with SceneBounds = sceneBounds })
            )

        let subject = new System.Reactive.Subjects.Subject<_>()

        let waitHandle = new ManualResetEventSlim()

        let d1 =
            Observable.Create (fun (obs: IObserver<Position>) ct ->
                async {
                    use mouseMovedSubscription = connection.MouseMoved()
                    use enumerator = mouseMovedSubscription.ResponseStream
                    let rec iterate () = async {
                        let! ct = Async.CancellationToken
                        let! hasMore = enumerator.MoveNext(ct) |> Async.AwaitTask
                        if hasMore then
                            Message.Position.ToDomain enumerator.Current
                            |> obs.OnNext
                            do! iterate()
                        else
                            obs.OnCompleted()
                    }

                    use d =
                        subject
                        |> Observable.choose (function | MouseMove position -> Some position | _ -> None)
                        |> Observable.sample (TimeSpan.FromMilliseconds 50.)
                        |> Observable.map (fun position ->
                            Observable.ofAsync (async {
                                return!
                                    position
                                    |> Message.Position.FromDomain
                                    |> mouseMovedSubscription.RequestStream.WriteAsync
                                    |> Async.AwaitTask
                            })
                        )
                        |> Observable.concatInner
                        |> Observable.subscribe ignore

                    do! iterate ()
                }
                |> fun a -> Async.StartAsTask(a, cancellationToken = ct)
                :> Task
            )
            |> Observable.subscribe (fun position ->
                Model.updateCurrent (fun model -> { model with MouseState = { model.MouseState with Position = position } })
                waitHandle.Set()
            )

        let d2 =
            subject
            |> Observable.choose (function | MouseClick data -> Some data | _ -> None)
            |> Observable.map (fun mouseClick ->
                async {
                    let request = Message.VirtualScreenMouseClick.FromDomain mouseClick
                    let! ct = Async.CancellationToken
                    use response = connection.MouseClickedAsync(request, cancellationToken = ct)
                    let! responseData = response.ResponseAsync |> Async.AwaitTask
                    return Message.MouseClick.ToDomain responseData
                }
                |> Observable.ofAsync
            )
            |> Observable.switch
            |> Observable.subscribe (fun mouseClick ->
                Model.updateCurrent (fun m -> { m with MouseState = { m.MouseState with LastClick = Some (Guid.NewGuid(), mouseClick) } })
            )

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
