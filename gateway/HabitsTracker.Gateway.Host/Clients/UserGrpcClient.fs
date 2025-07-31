namespace HabitTrackerApi.GrpcClients

open System.Threading.Tasks

open UserService.V1

type IUserGrpcClient =
    abstract DeleteUserAsync: int64 -> Task<unit>

type UserGrpcClient (grpcClient: UserService.UserServiceClient) =
    interface IUserGrpcClient with
        member _.DeleteUserAsync (id) : Task<unit> =
            task {
                let! _ = grpcClient.DeleteAccountAsync (DeleteAccountRequest (UserId = id))
                return ()
            }
