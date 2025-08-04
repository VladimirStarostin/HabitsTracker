type Program = class end

open System
open System.Text

open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.IdentityModel.Tokens

open Giraffe

open HabitsService.V1
open TrackingService.V1
open UserService.V1

open HabitsTracker.Gateway.Host.Infrastructure.Clients

let configureServices (builder: WebApplicationBuilder) : WebApplicationBuilder =
    let config = builder.Configuration

    let jwtKey = config.["Jwt:Key"]
    let jwtIssuer = config.["Jwt:Issuer"]
    let jwtAudience = config.["Jwt:Audience"]
    let keyBytes = Encoding.UTF8.GetBytes (jwtKey)

    builder.Services.AddGiraffe () |> ignore

    builder.Services
        .AddScoped<IHabitsTrackingClient, HabitsTrackingClient>()
        .AddScoped<IUsersClient, UsersClient>()
        .AddScoped<IHabitsClient, HabitsClient>()
        .AddScoped<IAuthClient, AuthClient> ()
    |> ignore

    builder.Services.AddGrpcClient<TrackingService.TrackingServiceClient> (fun opts ->
        opts.Address <-
            config.["GrpcOptions:HabitsTrackingServerAddress"]
            |> Uri

        ()
    )
    |> ignore

    builder.Services.AddGrpcClient<HabitsService.HabitsServiceClient> (fun opts ->
        opts.Address <- config.["GrpcOptions:HabitsServerAddress"] |> Uri

        ()
    )
    |> ignore

    builder.Services.AddGrpcClient<UserService.UserServiceClient> (fun opts ->
        opts.Address <- config.["GrpcOptions:UsersServerAddress"] |> Uri

        ()
    )
    |> ignore

    builder.Services.AddGrpcClient<HabitsTrackingClient> (fun opts ->
        opts.Address <- config.["GrpcOptions:UsersServerAddress"] |> Uri

        ()
    )
    |> ignore

    //builder.Services.AddSwaggerGen () |> ignore

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
        .Services.AddAuthorization ()
    |> ignore

    builder

let buildApplication (builder: WebApplicationBuilder) : WebApplication = builder.Build ()

let configureApplication (app: WebApplication) : WebApplication =
    if app.Environment.IsDevelopment () then
        app.UseDeveloperExceptionPage () |> ignore

    app
        //.UseSwagger()
        //.UseSwaggerUI(fun c -> c.SwaggerEndpoint ("/swagger/v1/swagger.json", "HabitTracker API V1"))
        .UseAuthentication()
        .UseAuthorization()
        .UseGiraffe (WebApp.webApp ())

    app

let startApplicaiton (app: WebApplication) : int =
    app.Run ()

    0

[<EntryPoint>]
let main args =
    args
    |> WebApplication.CreateBuilder
    |> configureServices
    |> buildApplication
    |> configureApplication
    |> startApplicaiton
