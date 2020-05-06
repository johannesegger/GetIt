module PerfectScrollbar

open Fable.Core
open Fable.React
open Fable.Core.JsInterop

type PerfectScrollbarProps =
    | Id of string

let inline perfectScrollbar (props : PerfectScrollbarProps list) (elems : ReactElement list) : ReactElement =
    ofImport "default" "react-perfect-scrollbar" (keyValueList CaseRules.LowerFirst props) elems
