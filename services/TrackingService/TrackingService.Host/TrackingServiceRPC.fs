namespace TrackingService.Host.Grpc

open System

open Grpc.Core

open HabitsTracker.Helpers
open TrackingService.V1

open TrackingService.Host.DataAccess.HabitEvents

type TrackingServiceImpl (connectionString: string) =
    inherit TrackingService.TrackingServiceBase ()

    override _.GetEventsByHabitIds
        (request: GetEventsByHabitIdsRequest, _)
        : Threading.Tasks.Task<GetEventsByHabitIdsResponse> =
        task {
            let! events =
                getByHabitIdsAsync (request.HabitIds |> Seq.toList)
                |> SQLite.executeWithConnection connectionString

            let response = GetEventsByHabitIdsResponse ()

            for e in events do
                response.Events.Add (HabitEvent (Id = e.Id, HabitId = e.HabitId, UserId = e.UserId, Date = e.Date))

            return response
        }

    override _.GetEventsByIds (request: GetEventsByIdsRequest, _) : Threading.Tasks.Task<GetEventsByIdsResponse> =
        task {
            let! events =
                getByIdsAsync (request.Ids |> Seq.toList)
                |> SQLite.executeWithConnection connectionString

            let response = GetEventsByIdsResponse ()

            for e in events do
                response.Events.Add (HabitEvent (Id = e.Id, HabitId = e.HabitId, UserId = e.UserId, Date = e.Date))

            return response
        }

    override _.TrackHabit (request: TrackHabitRequest, context: ServerCallContext) =
        task {
            let! id =
                insertSingleAsync
                    { HabitId = request.HabitId
                      Date = request.Date
                      UserId = HeadersParser.getUserId context.RequestHeaders }
                |> SQLite.executeWithConnection connectionString

            let! result =
                getByIdsAsync (List.singleton id)
                |> SQLite.executeWithConnection connectionString

            let habitEvent = result |> Seq.exactlyOne

            return
                TrackHabitResponse (
                    Event =
                        HabitEvent (
                            Id = habitEvent.Id,
                            HabitId = habitEvent.HabitId,
                            UserId = habitEvent.UserId,
                            Date = habitEvent.Date
                        )
                )
        }

    override _.DeleteEvent (request: DeleteEventRequest, _) =
        task {
            let! deletedRowsCount =
                deleteByIdAsync request.Id
                |> SQLite.executeWithConnection connectionString

            return DeleteEventResponse (DeletedRowsCount = deletedRowsCount)
        }
