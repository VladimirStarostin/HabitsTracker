open System
open System.Text
open System.Threading.Tasks

open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.IdentityModel.Logging
open Microsoft.IdentityModel.Tokens

open Giraffe

open HabitsService.V1
open TrackingService.V1
open UserService.V1

open HabitsTracker.Gateway.Host
open HabitsTracker.Gateway.Host.Infrastructure.Clients

type JwtOptions =
    { Secret: string
      Issuer: string
      Audience: string
      AccessTokenValidityMinutes: int
      RefreshTokenValidityDays: int }

let configureServices (builder: WebApplicationBuilder) : WebApplicationBuilder =
    let config = builder.Configuration

    builder.Logging.AddConsole().SetMinimumLevel (LogLevel.Warning)
    |> ignore

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

    if builder.Environment.IsDevelopment () then
        IdentityModelEventSource.ShowPII <- true

    let jwtKey = config.["Jwt:Secret"].TrimEnd ()
    let jwtIssuer = config.["Jwt:Issuer"].TrimEnd ()
    let jwtAudience = config.["Jwt:Audience"].TrimEnd ()
    let keyBytes = Encoding.UTF8.GetBytes (jwtKey)

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(fun opts ->
            opts.RequireHttpsMetadata <- true
            opts.SaveToken <- true

            opts.TokenValidationParameters <-
                TokenValidationParameters (
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    ClockSkew = TimeSpan.FromMinutes 5,
                    IssuerSigningKey = SymmetricSecurityKey (keyBytes)
                )

            let events = JwtBearerEvents ()

            events.OnAuthenticationFailed <-
                Func<AuthenticationFailedContext, Task> (fun ctx ->
                    let log = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<AuthenticationFailedContext>> ()
                    log.LogError (ctx.Exception, "JWT auth failed")
                    Task.CompletedTask
                )

            if builder.Environment.IsDevelopment () then
                events.OnChallenge <-
                    Func<JwtBearerChallengeContext, Task> (fun ctx ->
                        let log = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerChallengeContext>> ()
                        log.LogWarning ("JWT challenge: {Error} {Desc}", ctx.Error, ctx.ErrorDescription)
                        Task.CompletedTask
                    )

                events.OnMessageReceived <-
                    Func<MessageReceivedContext, Task> (fun ctx ->
                        let log = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<MessageReceivedContext>> ()

                        log.LogInformation (
                            "Authorization header present: {HasAuth}",
                            ctx.Request.Headers.ContainsKey "Authorization"
                        )

                        Task.CompletedTask
                    )

            opts.Events <- events
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
        .UseGiraffeErrorHandler(Giraffe.GrpcErrorHandler.errorHandler)
        .UseGiraffe (Giraffe.WebApp.webApp ())

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
