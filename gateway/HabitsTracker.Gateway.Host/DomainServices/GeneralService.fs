module HabitsTracker.Gateway.Host.Domain.Services.GeneralService

open System
open System.Threading.Tasks

open HabitsTracker.Helpers.Pipelines

open HabitsTracker.Gateway.Host.Infrastructure.Clients
open HabitsTracker.Helpers

type CreateEventRequest =
    { HabitId: int
      UserId: int }

type CreatedEvent =
    { Id: int
      HabitId: int
      UserId: int
      Date: DateTime }

type CreateEventPipelineError =
    | GrpcError of string
    | UnexistentUser
    | UnexistingHabit

type CreateEventPipelineResult<'Ok> = PipelineResult<'Ok, CreateEventPipelineError>

let createEventPipeline = PipelineBuilder ()

module internal Steps =
    let checkHabitByIdAsync (habitId: int) (habitsClient: IHabitsClient) : Task<CreateEventPipelineResult<unit>> =
        task {
            match!
                habitId
                |> Set.singleton
                |> habitsClient.GetByIdsAsync
                |> Task.map Seq.tryExactlyOne
            with
            | Some _ -> return CreateEventPipelineResult.Ok ()
            | None -> return CreateEventPipelineResult.Error CreateEventPipelineError.UnexistingHabit
        }

    let checkUserByIdAsync (userId: int) (usersClient: IUsersClient) : Task<CreateEventPipelineResult<unit>> =
        task {
            match!
                userId
                |> Set.singleton
                |> usersClient.GetByIdsAsync
                |> Task.map Seq.tryExactlyOne
            with
            | Some _ -> return CreateEventPipelineResult.Ok ()
            | None -> return CreateEventPipelineResult.Error CreateEventPipelineError.UnexistentUser
        }

    let trackHabitAsync
        (event: CreateEventRequest)
        (habitsTrackingClient: IHabitsTrackingClient)
        : Task<CreateEventPipelineResult<CreatedEvent>> =
        task {
            let eventDate = DateTime.UtcNow

            let request: TrackHabitRequest =
                { Date = eventDate
                  HabitId = event.HabitId }

            let! response = habitsTrackingClient.TrackHabitAsync request

            return
                CreateEventPipelineResult.Ok
                    { Id = response.Id
                      HabitId = event.HabitId
                      UserId = event.UserId
                      Date = eventDate }
        }

let tryTrackHabitAsync
    (event: CreateEventRequest)
    (habitsClient: IHabitsClient)
    (usersClient: IUsersClient)
    (habitsTrackingClient: IHabitsTrackingClient)
    : Task<CreateEventPipelineResult<CreatedEvent>> =
    createEventPipeline {
        do! Steps.checkHabitByIdAsync event.HabitId habitsClient
        do! Steps.checkUserByIdAsync event.UserId usersClient
        return! Steps.trackHabitAsync event habitsTrackingClient
    }
