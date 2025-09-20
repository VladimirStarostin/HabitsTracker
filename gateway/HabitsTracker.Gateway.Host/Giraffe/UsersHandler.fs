module HabitsTracker.Gateway.Host.Giraffe.UsersHandler

open Giraffe

open HabitsTracker.Gateway.Host.Domain.Services.HabitsService
open HabitsTracker.Gateway.Host.Domain.Services.UsersService
open HabitsTracker.Gateway.Host.Infrastructure.Clients

// DELETE /users/{userId}
let deleteUser (userId: int) : HttpHandler =
    fun next ctx ->
        task {
            let habitsClient = ctx.GetService<IHabitsClient> ()
            let trackingClient = ctx.GetService<IHabitsTrackingClient> ()
            let usersClient = ctx.GetService<IUsersClient> ()

            match! tryDeleteUserAsync userId habitsClient trackingClient usersClient with
            | DeleteUserPipelineResult.Ok deletedRowsCount -> return! Successful.OK deletedRowsCount next ctx
            | DeleteUserPipelineResult.Error error ->
                match error with
                | DeleteUserError.UserNotFound -> return! RequestErrors.NOT_FOUND "User not found" next ctx
                | DeleteUserError.GrpcError message ->
                    return! ServerErrors.INTERNAL_ERROR $"gRPC communication error: {message}" next ctx
                | DeleteUserError.HabitDeletion habitError ->
                    let reason =
                        match habitError.Error with
                        | DeleteHabitError.HabitNotFound -> "Habit was not found"
                        | DeleteHabitError.GrpcError msg -> $"gRPC communication error: {msg}"

                    return!
                        ServerErrors.INTERNAL_ERROR $"Failed to delete habit {habitError.HabitId}: {reason}" next ctx
        }
