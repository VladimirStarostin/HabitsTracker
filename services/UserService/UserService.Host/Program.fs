open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

open HabitsTracker.Helpers

open UserService.Host.Grpc
open UserService.Host.Domain.Common.Types

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder (args)

    let connectionString = builder.Configuration.GetConnectionString ("UserDb")

    builder.Services.AddSingleton<Security.IPasswordHasher>(Security.Pbkdf2PasswordHasher(150_000, 16, 32))
    |> ignore

    do
        builder.Services
            .AddGrpc()
            .Services.AddScoped<UserServiceImpl> (fun sp ->
                UserServiceImpl (
                    connectionString,
                    { Secret = "a_very_long_test_secret_key_for_hmacsha256"
                      Issuer = "test_issuer"
                      Audience = "test_audience"
                      AccessTokenValidityMinutes = 60
                      RefreshTokenValidityDays = 1 },
                    sp.GetRequiredService<Security.IPasswordHasher>()
                )
            )
        |> ignore

    let app = builder.Build ()
    do StatusEndpoint.add app |> ignore
    do app.MapGrpcService<UserServiceImpl> () |> ignore

    do MigrationRunner.runMigrations connectionString typeof<UserService.Migrations.CreateTablesMigration>.Assembly

    app.Run ()
    0
