namespace HabitsTracker.Gateway.Api

open System

module V1 =
    [<Literal>]
    let Prefix = "/api/v1"

    module Events =
        [<Literal>]
        let Resourse = Prefix + "/events"

        [<Literal>]
        let GetHabitEventById = Resourse + "/%i"

        [<Literal>]
        let GetHabitEventsByHabitId = Resourse + "/%i"

        [<Literal>]
        let DeleteHabitEventById = Resourse + "/%i"

        [<Literal>]
        let CreateHabitEvent = Resourse + "/%i"

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
        let GetUserHabits = Resourse

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
        [<Literal>]
        let private Resourse = Prefix + "/auth"

        [<Literal>]
        let Login = Resourse + "/login"

        [<Literal>]
        let Register = Resourse + "/register"

        [<Literal>]
        let Refresh = Resourse + "/refresh"

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
