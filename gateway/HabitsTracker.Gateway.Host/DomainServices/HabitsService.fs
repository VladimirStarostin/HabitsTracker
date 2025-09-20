module internal HabitsTracker.Gateway.Host.Domain.Services.HabitsService

open System.Threading.Tasks

open HabitsTracker.Helpers.Pipelines
open HabitsTracker.Gateway.Host.Infrastructure.Clients

type DeleteHabitError =
    | HabitNotFound
    | GrpcError of string

type DeleteHabitPipelineResult<'Ok> = PipelineResult<'Ok, DeleteHabitError>

let deleteHabitPipeline = PipelineBuilder ()

module internal Steps =
    let getEventsByHabitIdAsync
        (habitId: int)
        (trackingClient: IHabitsTrackingClient)
        : Task<DeleteHabitPipelineResult<HabitEventDto list>> =
        task {
            let! events = trackingClient.GetByHabitIdsAsync [ habitId ]
            return DeleteHabitPipelineResult.Ok events
        }

    let deleteEventsAsync
        (userId: int)
        (trackingClient: IHabitsTrackingClient)
        (events: HabitEventDto list)
        : Task<DeleteHabitPipelineResult<unit>> =
        task {
            let! _ = trackingClient.DeleteAsync (events |> List.map _.Id) userId
            return DeleteHabitPipelineResult.Ok ()
        }

    let deleteHabitAsync
        (habitId: int)
        (userId: int)
        (habitsClient: IHabitsClient)
        : Task<DeleteHabitPipelineResult<int>> =
        task {
            let! deleted = habitsClient.DeleteAsync [ habitId ] userId

            return
                if deleted = 1 then
                    DeleteHabitPipelineResult.Ok deleted
                else
                    DeleteHabitPipelineResult.Error DeleteHabitError.HabitNotFound
        }

let tryDeleteHabitAsync
    (habitId: int)
    (userId: int)
    (habitsClient: IHabitsClient)
    (trackingClient: IHabitsTrackingClient)
    : Task<DeleteHabitPipelineResult<int>> =
    deleteHabitPipeline {
        let! events = Steps.getEventsByHabitIdAsync habitId trackingClient
        do! Steps.deleteEventsAsync userId trackingClient events
        return! Steps.deleteHabitAsync habitId userId habitsClient
    }
