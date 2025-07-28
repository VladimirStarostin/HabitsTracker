open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting

open HabitsTracker.Helpers

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    let app = builder.Build()
    do StatusEndpoint.add app |> ignore
    MigrationRunner.runMigrations "Data Source=users.db" typeof<UserService.Migrations.CreateTablesMigration>.Assembly

    app.Run()
    0
