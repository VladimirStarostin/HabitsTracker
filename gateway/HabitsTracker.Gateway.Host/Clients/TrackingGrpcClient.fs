namespace HabitTracker.Host.Infrastructure.GrpcClients

open System
open System.Threading.Tasks
open HabitTracker.Host.Models.V1
open TrackingService.V1

type HabitEventDto =
    { Id: int64
      HabitId: int64
      UserId: int64
      Date: DateTime }

type CreateEventRequest =
    { HabitId: int64
      UserId: int64
      Date: DateTime }

type ITrackingGrpcClient =
    abstract GetEventsByHabitIdsAsync: int64 Set -> Task<HabitEventDto list>
    abstract GetEventByIdAsync: int64 -> Task<Result<HabitEventDto, string>>
    abstract CreateEventAsync: int64 -> CreateEventRequest -> Task<HabitEventDto>
    abstract DeleteEventAsync: int64 -> Task<Result<unit, string>>

type TrackingGrpcClient (grpcClient: TrackingService.TrackingServiceClient) =
    interface ITrackingGrpcClient with
        member _.GetEventsByHabitIdsAsync (habitIds: int64 Set) : Task<HabitEventDto list> =
            task {
                let request = GetEventsByHabitIdsRequest ()
                request.HabitIds.AddRange habitIds

                let! resp = grpcClient.GetEventsByHabitIdsAsync request

                return
                    resp.Events
                    |> Seq.map (fun e ->
                        { Id = e.Id
                          HabitId = e.HabitId
                          UserId = e.UserId
                          Date = e.Date |> DateTime.Parse }
                    )
                    |> Seq.toList
            }

        member _.GetEventByIdAsync (id: int64) : Task<Result<HabitEventDto, string>> =
            task {
                let request = GetEventsByIdsRequest ()
                do request.Ids.Add id

                let! result = grpcClient.GetEventsByIdsAsync request

                return
                    match result.Events |> Seq.tryExactlyOne with
                    | Some habitEvent ->
                        Ok (
                            { Id = habitEvent.Id
                              HabitId = habitEvent.HabitId
                              UserId = habitEvent.UserId
                              Date = DateTime.Parse habitEvent.Date }
                            : HabitEventDto
                        )
                    | None -> Error $"No event was found with id specified {id}"
            }

        member _.CreateEventAsync (habitId: int64) (request: CreateEventRequest) : Task<HabitEventDto> =
            task {
                let! createdHabitEvent =
                    grpcClient.TrackHabitAsync (
                        TrackHabitRequest (HabitId = habitId, Date = request.Date.ToShortDateString ())
                    )

                return
                    { Id = createdHabitEvent.Event.Id
                      HabitId = createdHabitEvent.Event.HabitId
                      UserId = createdHabitEvent.Event.UserId
                      Date = createdHabitEvent.Event.Date |> DateTime.Parse }
            }

        member _.DeleteEventAsync (id: int64) : Task<Result<unit, string>> =
            task {
                let! response = grpcClient.DeleteEventAsync (DeleteEventRequest (Id = id))

                return
                    if response.DeletedRowsCount = 1 then
                        Ok ()
                    else
                        Error $"Unexpected rows count deleted : {response.DeletedRowsCount}"
            }
