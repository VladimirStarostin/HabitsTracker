[<RequireQualifiedAccess>]
module HabitsTracker.Helpers.MigrationRunner

open System.Reflection

open Microsoft.Extensions.DependencyInjection

open FluentMigrator.Runner

let runMigrations (connectionString: string) (migrationAssembly: Assembly) =
    let serviceProvider =
        ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(fun builder ->
                builder
                    .AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(migrationAssembly)
                    .For.Migrations ()
                |> ignore
            )
            .AddLogging(fun lb -> lb.AddFluentMigratorConsole () |> ignore)
            .BuildServiceProvider ()

    use scope = serviceProvider.CreateScope ()
    let runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner> ()
    runner.MigrateUp ()
