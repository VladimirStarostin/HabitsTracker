namespace TrackingService.Host.Grpc

open System.Threading.Tasks

open Grpc.Core

open HabitsTracker.Helpers
open TrackingService.V1

open TrackingService.Host.DataAccess.Habits

type TrackingServiceImpl (connectionString: string) =
    inherit TrackingService.TrackingServiceBase ()

    override _.GetEventsByHabitIds (request: GetEventsByHabitIdsRequest, _) : Task<GetEventsByHabitIdsResponse> =
        task {
            let! events =
                getByHabitIdsAsync (request.HabitIds |> Set.ofSeq)
                |> Postgres.executeWithConnection connectionString

            let response = GetEventsByHabitIdsResponse ()

            for e in events do
                response.Events.Add (
                    HabitEvent (
                        Id = e.Id,
                        HabitId = e.HabitId,
                        UserId = e.UserId,
                        Date = (e.Date |> Grpc.toProtoTimestamp)
                    )
                )

            return response
        }

    override _.GetEventsByIds (request: GetEventsByIdsRequest, _) : Task<GetEventsByIdsResponse> =
        task {
            let! events =
                getByIdsAsync (request.Ids |> Set.ofSeq)
                |> Postgres.executeWithConnection connectionString

            let response = GetEventsByIdsResponse ()

            for e in events do
                response.Events.Add (
                    HabitEvent (
                        Id = e.Id,
                        HabitId = e.HabitId,
                        UserId = e.UserId,
                        Date = (e.Date |> Grpc.toProtoTimestamp)
                    )
                )

            return response
        }

    override _.TrackHabit (request: TrackHabitRequest, context: ServerCallContext) =
        task {
            let! habitEvent =
                insertSingleAsync
                    { HabitId = request.HabitId
                      Date = request.Date |> Grpc.fromProtoTimestamp
                      UserId = Grpc.getUserId context.RequestHeaders }
                |> Postgres.executeWithConnection connectionString

            return
                TrackHabitResponse (
                    Event =
                        HabitEvent (
                            Id = habitEvent.Id,
                            HabitId = habitEvent.HabitId,
                            UserId = habitEvent.UserId,
                            Date = (habitEvent.Date |> Grpc.toProtoTimestamp)
                        )
                )
        }

    override _.DeleteEvent (request: DeleteEventRequest, context: ServerCallContext) =
        task {
            let userId = Grpc.getUserId context.RequestHeaders

            let! deletedRowsCount =
                deleteByIdsAsync (request.Ids |> Seq.toList) userId
                |> Postgres.executeWithConnection connectionString

            return DeleteEventResponse (DeletedRowsCount = deletedRowsCount)
        }
