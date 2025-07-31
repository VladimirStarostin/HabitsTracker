open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

[<EntryPoint>]
let main args =

    let builder = WebApplication.CreateBuilder (args)
    builder.Services.AddControllers () |> ignore

    let app = builder.Build ()

    if app.Environment.IsDevelopment () then
        app.UseDeveloperExceptionPage () |> ignore

    app.UseRouting () |> ignore

    app.UseEndpoints (fun endpoints -> endpoints.MapControllers () |> ignore)
    |> ignore

    app.Run ()

    0
