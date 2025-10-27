open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

open HabitsTracker.Helpers

open UserService.Host.Domain.Common.Types
open UserService.Host.Grpc

type JwtOptions =
    { Secret: string
      Issuer: string
      Audience: string
      AccessTokenValidityMinutes: int
      RefreshTokenValidityDays: int }

[<EntryPoint>]
let main args =
    do Dapper.addRequiredTypeHandlers ()

    let builder = WebApplication.CreateBuilder (args)
    let config = builder.Configuration

    let connectionString = config.GetConnectionString ("Main")

    builder.Logging.AddConsole().SetMinimumLevel (LogLevel.Warning)
    |> ignore

    do
        (150_000, 16, 32)
        |> Security.Pbkdf2PasswordHasher
        |> builder.Services.AddSingleton<Security.IPasswordHasher>
        |> ignore

    let jwtKey = config.["Jwt:Secret"].TrimEnd ()
    let jwtIssuer = config.["Jwt:Issuer"].TrimEnd ()
    let jwtAudience = config.["Jwt:Audience"].TrimEnd ()
    let accessTokenValidityMinutes = (int) config.["Jwt:AccessTokenValidityMinutes"]
    let refreshTokenValidityDays = (int) config.["Jwt:RefreshTokenValidityDays"]

    do
        builder.Services
            .AddGrpc()
            .Services.AddScoped<UserServiceImpl> (fun sp ->
                UserServiceImpl (
                    connectionString,
                    { Secret = jwtKey
                      Issuer = jwtIssuer
                      Audience = jwtAudience
                      AccessTokenValidityMinutes = accessTokenValidityMinutes
                      RefreshTokenValidityDays = refreshTokenValidityDays },
                    sp.GetRequiredService<Security.IPasswordHasher> ()
                )
            )
        |> ignore

    do Logging.addLogging builder "UserService.Host"

    let app = builder.Build ()
    do StatusEndpoint.add app |> ignore
    do app.MapGrpcService<UserServiceImpl> () |> ignore

    do MigrationRunner.runMigrations connectionString typeof<UserService.Migrations.CreateTablesMigration>.Assembly

    app.Run ()
    0
