open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

open HabitsTracker.Helpers

open HabitsService.Host.Grpc

[<EntryPoint>]
let main args =
    do Dapper.addRequiredTypeHandlers ()
    let builder = WebApplication.CreateBuilder (args)

    let connectionString =
        builder.Configuration.GetConnectionString ("Main")

    do
        builder.Services
            .AddGrpc()
            .Services.AddScoped<HabitsServiceImpl> (fun _ -> new HabitsServiceImpl (connectionString))
        |> ignore

    do Logging.addLogging builder "HabitsService.Host"

    let app = builder.Build ()
    do StatusEndpoint.add app |> ignore
    do app.MapGrpcService<HabitsServiceImpl> () |> ignore

    do
        MigrationRunner.runMigrations connectionString typeof<HabitsService.Migrations.CreateTablesMigration>.Assembly

    app.Run ()
    0
