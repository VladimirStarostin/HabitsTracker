module WebApp
    
open Microsoft.AspNetCore.Authentication.JwtBearer

open Giraffe

open HabitsTracker.Gateway.Api.V1

open HabitsTracker.Gateway.Host.EventsTrackingHandler
open HabitsTracker.Gateway.Host.HabitsHandler
open HabitsTracker.Gateway.Host.UsersHandler

let apiRoutes: HttpHandler =
    subRouteCi Prefix (choose [ eventsRoutes; habitsRoutes; usersRoutes ])

let webApp: HttpHandler =
    choose
        [ requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)
          >=> apiRoutes
          setStatusCode 404 >=> text "Not Found" ]
