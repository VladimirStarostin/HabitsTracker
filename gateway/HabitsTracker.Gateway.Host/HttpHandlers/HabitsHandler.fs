module HabitsTracker.Gateway.Host.HabitsHandler

open System
open System.Security.Claims
open Microsoft.AspNetCore.Http
open Giraffe
open HabitsTracker.Helpers
open HabitsTracker.Gateway.Host.Domain.Services.GeneralService
open HabitsTracker.Gateway.Host.Infrastructure.Clients

open HabitsTracker.Gateway.Api.V1.Events

// GET /habit/{habitId}
let getHabitById (habitId: int) : HttpHandler =
    fun next ctx ->
        task {
            let client = ctx.GetService<IHabitsClient>()
            match!
                habitId
                |> Set.singleton
                |> client.GetByIdsAsync
                |> Task.map Set.toList
            with
            | [ single ] ->
                return! json single next ctx
            | _ ->
                return! RequestErrors.NOT_FOUND $"Habit not found: {habitId}" next ctx
        }

// GET /habit/{userId}
let getUserHabits (userId: int) : HttpHandler =
    fun next ctx ->
        task {
            let client = ctx.GetService<IHabitsClient>()
            let! habits = userId |> Set.singleton |> client.GetByUserIdsAsync 
            return! json habits next ctx
        }

// POST /habit/
let createHabit (userId: int) : HttpHandler =
    fun next ctx ->
        task {
            let! parameters = ctx.BindJsonAsync<CreateHabitParameters>()
            let client = ctx.GetService<IHabitsClient>()
            let! response = client.CreateAsync { Name = parameters.Name; Description = (if String.IsNullOrWhiteSpace parameters.Description then None else Some parameters.Description); OwnerUserId = userId }
            let createdHabit: CreatedHabit =
                { Id = response.Id
                  Name = response.Name
                  Description = response.Description |> Option.defaultValue String.Empty }
            return! json createdHabit next ctx
        }

// DELETE /habit/{habitId}
let deleteHabit (habitId: int) : HttpHandler =
    fun next ctx ->
        task {
            let client = ctx.GetService<IHabitsClient>()
            let! deletedCount = client.DeleteAsync habitId
            return!
                if deletedCount = 1 then
                    Successful.OK () next ctx
                else
                    RequestErrors.NOT_FOUND $"Habit not found: {habitId}" next ctx
        }

let habitsRoutes: HttpHandler =
    choose
        [ GET    >=> routef "/habit/%i" getHabitById
          GET    >=> routef "/habit/%i" getUserHabits
          POST   >=> routef "/habit/%i" createHabit
          DELETE >=> routef "/habit/%i" deleteHabit ]
