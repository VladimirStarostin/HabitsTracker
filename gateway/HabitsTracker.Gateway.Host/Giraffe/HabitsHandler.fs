module HabitsTracker.Gateway.Host.Giraffe.HabitsHandler

open System

open Giraffe

open HabitsTracker.Helpers
open HabitsTracker.Gateway.Host.Infrastructure.Clients
open HabitsTracker.Gateway.Host.Domain.Services.HabitsService

module Api = HabitsTracker.Gateway.Api.V1.Habits

let mapFromDtoToApi (dbHabit: HabitDto) : Api.Habit =
    { Id = dbHabit.Id
      Name = dbHabit.Name
      Description =
        dbHabit.Description
        |> Option.defaultValue String.Empty }

let getHabitById (habitId: int) : HttpHandler =
    fun next ctx ->
        task {
            let client = ctx.GetService<IHabitsClient> ()

            match! habitId |> List.singleton |> client.GetByIdsAsync with
            | [ single ] -> return! json (mapFromDtoToApi single) next ctx
            | _ -> return! RequestErrors.NOT_FOUND $"Habit not found: {habitId}" next ctx
        }

let getUserHabits (userId: int) : HttpHandler =
    fun next ctx ->
        task {
            let client = ctx.GetService<IHabitsClient> ()

            let! habits =
                userId
                |> List.singleton
                |> client.GetByUserIdsAsync
                |> Task.map (fun dbHabits -> List.map mapFromDtoToApi dbHabits)

            return! json habits next ctx
        }

let createHabit (userId: int) : HttpHandler =
    fun next ctx ->
        task {
            let! parameters = ctx.BindJsonAsync<Api.CreateHabitParameters> ()
            let client = ctx.GetService<IHabitsClient> ()

            let! createdHabit =
                client.CreateAsync
                    { Name = parameters.Name
                      Description =
                        (if String.IsNullOrWhiteSpace parameters.Description then
                             None
                         else
                             Some parameters.Description)
                      OwnerUserId = userId }

            return! json (mapFromDtoToApi createdHabit) next ctx
        }

let deleteHabit (userId: int) (habitId: int) : HttpHandler =
    fun next ctx ->
        task {
            let habitsClient = ctx.GetService<IHabitsClient> ()
            let trackingClient = ctx.GetService<IHabitsTrackingClient> ()

            match! tryDeleteHabitAsync habitId userId habitsClient trackingClient with
            | DeleteHabitPipelineResult.Ok deletedCount -> return! Successful.OK deletedCount next ctx
            | DeleteHabitPipelineResult.Error DeleteHabitError.HabitNotFound ->
                return! RequestErrors.NOT_FOUND $"Habit not found: {habitId}" next ctx
            | DeleteHabitPipelineResult.Error (DeleteHabitError.GrpcError err) ->
                return! ServerErrors.INTERNAL_ERROR $"gRPC communication error: {err}" next ctx
        }
