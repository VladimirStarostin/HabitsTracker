[<RequireQualifiedAccessAttribute>]
module HabitsTracker.Helpers.StatusEndpoint

open System
open System.Reflection

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Routing

[<Literal>]
let defaultRoute = "/api/status"

let add (builder: IEndpointRouteBuilder) =
    builder.MapGet (
        defaultRoute,
        Func<_> (fun _ ->
            let assemblyName = Assembly.GetEntryAssembly().GetName ()

            {| Version = assemblyName.Version
               Name = assemblyName.Name |}
        )
    )
    |> ignore

    builder
