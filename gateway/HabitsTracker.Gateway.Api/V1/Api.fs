module HabitsTracker.Gateway.Api.V1

open System

module Events =
    type CreateHabitEventParameters =
        { HabitId: int }

    type HabitEventInfo =
        { Id: int
          HabitId: int
          Date: DateTimeOffset }

module Habits =
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
