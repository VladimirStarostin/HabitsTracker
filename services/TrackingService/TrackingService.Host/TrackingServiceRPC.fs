namespace TrackingService.Host.Grpc

open System

open Grpc.Core

open HabitsTracker.Helpers
open TrackingService.V1

open TrackingService.Host.DataAccess.HabitEvents

type TrackingServiceImpl(connectionString: string) =
    inherit TrackingService.TrackingServiceBase()

    override _.GetAllEvents(_, _) =
        task {
            let! events = getAllAsync |> SQLite.executeWithConnection connectionString
            let response = GetAllEventsResponse()

            for e in events do
                response.Events.Add(
                    HabitEvent(Id = e.Id, HabitId = e.HabitId, UserId = e.UserId, Date = e.Date)
                )

            return response
        }

    override _.GetByIds(request: GetByIdsRequest, _) =
        task {
            let! events =
                getByIdsAsync (request.EventIds |> Seq.toList)
                |> SQLite.executeWithConnection connectionString

            let response = GetByIdsResponse()

            for e in events do
                response.Events.Add(
                    HabitEvent(Id = e.Id, HabitId = e.HabitId, UserId = e.UserId, Date = e.Date)
                )

            return response
        }

    override _.TrackHabit(request: TrackHabitRequest, context: ServerCallContext) =
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
                TrackHabitResponse(
                    Event =
                        HabitEvent(
                            Id = habitEvent.Id,
                            HabitId = habitEvent.HabitId,
                            UserId = habitEvent.UserId,
                            Date = habitEvent.Date
                        )
                )
        }

    override _.DeleteEvent(request: DeleteEventRequest, _) =
        task {
            let! _ =
                deleteByIdAsync request.EventId
                |> SQLite.executeWithConnection connectionString

            return DeleteEventResponse()
        }
