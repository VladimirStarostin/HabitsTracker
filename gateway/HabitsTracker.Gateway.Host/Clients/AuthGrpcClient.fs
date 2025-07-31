namespace HabitTracker.Host.Infrastructure.GrpcClients

open System.Threading.Tasks

open UserService.V1

type RegisterRequest =
    { Name: string
      Email: string
      Password: string }

type LoginRequest =
    { Email: string
      Password: string }

type RefreshRequest =
    { RefreshToken: string }

type AuthResponse =
    { AccessToken: string
      RefreshToken: string }

type UserDto =
    { Id: int64
      Name: string
      Email: string }

type IAuthGrpcClient =
    abstract RegisterAsync: RegisterRequest -> Task<AuthResponse>
    abstract LoginAsync: LoginRequest -> Task<AuthResponse>
    abstract RefreshAsync: RefreshRequest -> Task<AuthResponse>
    abstract GetByIdsAsync: int64 list -> Task<UserDto list>

type AuthGrpcClient (grpcClient: UserService.UserServiceClient) =
    let fromGrpcToApi (grpcResponse: UserService.V1.AuthResponse) : AuthResponse =
        { AccessToken = grpcResponse.AccessToken
          RefreshToken = grpcResponse.RefreshToken }

    interface IAuthGrpcClient with
        member _.RegisterAsync (request: RegisterRequest) : Task<AuthResponse> =
            task {
                let grpcRequest =
                    UserService.V1.RegisterRequest (
                        Email = request.Email,
                        Name = request.Name,
                        Password = request.Password
                    )

                let! grpcResponse = grpcClient.RegisterAsync grpcRequest
                return fromGrpcToApi grpcResponse
            }

        member _.LoginAsync (request: LoginRequest) : Task<AuthResponse> =
            task {
                let grpcRequest =
                    UserService.V1.LoginRequest (Email = request.Email, Password = request.Password)

                let! grpcResponse = grpcClient.LoginAsync (grpcRequest)
                return fromGrpcToApi grpcResponse
            }

        member _.RefreshAsync (request: RefreshRequest) =
            task {
                let grpcRequest =
                    UserService.V1.RefreshRequest (RefreshToken = request.RefreshToken)

                let! grpcResponse = grpcClient.RefreshAsync grpcRequest
                return fromGrpcToApi grpcResponse
            }

        member _.GetByIdsAsync (userIds: int64 list) : Task<UserDto list> =
            task {
                let request = GetByIdsRequest ()
                userIds |> List.iter request.UserIds.Add

                let! grpcResponse = grpcClient.GetByIdsAsync request

                return
                    grpcResponse.Users
                    |> Seq.map (fun user ->
                        { Id = user.Id
                          Name = user.Name
                          Email = user.Email }
                    )
                    |> Seq.toList
            }
