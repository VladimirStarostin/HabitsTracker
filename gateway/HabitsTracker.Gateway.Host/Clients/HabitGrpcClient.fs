namespace HabitTracker.Host.Infrastructure.GrpcClients

open System.Threading.Tasks
open HabitsService.V1

type HabitDto =
    { Id: int64
      Name: string
      Description: string option }

type CreateHabitRequest =
    { Name: string
      Description: string option }

type UpdateHabitRequest =
    { Name: string
      Description: string option }

type IHabitGrpcClient =
    abstract GetAllHabits: unit -> Task<HabitDto list>
    abstract GetHabitById: int64 -> Task<HabitDto>
    abstract CreateHabit: CreateHabitRequest -> Task<HabitDto>
    abstract UpdateHabit: int64 * UpdateHabitRequest -> Task<unit>
    abstract DeleteHabit: int64 -> Task<unit>

// TODO

//type HabitGrpcClient (grpcClient: HabitsService.HabitsServiceClient) =
//    interface IHabitGrpcClient with
//        member _.GetAllHabits () =
//            task {
//                let! resp = grpcClient.ListHabitsAsync(Google.Protobuf.WellKnownTypes.Empty ()).ResponseAsync
//                return resp.Habits |> List.ofSeq
//            }

//        member _.GetHabitById (id) =
//            client.GetHabitAsync(HabitRequest (Id = id)).ResponseAsync

//        member _.CreateHabit (req) =
//            client
//                .CreateHabitAsync(
//                    CreateHabitRequest (
//                        Name = req.Name,
//                        Description = req.Description
//                        |> Option.defaultValue ""
//                    )
//                )
//                .ResponseAsync

//        member _.UpdateHabit (id, req) =
//            client
//                .UpdateHabitAsync(
//                    UpdateHabitRequest (
//                        Id = id,
//                        Name = req.Name,
//                        Description = req.Description
//                        |> Option.defaultValue ""
//                    )
//                )
//                .ResponseAsync
//            |> ignore

//            System.Threading.Tasks.Task.CompletedTask

//        member _.DeleteHabit (id) =
//            client.DeleteHabitAsync(HabitRequest (Id = id)).ResponseAsync
//            |> ignore

//            System.Threading.Tasks.Task.CompletedTask
