module HabitsTracker.Gateway.Host.Giraffe.AuthHandler

open Giraffe

open HabitsTracker.Gateway.Host.Infrastructure.Clients

module Api = HabitsTracker.Gateway.Api.V1.Auth

let mapFromDtoToApi (authResponse: AuthResponseDto) : Api.AuthResponse =
    { AccessToken = authResponse.AccessToken
      RefreshToken = authResponse.RefreshToken }

let registerHandler (registrationParams: Api.RegisterParameters) : HttpHandler =
    fun next ctx ->
        task {
            let client = ctx.GetService<IAuthClient> ()

            let! response =
                client.RegisterAsync
                    { Name = registrationParams.Name
                      Email = registrationParams.Email
                      Password = registrationParams.Password }

            return! json (mapFromDtoToApi response) next ctx
        }

let loginHandler (loginParams: Api.LoginParameters) : HttpHandler =
    fun next ctx ->
        task {
            let client = ctx.GetService<IAuthClient> ()

            let! response =
                client.LoginAsync
                    { Email = loginParams.Email
                      Password = loginParams.Password }

            return! json (mapFromDtoToApi response) next ctx
        }

let refreshTokenHandler (model: Api.RefreshTokenParameters) : HttpHandler =
    fun next ctx ->
        task {
            let client = ctx.GetService<IAuthClient> ()
            let! response = client.RefreshTokenAsync { RefreshToken = model.RefreshToken }
            return! json (mapFromDtoToApi response) next ctx
        }
