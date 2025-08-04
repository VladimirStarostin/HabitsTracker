module HabitsTracker.Gateway.Host.UsersHandler

open Giraffe

open HabitsTracker.Gateway.Host.Infrastructure.Clients

open HabitsTracker.Gateway.Api.V1

// DELETE /users/{userId}
let deleteUser (userId: int) : HttpHandler =
    fun next ctx ->
        task {
            let client = ctx.GetService<IUsersClient> ()

            let! numRowsDeleted = userId |> client.DeleteAsync

            if numRowsDeleted = 1 then
                return! Successful.OK () next ctx
            else
                return! RequestErrors.NOT_FOUND "" next ctx
        }
