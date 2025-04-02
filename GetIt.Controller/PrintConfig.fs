namespace GetIt

open System
open System.Text.RegularExpressions
open Thoth.Json.Net

type TrayName = private TrayName of string with
    member this.Value with get () = let (TrayName name) = this in name
module TrayName =
    let tryParse (v: string) =
        if Regex.IsMatch(v, "^[A-Za-z0-9.\-_ ]+$") then Some (TrayName v)
        else None

type PrinterSettings = {
    Duplex: bool option
    Color: bool option
    SourceTrayName: TrayName option
}
module PrinterSettings =
    let standard = {
        Duplex = None
        Color = None
        SourceTrayName = None
    }

/// Configuration when printing screenshots
type PrintConfig =
    {
        TemplatePath: string
        TemplateParams: Map<string, string>
        PrinterName: string
        PrinterSettings: PrinterSettings
    }
    with
        static member Create (templatePath, printerName) =
            {
                TemplatePath = templatePath
                TemplateParams = Map.empty
                PrinterName = printerName
                PrinterSettings = PrinterSettings.standard
            }
        /// Read the print config from the environment.
        static member CreateFromEnvironment () =
            let decoder =
                Decode.object (fun get ->
                    {
                        TemplatePath = get.Required.Field "templatePath" Decode.string
                        TemplateParams =
                            let decoder =
                                Decode.list (Decode.tuple2 Decode.string Decode.string)
                                |> Decode.map Map.ofList
                            get.Optional.Field "templateParams" decoder
                            |> Option.defaultValue Map.empty
                        PrinterName = get.Required.Field "printerName" Decode.string
                        PrinterSettings =
                            let decoder =
                                Decode.object (fun get ->
                                    {
                                        Duplex = get.Optional.Field "duplex" Decode.bool
                                        Color = get.Optional.Field "color" Decode.bool
                                        SourceTrayName = get.Optional.Field "sourceTrayName" Decode.string |> Option.bind TrayName.tryParse
                                    }
                                )
                            get.Optional.Field "printerSettings" decoder
                            |> Option.defaultValue PrinterSettings.standard
                    }
                )
            let envVarName = "GET_IT_PRINT_CONFIG"
            let configString = Environment.GetEnvironmentVariable envVarName
            if isNull configString then raise (GetItException (sprintf "Can't read config from environment: Environment variable \"%s\" doesn't exist." envVarName))
            match Decode.fromString decoder configString with
            | Ok printConfig -> printConfig
            | Error e -> raise (GetItException (sprintf "Can't read config from environment: %s" e))

        /// Set the value of a template parameter.
        member this.Set (key, value) =
            { this with TemplateParams = Map.add key value this.TemplateParams }
