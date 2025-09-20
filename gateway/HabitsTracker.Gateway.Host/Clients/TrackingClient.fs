namespace HabitsTracker.Gateway.Host.Infrastructure.Clients

open System
open System.Threading.Tasks

open TrackingService.V1

open HabitsTracker.Helpers

type HabitEventDto =
    { Id: int
      HabitId: int
      Date: DateTimeOffset }

type TrackHabitRequest =
    { HabitId: int
      Date: DateTimeOffset }

type IHabitsTrackingClient =
    abstract GetByHabitIdsAsync: int list -> Task<HabitEventDto list>
    abstract GetByIdsAsync: int list -> Task<HabitEventDto list>
    abstract TrackHabitAsync: TrackHabitRequest -> int -> Task<HabitEventDto>
    abstract DeleteAsync: int list -> int -> Task<int>

type HabitsTrackingClient (grpcClient: TrackingService.TrackingServiceClient) =
    let toHabitEventDto (habitEvent: TrackingService.V1.HabitEvent) : HabitEventDto =
        { Id = habitEvent.Id
          HabitId = habitEvent.HabitId
          Date = habitEvent.Date |> Grpc.fromProtoTimestamp }

    interface IHabitsTrackingClient with
        member _.GetByHabitIdsAsync (habitIds: int list) : Task<HabitEventDto list> =
            task {
                let request = GetEventsByHabitIdsRequest ()
                request.HabitIds.AddRange habitIds

                let! resp = grpcClient.GetEventsByHabitIdsAsync request

                return
                    resp.Events
                    |> Seq.map toHabitEventDto
                    |> List.ofSeq
            }

        member _.GetByIdsAsync (ids: int list) : Task<HabitEventDto list> =
            task {
                let request = GetEventsByIdsRequest ()
                do request.Ids.AddRange ids

                let! resp = grpcClient.GetEventsByIdsAsync request

                return
                    resp.Events
                    |> Seq.map toHabitEventDto
                    |> List.ofSeq
            }

        member _.TrackHabitAsync (request: TrackHabitRequest) (userId: int) : Task<HabitEventDto> =
            task {
                let grpcRequest = TrackingService.V1.TrackHabitRequest ()
                grpcRequest.HabitId <- request.HabitId
                grpcRequest.Date <- request.Date |> Grpc.toProtoTimestamp

                let! createdHabitEvent =
                    Grpc.callWithUserId userId (fun md -> grpcClient.TrackHabitAsync (grpcRequest, headers = md))

                return toHabitEventDto createdHabitEvent.Event
            }

        member _.DeleteAsync (ids: int list) (userId: int) : Task<int> =
            task {
                let request = DeleteEventRequest ()
                request.Ids.AddRange ids
                let! response = Grpc.callWithUserId userId (fun md -> grpcClient.DeleteEventAsync (request, md))

                return response.DeletedRowsCount
            }
