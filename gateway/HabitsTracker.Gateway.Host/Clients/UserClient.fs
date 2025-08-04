namespace HabitsTracker.Gateway.Host.Infrastructure.Clients

open System.Threading.Tasks

open UserService.V1

type UserInfoDto =
    { Email: string
      Name: string }

type IUsersClient =
    abstract GetByIdsAsync: int list -> Task<UserInfoDto list>
    abstract DeleteAsync: int -> Task<int>

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

        member _.DeleteAsync (id) : Task<int> =
            task {
                let! response = grpcClient.DeleteAccountAsync (DeleteAccountRequest (UserId = id))
                return response.NumRowsDeleted
            }
