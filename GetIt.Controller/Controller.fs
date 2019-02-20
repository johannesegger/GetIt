namespace GetIt

open System
open System.Diagnostics
open System.IO
open System.IO.Pipes
open System.Threading
open Newtonsoft.Json

type EventHandler =
    | KeyDown of key: KeyboardKey option * handler: (KeyboardKey -> unit)
    | ClickScene of handler: (Position -> MouseButton -> unit)
    | ClickPlayer of playerId: PlayerId * handler: (unit -> unit)
    | MouseEnterPlayer of playerId: PlayerId * handler: (unit -> unit)

type Model =
    { SceneBounds: Rectangle
      Players: Map<PlayerId, PlayerData>
      MouseState: MouseState
      KeyboardState: KeyboardState
      EventHandlers: EventHandler list }

module Model =
    let mutable current =
        { SceneBounds = { Position = Position.zero; Size = { Width = 0.; Height = 0. } }
          Players = Map.empty
          MouseState = MouseState.empty
          KeyboardState = KeyboardState.empty
          EventHandlers = [] }

module internal InterProcessCommunication =
    let private uiProcess =
        lazy (
            // TODO determine UI technology based on host OS
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
                ProcessStartInfo("GetIt.WPF.exe")
#endif
            
            let proc = Process.Start(startInfo)

            let pipeClient = new NamedPipeClientStream(".", "GetIt", PipeDirection.InOut)
            pipeClient.Connect()

            (new StreamWriter(pipeClient), new StreamReader(pipeClient))
        )

    let serializerSettings = JsonSerializerSettings(Formatting = Formatting.None)

    let private applyMessage model message =
        match message with
        | InitializedScene sceneBounds -> { model with SceneBounds = sceneBounds }
        | PlayerAdded (playerId, player) -> { model with Players = Map.add playerId player model.Players }
        | PlayerRemoved playerId ->
            { model with Players = Map.remove playerId model.Players }
        | PositionSet (playerId, position) ->
            let player = Map.find playerId model.Players
            let player' = { player with Position = position }
            { model with Players = Map.add playerId player' model.Players }
        | DirectionSet (playerId, angle) ->
            let player = Map.find playerId model.Players
            let player' = { player with Direction = angle }
            { model with Players = Map.add playerId player' model.Players }

    let sendCommands (commands: RequestMsg list) =
        let (pipeWriter, pipeReader) = uiProcess.Force()

        let line = JsonConvert.SerializeObject(commands, serializerSettings)
        pipeWriter.WriteLine(line)
        pipeWriter.Flush()

        let line = pipeReader.ReadLine()
        let messages = JsonConvert.DeserializeObject<ResponseMsg list>(line, serializerSettings)

        Model.current <- List.fold applyMessage Model.current messages

type Player(playerId) =
    let mutable isDisposed = 0

    member internal x.PlayerId with get() = playerId
    member private x.Player with get() = Map.find playerId Model.current.Players
    /// <summary>
    /// The actual size of the player.
    /// </summary>
    member x.Size with get() = x.Player.Size

    /// <summary>
    /// The factor that is used to resize the player.
    /// </summary>
    member x.SizeFactor with get() = x.Player.SizeFactor

    /// <summary>
    /// The position of the player.
    /// </summary>
    member x.Position with get() = x.Player.Position

    /// <summary>
    /// The actual bounds of the player.
    /// </summary>
    member x.Bounds with get() = x.Player.Bounds

    /// <summary>
    /// The direction of the player.
    /// </summary>
    member x.Direction with get() = x.Player.Direction

    /// <summary>
    /// The pen of the player.
    /// </summary>
    member x.Pen with get() = x.Player.Pen

    interface IDisposable with
        member x.Dispose() =
            if Interlocked.Exchange(ref isDisposed, 1) = 0
            then
                InterProcessCommunication.sendCommands [ RemovePlayer playerId ]

module Game =
    let mutable defaultTurtle = None // TODO reset to `None` when disposed

    [<CompiledName("ShowSceneAndAddTurtle")>]
    let showSceneAndAddTurtle() =
        InterProcessCommunication.sendCommands [ ShowScene; AddPlayer Player.turtle ]
        defaultTurtle <-
            Map.toSeq Model.current.Players
            |> Seq.tryHead
            |> Option.map (fst >> (fun playerId -> new Player(playerId)))
