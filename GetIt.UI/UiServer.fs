namespace GetIt

open System
open System.Threading
open System.Threading.Tasks
open FSharp.Control.Reactive
open Google.Protobuf.WellKnownTypes
open Grpc.Core

type UIRequest =
    | UIRequestMsg of App.Msg
    | ShowScene of WindowSize
    | SetWindowTitle of string option
    | MakeScreenshot
    | MouseMoved of virtualScreenPosition: Position
    | MouseClicked of VirtualScreenMouseClick

type UIResponse =
    | UIResponseMsg of App.Msg * App.Model
    | Screenshot of PngImage

type UIServer(executeCommand: UIRequest -> unit, uiMessages: IObservable<UIResponse>) =
    inherit Ui.UI.UIBase()

    let empty = Empty()

    let sendAndWait request responseFn =
        async {
            use waitHandle = new ManualResetEventSlim()
            let mutable response = None
            use d =
                uiMessages
                |> Observable.choose responseFn
                |> Observable.first
                |> Observable.subscribe (fun r ->
                    response <- Some r
                    waitHandle.Set()
                )

            executeCommand request |> ignore

            waitHandle.Wait()

            return Option.get response
        }
        |> Async.StartAsTask

    override this.ShowScene(request: Ui.WindowSize, context: ServerCallContext) : Task<Ui.Rectangle> =
        sendAndWait
            (ShowScene (Message.WindowSize.ToDomain request))
            (function | UIResponseMsg (App.SetSceneBounds p, model) -> Some (Message.Rectangle.FromDomain p) | _ -> None)

    override this.SetWindowTitle(request: Ui.WindowTitle, context: ServerCallContext) : Task<Empty> =
        if String.IsNullOrWhiteSpace request.Title then None
        else Some request.Title
        |> SetWindowTitle
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.SetBackground(request: Ui.SvgImage, context: ServerCallContext) : Task<Empty> =
        Message.SvgImage.ToDomain request
        |> App.SetBackground
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.ClearScene(request: Empty, context: ServerCallContext) : Task<Empty> =
        App.ClearScene
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.MakeScreenshot (request: Empty, context: ServerCallContext) : Task<Ui.PngImage> =
        sendAndWait
            MakeScreenshot
            (function | Screenshot image -> Some (Message.PngImage.FromDomain image) | _ -> None)

    override this.AddPlayer (request: Ui.Player, context: ServerCallContext) : Task<Empty> =
        Message.Player.ToDomain request
        |> App.AddPlayer
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.RemovePlayer (request: Ui.PlayerId, context: ServerCallContext) : Task<Empty> =
        Message.PlayerId.ToDomain request
        |> App.RemovePlayer
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.SetPosition (request: Ui.PlayerPosition, context: ServerCallContext) : Task<Empty> =
        Message.PlayerPosition.ToDomain request
        |> App.SetPosition
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.ChangePosition (request: Ui.PlayerPosition, context: ServerCallContext) : Task<Empty> =
        Message.PlayerPosition.ToDomain request
        |> App.ChangePosition
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.SetDirection (request: Ui.PlayerDirection, context: ServerCallContext) : Task<Empty> =
        Message.PlayerDirection.ToDomain request
        |> App.SetDirection
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.ChangeDirection (request: Ui.PlayerDirection, context: ServerCallContext) : Task<Empty> =
        Message.PlayerDirection.ToDomain request
        |> App.ChangeDirection
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.Say (request: Ui.PlayerText, context: ServerCallContext) : Task<Empty> =
        Message.PlayerText.ToDomain request
        |> fun (playerId, text) -> playerId, Some (Say text)
        |> App.SetSpeechBubble
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.ShutUp (request: Ui.PlayerId, context: ServerCallContext) : Task<Empty> =
        Message.PlayerId.ToDomain request
        |> fun playerId -> playerId, None
        |> App.SetSpeechBubble
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.AskString (request: Ui.PlayerText, context: ServerCallContext) : Task<Ui.StringAnswer> =
        let (playerId, text) = Message.PlayerText.ToDomain request
        sendAndWait
            (UIRequestMsg (App.SetSpeechBubble (playerId, Some (AskString text))))
            (function | UIResponseMsg (App.ApplyStringAnswer (pId, answer), model) when pId = playerId -> Some (Message.StringAnswer.FromDomain answer) | _ -> None)

    override this.AskBool (request: Ui.PlayerText, context: ServerCallContext) : Task<Ui.BoolAnswer> =
        let (playerId, text) = Message.PlayerText.ToDomain request
        sendAndWait
            (UIRequestMsg (App.SetSpeechBubble (playerId, Some (AskBool text))))
            (function | UIResponseMsg (App.ApplyBoolAnswer (pId, answer), model) when pId = playerId -> Some (Message.BoolAnswer.FromDomain answer) | _ -> None)

    override this.SetPenState (request: Ui.PlayerPenState, context: ServerCallContext) : Task<Empty> =
        Message.PlayerPenState.ToDomain request
        |> App.SetPenState
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.TogglePenState (request: Ui.PlayerId, context: ServerCallContext) : Task<Empty> =
        Message.PlayerId.ToDomain request
        |> App.TogglePenState
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.SetPenColor (request: Ui.PlayerPenColor, context: ServerCallContext) : Task<Empty> =
        Message.PlayerPenColor.ToDomain request
        |> App.SetPenColor
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.ShiftPenColor (request: Ui.PlayerPenColorShift, context: ServerCallContext) : Task<Empty> =
        Message.PlayerPenColorShift.ToDomain request
        |> App.ShiftPenColor
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.SetPenWeight (request: Ui.PlayerPenWeight, context: ServerCallContext) : Task<Empty> =
        Message.PlayerPenWeight.ToDomain request
        |> App.SetPenWeight
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.ChangePenWeight (request: Ui.PlayerPenWeight, context: ServerCallContext) : Task<Empty> =
        Message.PlayerPenWeight.ToDomain request
        |> App.ChangePenWeight
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.SetSizeFactor (request: Ui.PlayerSizeFactor, context: ServerCallContext) : Task<Empty> =
        Message.PlayerSizeFactor.ToDomain request
        |> App.SetSizeFactor
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.ChangeSizeFactor (request: Ui.PlayerSizeFactor, context: ServerCallContext) : Task<Empty> =
        Message.PlayerSizeFactor.ToDomain request
        |> App.ChangeSizeFactor
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.SetVisibility (request: Ui.PlayerVisibility, context: ServerCallContext) : Task<Empty> =
        Message.PlayerVisibility.ToDomain request
        |> App.SetVisibility
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.SetNextCostume (request: Ui.PlayerId, context: ServerCallContext) : Task<Empty> =
        Message.PlayerId.ToDomain request
        |> App.SetNextCostume
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.SendToBack (request: Ui.PlayerId, context: ServerCallContext) : Task<Empty> =
        Message.PlayerId.ToDomain request
        |> App.SendToBack
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.BringToFront (request: Ui.PlayerId, context: ServerCallContext) : Task<Empty> =
        Message.PlayerId.ToDomain request
        |> App.BringToFront
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.MouseMoved (requestStream: IAsyncStreamReader<Ui.Position>, responseStream: IServerStreamWriter<Ui.Position>, context: ServerCallContext): Task =
        let rec iterate () = async {
            let! hasMore = requestStream.MoveNext(CancellationToken.None) |> Async.AwaitTask
            if hasMore then
                Message.Position.ToDomain requestStream.Current
                |> MouseMoved
                |> executeCommand
                |> ignore

                do! iterate ()
            else
                ()
        }

        async {
            use d =
                uiMessages
                |> Observable.choose (function
                    | UIResponseMsg (App.SetMousePosition positionRelativeToSceneControl, model) ->
                        let position =
                            {
                                X = model.SceneBounds.Left + positionRelativeToSceneControl.X
                                Y = model.SceneBounds.Top - positionRelativeToSceneControl.Y
                            }
                        Some (Message.Position.FromDomain position)
                    | _ -> None
                )
                |> Observable.map (fun position ->
                    async {
                        do!
                            responseStream.WriteAsync position
                            |> Async.AwaitTask
                    }
                    |> Observable.ofAsync
                )
                |> Observable.concatInner
                |> Observable.subscribe ignore

            do! iterate ()
        }
        |> Async.StartAsTask
        :> Task

    override this.MouseClicked (request: Ui.VirtualScreenMouseClick, context: ServerCallContext) : Task<Ui.MouseClick> =
        sendAndWait
            (MouseClicked (Message.VirtualScreenMouseClick.ToDomain request))
            (function
                | UIResponseMsg (App.ApplyMouseClick (mouseButton, positionRelativeToSceneControl), model) ->
                    let position =
                        {
                            X = model.SceneBounds.Left + positionRelativeToSceneControl.X
                            Y = model.SceneBounds.Top - positionRelativeToSceneControl.Y
                        }
                    let mouseClick =
                        { Button = mouseButton; Position = position }
                        |> Message.MouseClick.FromDomain
                    Some mouseClick
                | _ -> None
            )

    override this.StartBatch (request: Empty, context: ServerCallContext) : Task<Empty> =
        App.StartBatch
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.ApplyBatch (request: Empty, context: ServerCallContext) : Task<Empty> =
        App.ApplyBatch
        |> UIRequestMsg
        |> executeCommand
        |> ignore

        Task.FromResult empty

    override this.SceneBoundsChanged (request: Empty, responseStream: IServerStreamWriter<Ui.Rectangle>, context: ServerCallContext) : Task =
        async {
            use d =
                uiMessages
                |> Observable.choose (function
                    | UIResponseMsg (App.SetSceneBounds sceneBounds, model) -> Some (Message.Rectangle.FromDomain sceneBounds)
                    | _ -> None
                )
                |> Observable.map (fun sceneBounds ->
                    async {
                        do!
                            responseStream.WriteAsync sceneBounds
                            |> Async.AwaitTask
                    }
                    |> Observable.ofAsync
                )
                |> Observable.concatInner
                |> Observable.subscribe ignore

            use mre = new ManualResetEventSlim()
            do! Async.AwaitWaitHandle mre.WaitHandle |> Async.Ignore
        }
        |> Async.StartAsTask
        :> Task

