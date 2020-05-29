namespace GetIt

open System
open Thoth.Json.Net

/// Configuration when printing screenshots
type PrintConfig =
    {
        TemplatePath: string
        TemplateParams: Map<string, string>
        PrinterName: string
    }
    with
        static member Create (templatePath, printerName) =
            {
                TemplatePath = templatePath
                TemplateParams = Map.empty
                PrinterName = printerName
            }
        /// Read the print config from the environment.
        static member CreateFromEnvironment () =
            let decoder =
                Decode.object (fun get ->
                    let templateParamsDecoder =
                        Decode.list (Decode.tuple2 Decode.string Decode.string)
                        |> Decode.map Map.ofList
                    {
                        TemplatePath = get.Required.Field "templatePath" Decode.string
                        TemplateParams =
                            get.Optional.Field "templateParams" templateParamsDecoder
                            |> Option.defaultValue Map.empty
                        PrinterName = get.Required.Field "printerName" Decode.string
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
