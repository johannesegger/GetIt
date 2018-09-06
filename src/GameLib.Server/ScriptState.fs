module GameLib.Server.ScriptState

open GameLib.Data.Server
open GameLib.Execution

let applyPlayerInstruction player = function
    | SetPositionInstruction position -> { player with Position = position }
    | SetDirectionInstruction direction -> { player with Direction = direction }
    | SayInstruction _ -> player
    | SetPenOnInstruction isOn -> { player with Pen = { player.Pen with IsOn = isOn } }
    | SetPenColorInstruction color -> { player with Pen = { player.Pen with Color = color } }
    | SetPenWeigthInstruction weight -> { player with Pen = { player.Pen with Weight = weight } }

let applySceneInstruction scene = function
    | ClearLinesInstruction -> scene

let applyInstruction state = function
    | PlayerInstruction instruction -> { state with Player = applyPlayerInstruction state.Player instruction }
    | SceneInstruction instruction -> { state with Scene = applySceneInstruction state.Scene instruction }