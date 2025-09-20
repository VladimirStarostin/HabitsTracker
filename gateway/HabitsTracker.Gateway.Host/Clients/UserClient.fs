namespace HabitsTracker.Gateway.Host.Infrastructure.Clients

open System.Threading.Tasks

open HabitsTracker.Helpers
open UserService.V1

type UserInfoDto =
    { Email: string
      Name: string }

type IUsersClient =
    abstract GetByIdsAsync: int list -> Task<UserInfoDto list>
    abstract DeleteRefreshTokensAsync: int -> Task<int>
    abstract DeleteUsersAsync: int -> Task<int>

type UsersClient (grpcClient: UserService.UserServiceClient) =
    interface IUsersClient with
        member _.GetByIdsAsync (userIds: int list) : Task<UserInfoDto list> =
            task {
                let request = GetByIdsRequest ()
                request.UserIds.AddRange userIds

                let! grpcResponse = grpcClient.GetByIdsAsync request

                return
                    grpcResponse.Users
                    |> Seq.map (fun user ->
                        { Name = user.Name
                          Email = user.Email }
                    )
                    |> List.ofSeq
            }

        member _.DeleteRefreshTokensAsync (userId: int) : Task<int> =
            task {
                let! response =
                    Grpc.callWithUserId
                        userId
                        (fun metadata -> grpcClient.DeleteRefreshTokensAsync (DeleteRefreshTokensRequest (), metadata))

                return response.DeletedRowsCount
            }

        member _.DeleteUsersAsync (id) : Task<int> =
            task {
                let! response =
                    Grpc.callWithUserId
                        id
                        (fun metadata -> grpcClient.DeleteAccountAsync (DeleteAccountRequest (), headers = metadata))

                return response.DeletedRowsCount
            }
