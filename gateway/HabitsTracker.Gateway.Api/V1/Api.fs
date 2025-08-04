module HabitsTracker.Gateway.Api.V1

open System

[<Literal>]
let Prefix = "/api/v1"

module Events =
    [<Literal>]
    let Resourse = Prefix + "/events"

    type CreateHabitEventParameters =
        { HabitId: int }

    type HabitEventInfo =
        { Id: int
          HabitId: int
          Date: DateTimeOffset }

module Users =
    [<Literal>]
    let Resourse = Prefix + "/users"

module Habits =
    [<Literal>]
    let private Resourse = Prefix + "/habits"

    [<Literal>]
    let GetHabitById = Resourse + "/%i"

    [<Literal>]
    let GetCurrentUserHabits = Resourse

    [<Literal>]
    let CreateHabit = Resourse

    [<Literal>]
    let DeleteHabitById = Resourse + "/%i"

    type CreateHabitParameters =
        { Name: string
          Description: string }

    type Habit =
        { Id: int
          Name: string
          Description: string }

module Auth =
    type RegisterParameters =
        { Name: string
          Email: string
          Password: string }

    type LoginParameters =
        { Email: string
          Password: string }

    type RefreshTokenParameters =
        { RefreshToken: string }

    type AuthResponse =
        { AccessToken: string
          RefreshToken: string }
