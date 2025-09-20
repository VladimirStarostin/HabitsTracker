open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

open HabitsTracker.Helpers

open TrackingService.Host.Grpc

[<EntryPoint>]
let main args =
    do Dapper.addRequiredTypeHandlers ()
    let builder = WebApplication.CreateBuilder (args)

    let connectionString = builder.Configuration.GetConnectionString ("TrackingDb")

    do
        builder.Services
            .AddGrpc()
            .Services.AddScoped<TrackingServiceImpl> (fun _ -> new TrackingServiceImpl (connectionString))
        |> ignore

    let app = builder.Build ()
    do StatusEndpoint.add app |> ignore

    do
        app.MapGrpcService<TrackingServiceImpl> ()
        |> ignore

    do
        MigrationRunner.runMigrations
            connectionString
            typeof<TrackingService.Migrations.CreateHabitEventsTableMigration>.Assembly

    app.Run ()
    0
