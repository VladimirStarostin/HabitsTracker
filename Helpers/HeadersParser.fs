[<RequireQualifiedAccessAttribute>]
module HabitsTracker.Helpers.HeadersParser

open System

open Grpc.Core

let getUserId (requestHeaders: Metadata) =
    requestHeaders
    |> Seq.find (fun h -> h.Key = "x-user-id")
    |> fun h -> h.Value |> Int32.Parse
