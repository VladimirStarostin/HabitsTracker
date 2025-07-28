open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting

open HabitsTracker.Helpers

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    let app = builder.Build()
    do StatusEndpoint.add app |> ignore

    do
        MigrationRunner.runMigrations
            "Data Source=habits.db"
            typeof<HabitsService.Migrations.CreateTablesMigration>.Assembly

    app.Run()
    0
