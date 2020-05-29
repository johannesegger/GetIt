[<AutoOpen>]
module Utils

open System
open System.IO
open System.Net.Http
open System.Text.RegularExpressions
open Polly
open GetIt

type SvgImage = {
    Width: float
    Height: float
    Data: string
}

module SvgImage =
    let fromSvgData content =
        let (width, height) = Svg.getSizeFromSvgDocument content
        {
            Width = width
            Height = height
            Data = content
        }

    let fromSvgFile path =
        File.ReadAllText path
        |> fromSvgData

module String =
    let firstToUpper (text: string) =
        if String.IsNullOrEmpty text then text
        else sprintf "%c%s" (Char.ToUpper text.[0]) (text.Substring 1)

    let toCamelCase (name: string) =
        Regex.Replace(name, @"\s", "")
        |> fun s -> Regex.Replace(s, @"-(?<c>.)", fun m -> m.Groups.["c"].Value.ToUpper())

    let toPascalCase =
        toCamelCase >> firstToUpper

    let removeNewLines (text: string) =
        text.Replace("\r", "").Replace("\n", "")

module Http =
    let retryPolicy =
        let delays =
            Seq.initInfinite (float >> TimeSpan.FromSeconds)
            |> Seq.map (fun t -> if t.TotalSeconds < 10. then t else TimeSpan.FromSeconds(10.))
        Policy
            .HandleInner<HttpRequestException>()
            .WaitAndRetryAsync(delays)