module internal HabitsTracker.Gateway.Host.Domain.Services.UsersService

open System
open System.Threading.Tasks

open HabitsTracker.Helpers.Pipelines

open HabitsTracker.Gateway.Host.Infrastructure.Clients
open HabitsTracker.Gateway.Host.Domain.Services.HabitsService

[<CLIMutable>]
type HabitDeletionError =
    { HabitId: int
      Error: DeleteHabitError }

type DeleteUserError =
    | HabitDeletion of HabitDeletionError
    | UserNotFound
    | GrpcError of string

type DeleteUserPipelineResult<'Ok> = PipelineResult<'Ok, DeleteUserError>

let deleteUserPipeline = PipelineBuilder ()

module internal Steps =
    let getUserHabitsAsync (userId: int) (habitsClient: IHabitsClient) : Task<DeleteUserPipelineResult<HabitDto list>> =
        task {
            try
                let! habits = habitsClient.GetByUserIdsAsync [ userId ]
                return DeleteUserPipelineResult.Ok habits
            with ex ->
                return DeleteUserPipelineResult.Error (DeleteUserError.GrpcError ex.Message)
        }

    let rec loop
        (remaining: HabitDto list)
        (userId: int)
        (habitsClient: IHabitsClient)
        (trackingClient: IHabitsTrackingClient)
        : Task<DeleteUserPipelineResult<unit>> =
        task {
            match remaining with
            | [] -> return DeleteUserPipelineResult.Ok ()
            | habit :: rest ->
                let! deletionResult = tryDeleteHabitAsync habit.Id userId habitsClient trackingClient

                match deletionResult with
                | DeleteHabitPipelineResult.Ok _ -> return! loop rest userId habitsClient trackingClient
                | DeleteHabitPipelineResult.Error err ->
                    return
                        DeleteUserPipelineResult.Error (
                            DeleteUserError.HabitDeletion
                                { HabitId = habit.Id
                                  Error = err }
                        )
        }

    let deleteHabitsAsync
        (userId: int)
        (habitsClient: IHabitsClient)
        (trackingClient: IHabitsTrackingClient)
        (habits: HabitDto list)
        : Task<DeleteUserPipelineResult<unit>> =
        task {
            try
                return! loop habits userId habitsClient trackingClient
            with ex ->
                return DeleteUserPipelineResult.Error (DeleteUserError.GrpcError ex.Message)
        }

    let deleteRefreshTokensAsync (userId: int) (usersClient: IUsersClient) : Task<DeleteUserPipelineResult<unit>> =
        task {
            try
                let! _ = usersClient.DeleteRefreshTokensAsync userId
                return DeleteUserPipelineResult.Ok ()
            with ex ->
                return DeleteUserPipelineResult.Error (DeleteUserError.GrpcError ex.Message)
        }

    let deleteUsersAccount (userId: int) (usersClient: IUsersClient) : Task<DeleteUserPipelineResult<int>> =
        task {
            try
                let! deleted = usersClient.DeleteUsersAsync userId

                return
                    if deleted = 1 then
                        DeleteUserPipelineResult.Ok deleted
                    else
                        DeleteUserPipelineResult.Error DeleteUserError.UserNotFound
            with ex ->
                return DeleteUserPipelineResult.Error (DeleteUserError.GrpcError ex.Message)
        }

let tryDeleteUserAsync
    (userId: int)
    (habitsClient: IHabitsClient)
    (trackingClient: IHabitsTrackingClient)
    (usersClient: IUsersClient)
    : Task<DeleteUserPipelineResult<int>> =
    deleteUserPipeline {
        let! habits = Steps.getUserHabitsAsync userId habitsClient
        do! Steps.deleteHabitsAsync userId habitsClient trackingClient habits
        do! Steps.deleteRefreshTokensAsync userId usersClient
        let! res = Steps.deleteUsersAccount userId usersClient
        return res
    }
