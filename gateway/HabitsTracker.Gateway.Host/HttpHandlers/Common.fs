module HabitsTracker.Gateway.Host.Common

open System
open System.Security.Claims

open Giraffe

let requireUserId (innerHandler: int -> HttpHandler) : HttpHandler =
    fun next ctx ->
        task {
            match ctx.User.FindFirst ClaimTypes.NameIdentifier with
            | null -> return! RequestErrors.BAD_REQUEST "Missing user id claim" next ctx
            | claim ->
                match Int32.TryParse claim.Value with
                | false, _ -> return! RequestErrors.BAD_REQUEST "Invalid user id format" next ctx
                | true, uid -> return! innerHandler uid next ctx
        }
