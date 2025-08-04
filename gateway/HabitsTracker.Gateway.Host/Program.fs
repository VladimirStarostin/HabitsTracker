open System
open System.Text

open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.IdentityModel.Tokens

open Giraffe

open HabitsTracker.Gateway.Host.Infrastructure.Clients

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder (args)
    let config = builder.Configuration

    let jwtKey = config.["Jwt:Key"]
    let jwtIssuer = config.["Jwt:Issuer"]
    let jwtAudience = config.["Jwt:Audience"]
    let keyBytes = Encoding.UTF8.GetBytes (jwtKey)

    builder.Services.AddGiraffe () |> ignore

    builder.Services
        .AddSingleton<IHabitsTrackingClient, HabitsTrackingClient>()
        .AddSingleton<IUsersClient, UsersClient>()
        .AddSingleton<IHabitsClient, HabitsClient>()
        .AddSingleton<IAuthClient, AuthClient> ()
    |> ignore

    builder.Services.AddSwaggerGen () |> ignore

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(fun opts ->
            opts.RequireHttpsMetadata <- true
            opts.SaveToken <- true

            opts.TokenValidationParameters <-
                TokenValidationParameters (
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = SymmetricSecurityKey (keyBytes)
                )
        )
        .Services.AddAuthorization () |> ignore

    let app = builder.Build ()

    if app.Environment.IsDevelopment () then
        app.UseDeveloperExceptionPage () |> ignore

    app
        .UseSwagger()
        .UseSwaggerUI(fun c -> c.SwaggerEndpoint ("/swagger/v1/swagger.json", "HabitTracker API V1"))
        .UseAuthentication()
        .UseAuthorization()
        .UseGiraffe WebApp.webApp

    app.Run ()
    0
