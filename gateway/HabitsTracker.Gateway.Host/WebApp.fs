module WebApp

open Microsoft.AspNetCore.Authentication.JwtBearer

open Giraffe

open HabitsTracker.Gateway.Host
open HabitsTracker.Gateway.Host.AuthHandler
open HabitsTracker.Gateway.Host.EventsTrackingHandler
open HabitsTracker.Gateway.Host.HabitsHandler
open HabitsTracker.Gateway.Host.UsersHandler

module Api = HabitsTracker.Gateway.Api.V1

let usersRoutes: HttpHandler =
    choose
        [ DELETE
          >=> route Api.Users.Resourse
          >=> Common.requireUserId (fun userId -> deleteUser userId) ]

let habitsRoutes: HttpHandler =
    choose
        [ GET
          >=> routef Api.Habits.GetHabitById getHabitById
          GET
          >=> route Api.Habits.GetUserHabits
          >=> Common.requireUserId (fun userId -> getUserHabits userId)
          POST
          >=> route Api.Habits.CreateHabit
          >=> Common.requireUserId (fun userId -> createHabit userId)
          DELETE
          >=> routef Api.Habits.DeleteHabitById deleteHabit ]

let eventsRoutes: HttpHandler =
    choose
        [ GET
          >=> routef Api.Events.GetHabitEventsByHabitId getEventsByHabitId
          GET
          >=> routef Api.Events.GetHabitEventById getEventById
          POST
          >=> routef
              Api.Events.CreateHabitEvent
              (fun habitId -> Common.requireUserId (fun userId -> createEvent userId habitId))
          DELETE
          >=> routef Api.Events.DeleteHabitEventById deleteEvent ]

let apiRoutes: HttpHandler =
    choose
        [ eventsRoutes
          habitsRoutes
          usersRoutes ]

let authRoutes =
    subRoute
        "/auth"
        (choose
            [ POST
              >=> route "/login"
              >=> bindJson<Api.Auth.LoginParameters> loginHandler
              POST
              >=> route "/register"
              >=> bindJson<Api.Auth.RegisterParameters> registerHandler
              POST
              >=> route "/refresh"
              >=> bindJson<Api.Auth.RefreshTokenParameters> refreshTokenHandler ])

let mustBeAuth =
    requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

let routeExac () = routex @"^/api/v1/auth/login$"

let webApp () =
    subRoute
        "/api/v1"
        (choose
            [ authRoutes
              mustBeAuth >=> apiRoutes
              setStatusCode 404 >=> text "Not Found" ])
