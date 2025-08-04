type UserHostProgram = class end

open System.Text

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

open HabitsTracker.Helpers

open UserService.Host.Domain.Common.Types
open UserService.Host.Grpc
open System.Security.Cryptography

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

    let connectionString = builder.Configuration.GetConnectionString ("UserDb")

    builder.Logging.AddConsole().SetMinimumLevel (LogLevel.Warning)
    |> ignore

    let config = builder.Configuration

    if builder.Environment.IsDevelopment () then
        builder.Configuration.AddUserSecrets<UserHostProgram> ()
        |> ignore

    builder.Services.AddSingleton<Security.IPasswordHasher> (Security.Pbkdf2PasswordHasher (150_000, 16, 32))
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

    let app = builder.Build ()
    do StatusEndpoint.add app |> ignore
    do app.MapGrpcService<UserServiceImpl> () |> ignore

    do MigrationRunner.runMigrations connectionString typeof<UserService.Migrations.CreateTablesMigration>.Assembly

    app.Run ()
    0
