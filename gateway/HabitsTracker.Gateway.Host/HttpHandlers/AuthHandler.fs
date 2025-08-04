module HabitsTracker.Gateway.Host.AuthHandler

open Giraffe

open HabitsTracker.Gateway.Host.Infrastructure.Clients

module Api = HabitsTracker.Gateway.Api.V1.Auth

let mapFromDtoToApi (authResponse: AuthResponseDto) : Api.AuthResponse =
    { AccessToken = authResponse.AccessToken
      RefreshToken = authResponse.RefreshToken }

let registerHandler (model: Api.RegisterParameters) : HttpHandler =
    fun next ctx ->
        task {
            let client = ctx.GetService<IAuthClient> ()

            let! response =
                client.RegisterAsync
                    { Name = model.Name
                      Email = model.Email
                      Password = model.Password }

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
