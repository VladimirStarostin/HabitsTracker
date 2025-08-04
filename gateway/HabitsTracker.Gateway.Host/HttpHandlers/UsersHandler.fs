module HabitsTracker.Gateway.Host.UsersHandler

open System
open System.Security.Claims
open Microsoft.AspNetCore.Http
open Giraffe
open HabitsTracker.Helpers
open HabitsTracker.Gateway.Host.Domain.Services.GeneralService
open HabitsTracker.Gateway.Host.Infrastructure.Clients

open HabitsTracker.Gateway.Api.V1.Events

// GET /users/{userId}
let deleteUser (userId: int) : HttpHandler =
    fun next ctx ->
        task {
            let client = ctx.GetService<IHabitsClient> ()

            let! habits =
                userId
                |> Set.singleton
                |> client.GetByUserIdsAsync

            return! json habits next ctx
        }

let usersRoutes: HttpHandler =
    choose
        [ DELETE >=> routef "/users/%i" deleteUser ]
