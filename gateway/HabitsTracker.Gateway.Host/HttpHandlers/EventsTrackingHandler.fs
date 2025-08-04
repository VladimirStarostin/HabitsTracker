module HabitsTracker.Gateway.Host.EventsTrackingHandler

open Microsoft.AspNetCore.Http

open Giraffe

open HabitsTracker.Helpers

module Api = HabitsTracker.Gateway.Api.V1.Events

open HabitsTracker.Gateway.Host.Domain.Services.GeneralService
open HabitsTracker.Gateway.Host.Infrastructure.Clients

let mapFromDtoToApi (event: HabitEventDto) : Api.HabitEventInfo =
    { Id = event.Id
      HabitId = event.HabitId
      Date = event.Date }

// GET /api/v1/events/{habitId}
let getEventsByHabitId (habitId: int) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let client = ctx.GetService<IHabitsTrackingClient> ()

            let! events =
                client.GetByHabitIdsAsync (List.singleton habitId)
                |> Task.map (List.map mapFromDtoToApi)

            return! json events next ctx
        }

// GET /api/v1/events/{eventId}
let getEventById (eventId: int) : HttpHandler =
    fun next ctx ->
        task {
            let client = ctx.GetService<IHabitsTrackingClient> ()

            match!
                client.GetByIdsAsync (List.singleton eventId)
                |> Task.map (List.map mapFromDtoToApi)
            with
            | [ singleton ] -> return! json singleton next ctx
            | _ -> return! RequestErrors.NOT_FOUND "Event not found" next ctx
        }

// DELETE /api/v1/events/{eventId}
let deleteEvent (eventId: int) : HttpHandler =
    fun next ctx ->
        task {
            let client = ctx.GetService<IHabitsTrackingClient> ()
            let! numDeletedRows = client.DeleteAsync eventId

            return!
                if numDeletedRows = 1 then
                    Successful.OK () next ctx
                else
                    RequestErrors.NOT_FOUND $"Unexpected rows count deleted : {numDeletedRows}" next ctx
        }

// POST /api/v1/events/{habitId}
let createEvent (userId: int) (habitId: int) : HttpHandler =
    fun next ctx ->
        task {
            let habitsTrackingClient = ctx.GetService<IHabitsTrackingClient> ()
            let usersClient = ctx.GetService<IUsersClient> ()
            let habitsClient = ctx.GetService<IHabitsClient> ()

            match!
                tryTrackHabitAsync
                    { UserId = userId
                      HabitId = habitId }
                    habitsClient
                    usersClient
                    habitsTrackingClient
            with
            | CreateEventPipelineResult.Ok createdEvent ->
                let loc = $"/api/v1/events/{createdEvent.Id}"
                ctx.SetHttpHeader ("Location", loc)

                let habitEvent =
                    { Id = createdEvent.Id
                      HabitId = createdEvent.HabitId
                      Date = createdEvent.Date }

                return! Successful.CREATED habitEvent next ctx
            | CreateEventPipelineResult.Error error ->
                match error with
                | CreateEventPipelineError.UnexistentUser ->
                    return! RequestErrors.NOT_FOUND $"User was not found: {userId}" next ctx
                | CreateEventPipelineError.UnexistingHabit ->
                    return! RequestErrors.NOT_FOUND $"Habit was not found: {habitId}" next ctx
                | CreateEventPipelineError.GrpcError err ->
                    return! ServerErrors.INTERNAL_ERROR $"gRPC communication error: {err}" next ctx
        }
