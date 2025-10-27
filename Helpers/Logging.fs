[<RequireQualifiedAccess>]
module HabitsTracker.Helpers.Logging

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Logging

let addLogging (builder: WebApplicationBuilder) (namespaceName: string) =
    builder.Logging.ClearProviders () |> ignore
    builder.Logging.AddConsole () |> ignore

    builder.Logging.AddFilter ("Microsoft.AspNetCore", LogLevel.Warning)
    |> ignore

    builder.Logging.AddFilter ("System", LogLevel.Warning)
    |> ignore

    builder.Logging.AddFilter (namespaceName, LogLevel.Information)
    |> ignore
