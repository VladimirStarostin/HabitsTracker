module HabitsTracker.Helpers.StatusEndpoint

open System
open System.Reflection

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Routing

let add (builder: IEndpointRouteBuilder) =
    builder.MapGet(
        "api/status",
        Func<_>(fun _ ->
            let assemblyName = Assembly.GetEntryAssembly().GetName()

            {| Version = assemblyName.Version
               Name = assemblyName.Name |})
    )
    |> ignore

    builder
