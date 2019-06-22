namespace GetIt

open Elmish.Streams.AspNetCore.Middleware
open FSharp.Control
open FSharp.Control.Reactive
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open System
open System.Diagnostics
open System.IO
open System.Reactive.Linq
open System.Reactive.Disposables
open System.Reflection
open System.Runtime.InteropServices
open System.Threading
open Thoth.Json.Net

module UICommunication =
    type CommunicationState = {
        Disposable: IDisposable
        MessageSubject: System.Reactive.Subjects.Subject<ControllerMsg>
    }
    let mutable private showSceneCalled = 0
    let mutable private communicationState = None
    let private url = "http://localhost:1503/"

    let private startWebServer controllerMsgs ct = async {
        let stream (connectionId: ConnectionId) (msgs: IAsyncObservable<ChannelMsg * ConnectionId>) : IAsyncObservable<ChannelMsg * ConnectionId> =
            printfn "Client %s connected" connectionId
            let controllerMsgs =
                AsyncRx.create (fun obs -> async {
                    return
                        controllerMsgs
                        |> Observable.subscribeWithCallbacks
                            (obs.OnNextAsync >> Async.Start)
                            (obs.OnErrorAsync >> Async.Start)
                            (obs.OnCompletedAsync >> Async.Start)
                        |> fun d -> AsyncDisposable.Create (fun () -> async { d.Dispose() })
                })
                |> AsyncRx.map (fun msg -> ControllerMsg msg, "")
            
            msgs
            |> AsyncRx.flatMap(fun (msg, connId) ->
                match msg with
                | ControllerMsg msg -> AsyncRx.empty ()
                | UIMsg (SetSceneBounds sceneBounds as uiMsg) ->
                    Model.updateCurrent (fun model -> Some uiMsg, { model with SceneBounds = sceneBounds })
                    AsyncRx.single (msg, connId)
                | UIMsg (SetMousePosition mousePosition as uiMsg) ->
                    Model.updateCurrent (fun model -> Some uiMsg, { model with MouseState = { model.MouseState with Position = mousePosition } })
                    AsyncRx.single (msg, connId)
                | UIMsg (ApplyMouseClick _ as uiMsg)
                | UIMsg (UpdateStringAnswer _ as uiMsg)
                | UIMsg (AnswerStringQuestion _ as uiMsg)
                | UIMsg (AnswerBoolQuestion _ as uiMsg)
                | UIMsg (Screenshot _ as uiMsg) ->
                    Model.updateCurrent (fun model -> Some uiMsg, model)
                    AsyncRx.single (msg, connId)
            )
            |> AsyncRx.merge controllerMsgs

        let configureApp (app: IApplicationBuilder) =
            app
                .UseDefaultFiles()
                .UseStaticFiles()
                .UseWebSockets()
                .UseStream(fun options ->
                    { options with
                        Stream = stream
                        Encode = Encode.channelMsg >> Encode.toString 0
                        Decode =
                            Decode.fromString Decode.channelMsg
                            >> (function
                                | Ok p -> Some p
                                | Error p ->
                                    eprintfn "Deserializing message failed: %O" p
                                    None
                            )
                        RequestPath = MessageChannel.endpoint
                    }
                )
                |> ignore

        do!
            WebHostBuilder()
                .UseKestrel()
                .UseWebRoot(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "GetIt.UI"))
                .Configure(Action<IApplicationBuilder> configureApp)
                .UseUrls(url)
                .Build()
                .RunAsync(ct)
            |> Async.AwaitTask
    }

    let private startUI windowSize = async {
        // TODO check that chrome is installed and/or support other browsers
        let args =
            [
                yield "--user-data-dir", Path.Combine(Path.GetTempPath(), "chrome-workspace-for-getit") |> Some
#if DEBUG
                yield "--app", "http://localhost:8080" |> Some
#else
                yield "--app", url |> Some
#endif
                match windowSize with
                | SpecificSize windowSize ->
                    yield "--window-size", sprintf "%d,%d" (int windowSize.Width) (int windowSize.Height) |> Some
                | Maximized ->
                    yield "--start-maximized", None
            ]
            |> List.map (function
                | key, Some value -> sprintf "%s=\"%s\"" key value
                | key, None -> key
            )
            |> String.concat " "
        let proc = Process.Start("chrome.exe", args)
        proc.WaitForExit ()
        if proc.ExitCode <> 0 then
            raise (GetItException (sprintf "UI exited with non-zero exit code: %d" proc.ExitCode))
    }

    let inputEvents =
        Observable.Create (fun (obs: IObserver<InputEvent>) ->
            let observable =
                if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
                    GetIt.Windows.DeviceEvents.observable
                else
                    raise (GetItException (sprintf "Operating system \"%s\" is not supported." RuntimeInformation.OSDescription))

            let d1 =
                observable
                |> Observable.choose (function | MouseMove _ as x -> Some x | _ -> None)
                |> Observable.sample (TimeSpan.FromMilliseconds 50.)
                |> Observable.subscribeObserver obs

            let d2 =
                observable
                |> Observable.choose (function | MouseClick _ as x -> Some x | _ -> None)
                |> Observable.subscribeObserver obs

            let d3 =
                observable
                |> Observable.subscribe (function
                    | MouseMove _ | MouseClick _ -> ()
                    | KeyDown key ->
                        Model.updateCurrent (fun m -> None, { m with KeyboardState = { m.KeyboardState with KeysPressed = Set.add key m.KeyboardState.KeysPressed } })
                    | KeyUp key ->
                        Model.updateCurrent (fun m -> None, { m with KeyboardState = { m.KeyboardState with KeysPressed = Set.remove key m.KeyboardState.KeysPressed } })
                )

            d1
            |> Disposable.compose d2
            |> Disposable.compose d3
        )

    let showScene windowSize =
        if Interlocked.CompareExchange(&showSceneCalled, 1, 0) <> 0 then
            raise (GetItException "Connection to UI already set up. Do you call `Game.ShowScene()` multiple times?")

        let webServerStopDisposable = new CancellationDisposable()
        let controllerMsgs = new System.Reactive.Subjects.Subject<_>()

        let runThread =
            Thread(
                (fun () ->
                    async {
                        let msgs =
                            inputEvents
                            |> Observable.map InputEvent
                            |> Observable.merge controllerMsgs
                        let! webServerRunTask = startWebServer msgs webServerStopDisposable.Token |> Async.StartChild
                        let! processRunTask = startUI windowSize |> Async.StartChild
                        
                        do! processRunTask
                        webServerStopDisposable.Dispose()
                        do! webServerRunTask
                    }
                    |> Async.RunSynchronously
                ),
                Name = "Run thread",
                IsBackground = false
            )
        runThread.Start()

        printfn "Waiting for scene bounds"

        [
            Model.observable
            |> Observable.choose (fst >> function | Some (SetSceneBounds _) -> Some () | _ -> None)
            |> Observable.first

            Model.observable
            |> Observable.choose (fst >> function | Some (SetMousePosition _) -> Some () | _ -> None)
            |> Observable.first
        ]
        |> Observable.mergeSeq
        |> Observable.wait
        |> ignore

        printfn "Setup complete"

        communicationState <-
            Some {
                Disposable = webServerStopDisposable
                MessageSubject = controllerMsgs
            }

        ()

    let private sendMessage msg =
        match communicationState with
        | Some state ->
            state.MessageSubject.OnNext msg
        | None ->
            raise (GetItException "Connection to UI not set up. Consider calling `Game.ShowScene()` at the beginning.")

    let private sendMessageAndWaitForResponse msg responseFilter =
        let mutable response = None
        use waitHandle = new ManualResetEventSlim()
        use d =
            Model.observable
            |> Observable.choose responseFilter
            |> Observable.first
            |> Observable.subscribe (fun p ->
                response <- Some p
                waitHandle.Set()
            )

        sendMessage msg

        waitHandle.Wait()
        Option.get response

    let addPlayer playerData =
        let playerId = PlayerId.create ()
        sendMessage <| AddPlayer (playerId, playerData)
        Model.updateCurrent (fun m -> None, { m with Players = Map.add playerId playerData m.Players |> Player.sendToBack playerId })
        playerId

    let removePlayer playerId =
        sendMessage <| RemovePlayer playerId
        Model.updateCurrent (fun m -> None, { m with Players = Map.remove playerId m.Players })

    let setWindowTitle title =
        sendMessage <| SetWindowTitle title

    let setBackground background =
        sendMessage <| SetBackground background

    let clearScene () =
        sendMessage ClearScene

    let makeScreenshot () =
        sendMessageAndWaitForResponse
            MakeScreenshot
            (fst >> function | Some (Screenshot image) -> Some image | _ -> None)

    let startBatch () =
        sendMessage StartBatch

    let applyBatch () =
        sendMessage ApplyBatch

    let setPosition playerId position =
        SetPosition (playerId, position)
        |> sendMessage
        Model.updatePlayer playerId (fun p -> None, { p with Position = position })

    let changePosition playerId relativePosition =
        ChangePosition (playerId, relativePosition)
        |> sendMessage
        Model.updatePlayer playerId (fun p -> None, { p with Position = p.Position + relativePosition })

    let setDirection playerId direction =
        SetDirection (playerId, direction)
        |> sendMessage
        Model.updatePlayer playerId (fun p -> None, { p with Direction = direction })

    let changeDirection playerId relativeDirection =
        ChangeDirection (playerId, relativeDirection)
        |> sendMessage
        Model.updatePlayer playerId (fun p -> None, { p with Direction = p.Direction + relativeDirection })

    let say playerId text =
        SetSpeechBubble (playerId, Some (Say text))
        |> sendMessage
        Model.updatePlayer playerId (fun p -> None, { p with SpeechBubble = Some (Say text) })

    let private setTemporarySpeechBubble playerId speechBubble =
        Model.updatePlayer playerId (fun p -> None, { p with SpeechBubble = Some speechBubble })
        Disposable.create (fun () ->
            Model.updatePlayer playerId (fun p -> None, { p with SpeechBubble = None })
        )

    let askString playerId text =
        use d = setTemporarySpeechBubble playerId (AskString text)
        sendMessageAndWaitForResponse
            (SetSpeechBubble (playerId, Some (AskString text)))
            (fst >> function | Some (AnswerStringQuestion (pId, answer)) when pId = playerId -> Some answer | _ -> None)

    let askBool playerId text =
        use d = setTemporarySpeechBubble playerId (AskBool text)
        sendMessageAndWaitForResponse
            (SetSpeechBubble (playerId, Some (AskBool text)))
            (fst >> function | Some (AnswerBoolQuestion (pId, answer)) when pId = playerId -> Some answer | _ -> None)

    let shutUp playerId =
        SetSpeechBubble (playerId, None)
        |> sendMessage
        Model.updatePlayer playerId (fun p -> None, { p with SpeechBubble = None })

    let setPenState playerId isOn =
        SetPenState (playerId, isOn)
        |> sendMessage
        Model.updatePlayer playerId (fun p -> None, { p with Pen = { p.Pen with IsOn = isOn } })

    let togglePenState playerId =
        TogglePenState playerId
        |> sendMessage
        Model.updatePlayer playerId (fun p -> None, { p with Pen = { p.Pen with IsOn = not p.Pen.IsOn } })

    let setPenColor playerId color =
        SetPenColor (playerId, color)
        |> sendMessage
        Model.updatePlayer playerId (fun p -> None, { p with Pen = { p.Pen with Color = color } })

    let shiftPenColor playerId angle =
        ShiftPenColor (playerId, angle)
        |> sendMessage
        Model.updatePlayer playerId (fun p -> None, { p with Pen = { p.Pen with Color = Color.hueShift angle p.Pen.Color } })

    let setPenWeight playerId weight =
        SetPenWeight (playerId, weight)
        |> sendMessage
        Model.updatePlayer playerId (fun p -> None, { p with Pen = { p.Pen with Weight = weight } })

    let changePenWeight playerId weight =
        ChangePenWeight (playerId, weight)
        |> sendMessage
        Model.updatePlayer playerId (fun p -> None, { p with Pen = { p.Pen with Weight = p.Pen.Weight + weight } })

    let setSizeFactor playerId sizeFactor =
        SetSizeFactor (playerId, sizeFactor)
        |> sendMessage
        Model.updatePlayer playerId (fun p -> None, { p with SizeFactor = sizeFactor })

    let changeSizeFactor playerId sizeFactor =
        ChangeSizeFactor (playerId, sizeFactor)
        |> sendMessage
        Model.updatePlayer playerId (fun p -> None, { p with SizeFactor = p.SizeFactor + sizeFactor })

    let setNextCostume playerId =
        SetNextCostume playerId
        |> sendMessage
        Model.updatePlayer playerId (fun p -> None, Player.nextCostume p)

    let sendToBack playerId =
        SendToBack playerId
        |> sendMessage
        Model.updateCurrent (fun m -> None, { m with Players = Player.sendToBack playerId m.Players })

    let bringToFront playerId =
        BringToFront playerId
        |> sendMessage
        Model.updateCurrent (fun m -> None, { m with Players = Player.bringToFront playerId m.Players })

    let setVisibility playerId isVisible =
        SetVisibility (playerId, isVisible)
        |> sendMessage
        Model.updatePlayer playerId (fun p -> None, { p with IsVisible = isVisible })

    let toggleVisibility playerId =
        ToggleVisibility playerId
        |> sendMessage
        Model.updatePlayer playerId (fun p -> None, { p with IsVisible = not p.IsVisible })
