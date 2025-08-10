[<RequireQualifiedAccess>]
module internal UserService.Host.Domain.Operations.Login

open System
open System.Data
open System.Threading.Tasks
open Grpc.Core

open HabitsTracker.Helpers.Pipelines

open UserService.Host.DataAccess.Users
open UserService.Host.Domain.Common.Types
open UserService.Host.Domain.Common.Operations
open UserService.Host.DataAccess

type LoginInput =
    { Email: string
      Password: string }

type LoginError =
    | InvalidInput of string
    | UserNotFound
    | InvalidPassword
    | PasswordHashingError of string
    | DbError of string
    | TokenError of string
    | JwtError of string

    member this.ToErrMessage() =
        match this with
        | InvalidInput _ -> "invalid_input"
        | UserNotFound -> "invalid_credentials"
        | InvalidPassword -> "invalid_credentials"
        | DbError _ -> "backend_unavailable"
        | PasswordHashingError _ -> "hashing_error"
        | TokenError _ -> "token_error"
        | JwtError _ -> "jwt_error"

type PipelineResult<'a> = PipelineResult<'a, LoginError>

let internal validation = PipelineBuilder ()

module private Steps =
    let validateInput (input: LoginInput) : PipelineResult<LoginInput> =
        let isBlank (s: string) = String.IsNullOrWhiteSpace s

        validation {
            do!
                if isBlank input.Email then
                    Error (InvalidInput "Email is required.")
                else
                    Ok ()

            do!
                if isBlank input.Password then
                    Error (InvalidInput "Password is required.")
                else
                    Ok ()

            return input
        }

    let tryFindUserByEmailAsync (email: string) (conn: IDbConnection) : Task<PipelineResult<User>> =
        task {
            try
                let! opt = Users.getByEmailAsync email conn

                return
                    match opt with
                    | Some u -> Ok u
                    | None -> Error UserNotFound
            with ex ->
                return Error (DbError ex.Message)
        }

    let verifyPassword (hasher: Security.IPasswordHasher) (input: LoginInput) (user: User) : PipelineResult<User> =
        match hasher.Verify (input.Password, user.PasswordHash) with
        | Result.Ok () -> Ok user
        | Result.Error msg ->
            if String.IsNullOrWhiteSpace msg then
                Error InvalidPassword
            else
                Error (PasswordHashingError msg)

let runPipelineAsync
    (input: LoginInput)
    (jwtSettings: JwtSettings)
    (hasher: Security.IPasswordHasher)
    (conn: IDbConnection)
    : Task<PipelineResult<AuthResponse>> =
    validation {
        let! valid = Steps.validateInput input
        let! user = Steps.tryFindUserByEmailAsync valid.Email conn
        let! userOk = Steps.verifyPassword hasher valid user
        let! newToken = createRefreshToken<LoginError> userOk.Id jwtSettings
        let! savedToken = saveNewTokenAsync newToken conn
        let! authResp = generateJwt savedToken newToken.UserId jwtSettings

        return authResp
    }

let toRpcError (err: LoginError) : RpcException =
    let status =
        match err with
        | InvalidInput msg -> Status(StatusCode.InvalidArgument, msg)
        | UserNotFound -> Status(StatusCode.NotFound, "User not found.")
        | InvalidPassword -> Status(StatusCode.PermissionDenied, "Invalid password.")
        | PasswordHashingError msg -> Status(StatusCode.Internal, $"Password hashing error: {msg}")
        | DbError msg -> Status(StatusCode.Internal, $"Database error: {msg}")
        | TokenError msg -> Status(StatusCode.Internal, $"Token generation error: {msg}")
        | JwtError msg -> Status(StatusCode.Internal, $"JWT error: {msg}")
    RpcException(status)
