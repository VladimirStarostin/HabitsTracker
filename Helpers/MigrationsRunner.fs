[<RequireQualifiedAccess>]
module HabitsTracker.Helpers.MigrationRunner

open System.Reflection

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

open FluentMigrator.Runner

let runMigrations (connectionString: string) (migrationAssembly: Assembly) =
    if System.String.IsNullOrWhiteSpace connectionString then
        $"Connection string is null or white space : {connectionString}"
        |> System.ArgumentException
        |> raise

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
    let logger = scope.ServiceProvider.GetRequiredService<ILogger<unit>> ()
    let runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner> ()

    //do logger.LogInformation ("About to migrate up with connection string: {connectionString}", connectionString)
    do runner.MigrateUp ()
