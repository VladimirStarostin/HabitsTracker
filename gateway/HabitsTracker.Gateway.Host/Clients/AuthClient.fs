namespace HabitsTracker.Gateway.Host.Infrastructure.Clients

open System.Threading.Tasks

open UserService.V1

type RegisterDto =
    { Name: string
      Email: string
      Password: string }

type LoginDto =
    { Email: string
      Password: string }

type RefreshTokenDto =
    { RefreshToken: string }

type AuthResponseDto =
    { AccessToken: string
      RefreshToken: string }

type IAuthClient =
    abstract RegisterAsync: RegisterDto -> Task<AuthResponseDto>
    abstract LoginAsync: LoginDto -> Task<AuthResponseDto>
    abstract RefreshTokenAsync: RefreshTokenDto -> Task<AuthResponseDto>

//type AttemptBuilder (map: exn -> 'e) =
//    member _.Return x = Ok x
//    member _.ReturnFrom r = r
//    member _.Bind (r, f) = Result.bind f r
//    member _.Delay f = f

//    member _.Run f =
//        try
//            Ok (f ())
//        with e ->
//            Error (map e)

type AuthClient (grpcClient: UserService.UserServiceClient) =
    let toAuthResponseDto (grpcResponse: UserService.V1.AuthResponse) : AuthResponseDto =
        { AccessToken = grpcResponse.AccessToken
          RefreshToken = grpcResponse.RefreshToken }

    interface IAuthClient with
        member _.RegisterAsync (request: RegisterDto) : Task<AuthResponseDto> =
            task {
                let grpcRequest =
                    UserService.V1.RegisterRequest (
                        Email = request.Email,
                        Name = request.Name,
                        Password = request.Password
                    )

                let! grpcResponse = grpcClient.RegisterAsync grpcRequest
                return toAuthResponseDto grpcResponse
            }

        member _.LoginAsync (request: LoginDto) : Task<AuthResponseDto> =
            task {
                let grpcRequest =
                    UserService.V1.LoginRequest (Email = request.Email, Password = request.Password)

                let! grpcResponse = grpcClient.LoginAsync (grpcRequest)
                return toAuthResponseDto grpcResponse
            }

        member _.RefreshTokenAsync (request: RefreshTokenDto) =
            task {
                let grpcRequest =
                    UserService.V1.RefreshRequest (RefreshToken = request.RefreshToken)

                let! grpcResponse = grpcClient.RefreshAsync grpcRequest
                return toAuthResponseDto grpcResponse
            }
