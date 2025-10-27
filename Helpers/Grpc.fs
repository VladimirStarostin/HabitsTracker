[<RequireQualifiedAccessAttribute>]
module HabitsTracker.Helpers.Grpc

open System

open Google.Protobuf.WellKnownTypes
open Grpc.Core

let callWithUserId (userId: int) (f: Metadata -> AsyncUnaryCall<'a>) =
    task {
        let metadata = Metadata ()
        metadata.Add ("x-user-id", userId.ToString ())
        return! f metadata
    }

let getUserId (requestHeaders: Metadata) =
    requestHeaders
    |> Seq.find (fun h -> h.Key = "x-user-id")
    |> fun h -> h.Value |> Int32.Parse

let toProtoTimestamp (dto: DateTimeOffset) = Timestamp.FromDateTimeOffset (dto)

let fromProtoTimestamp (ts: Timestamp) = ts.ToDateTimeOffset ()
