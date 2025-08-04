namespace HabitsTracker.Gateway.Host.Infrastructure.Clients

open System.Threading.Tasks

open UserService.V1

type UserDto =
    { Id: int
      Email: string
      Name: string }

type IUsersClient =
    abstract GetByIdsAsync: int Set -> Task<UserDto Set>
    abstract DeleteAsync: int -> Task<unit>

type UsersClient (grpcClient: UserService.UserServiceClient) =
    interface IUsersClient with
        member _.GetByIdsAsync (userIds: int Set) : Task<UserDto Set> =
            task {
                let request = GetByIdsRequest ()
                request.UserIds.AddRange userIds

                let! grpcResponse = grpcClient.GetByIdsAsync request

                return
                    grpcResponse.Users
                    |> Seq.map (fun user ->
                        { Id = user.Id
                          Name = user.Name
                          Email = user.Email }
                    )
                    |> Set.ofSeq
            }

        member _.DeleteAsync (id) : Task<unit> =
            task {
                let! _ = grpcClient.DeleteAccountAsync (DeleteAccountRequest (UserId = id))
                return ()
            }
