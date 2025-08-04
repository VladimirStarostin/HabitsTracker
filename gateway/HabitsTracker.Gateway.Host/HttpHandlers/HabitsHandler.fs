module HabitsTracker.Gateway.Host.HabitsHandler

open System

open Giraffe

open HabitsTracker.Helpers
open HabitsTracker.Gateway.Host.Infrastructure.Clients

module Api = HabitsTracker.Gateway.Api.V1.Habits

let mapFromDtoToApi (dbHabit: HabitDto) : Api.Habit =
    { Id = dbHabit.Id
      Name = dbHabit.Name
      Description =
        dbHabit.Description
        |> Option.defaultValue String.Empty }

// GET /habit/{habitId}
let getHabitById (habitId: int) : HttpHandler =
    fun next ctx ->
        task {
            let client = ctx.GetService<IHabitsClient> ()

            match! habitId |> List.singleton |> client.GetByIdsAsync with
            | [ single ] -> return! json (mapFromDtoToApi single) next ctx
            | _ -> return! RequestErrors.NOT_FOUND $"Habit not found: {habitId}" next ctx
        }

// GET /habit/{userId}
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

// POST /habit/
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

// DELETE /habit/{habitId}
let deleteHabit (habitId: int) : HttpHandler =
    fun next ctx ->
        task {
            let client = ctx.GetService<IHabitsClient> ()
            let! deletedCount = client.DeleteAsync habitId

            return!
                if deletedCount = 1 then
                    Successful.OK () next ctx
                else
                    RequestErrors.NOT_FOUND $"Habit not found: {habitId}" next ctx
        }
