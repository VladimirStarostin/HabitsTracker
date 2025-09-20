module HabitsTracker.Gateway.Host.Giraffe.WebApp

open Microsoft.AspNetCore.Authentication.JwtBearer

open Giraffe

open AuthHandler
open EventsTrackingHandler
open HabitsHandler
open UsersHandler

module Api = HabitsTracker.Gateway.Api.V1

let usersRoutes: HttpHandler =
    subRoute
        "/users"
        (choose
            [ DELETE
              >=> route ""
              >=> Common.requireUserId (fun userId -> deleteUser userId) ])

let habitsRoutes: HttpHandler =
    subRoute
        "/habits"
        (choose
            [ GET >=> routef "%i" getHabitById
              GET
              >=> route ""
              >=> Common.requireUserId (fun userId -> getUserHabits userId)
              POST
              >=> route ""
              >=> Common.requireUserId (fun userId -> createHabit userId)
              DELETE
              >=> routef "/%i" (fun habitId -> Common.requireUserId (fun userId -> deleteHabit userId habitId)) ])

let eventsRoutes: HttpHandler =
    subRoute
        "/events"
        (choose
            [ GET >=> routef "/%i" getEventsByHabitId
              GET >=> routef "/by-habit/%i" getEventById
              POST
              >=> bindJson<Api.Events.CreateHabitEventParameters> (fun parameters ->
                  Common.requireUserId (fun userId -> createEvent parameters userId)
              )
              DELETE
              >=> routef "/%i" (fun eventId -> Common.requireUserId (fun userId -> deleteEvent userId eventId)) ])

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

let webApp () =
    subRoute
        "/api/v1"
        (choose
            [ authRoutes
              mustBeAuth >=> apiRoutes
              setStatusCode 404 >=> text "Not Found" ])
