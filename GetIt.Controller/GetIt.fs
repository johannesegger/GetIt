namespace GetIt

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
      Players: Map<PlayerId, Player>
      MouseState: MouseState
      KeyboardState: KeyboardState
      EventHandlers: EventHandler list }

module Model =
    let mutable defaultTurtleId = None

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
            // startInfo.RedirectStandardOutput <- true
            // startInfo.RedirectStandardError <- true
            // startInfo.UseShellExecute <- false
            
            let proc = Process.Start(startInfo)
            // let d1 = proc.OutputDataReceived.Subscribe(fun data -> printfn "[UI] %s" data.Data)
            // let d2 = proc.ErrorDataReceived.Subscribe(fun data -> eprintfn "[UI] %s" data.Data)
            // proc.BeginOutputReadLine()
            // proc.BeginErrorReadLine()

            let pipeClient = new NamedPipeClientStream(".", "GetIt", PipeDirection.InOut)
            pipeClient.Connect()

            (new StreamWriter(pipeClient), new StreamReader(pipeClient))
        )

    let serializerSettings = JsonSerializerSettings(Formatting = Formatting.None)

    let private applyMessage model message =
        match message with
        | InitializedScene sceneBounds -> { model with SceneBounds = sceneBounds }
        | AddedPlayer (playerId, player) -> { model with Players = Map.add playerId player model.Players }
        | UpdatedPosition (playerId, position) ->
            let player = Map.find playerId model.Players
            let player' = { player with Position = position }
            { model with Players = Map.add playerId player' model.Players }

    let sendCommands (commands: RequestMsg list) =
        let (pipeWriter, pipeReader) = uiProcess.Force()

        let line = JsonConvert.SerializeObject(commands, serializerSettings)
        pipeWriter.WriteLine(line)
        pipeWriter.Flush()

        let line = pipeReader.ReadLine()
        let messages = JsonConvert.DeserializeObject<ResponseMsg list>(line, serializerSettings)

        Model.current <- List.fold applyMessage Model.current messages

module Game =
    [<CompiledName("ShowSceneAndAddTurtle")>]
    let showSceneAndAddTurtle() =
        InterProcessCommunication.sendCommands [ ShowScene; AddPlayer Player.turtle ]
        Model.defaultTurtleId <- Map.toSeq Model.current.Players |> Seq.map fst |> Seq.tryHead

module Turtle =
    let getTurtleIdOrFail () =
        match Model.defaultTurtleId with
        | Some v -> v
        | None -> failwith "Default player hasn't been added to the scene. Consider calling `Game.ShowSceneAndAddTurtle()` at the beginning."

    [<CompiledName("MoveTo")>]
    let moveTo x y =
        InterProcessCommunication.sendCommands [ MoveTo (getTurtleIdOrFail(), { X = x; Y = y }) ]
