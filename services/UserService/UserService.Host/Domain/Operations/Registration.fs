[<RequireQualifiedAccess>]
module internal UserService.Host.Domain.Operations.Registration

open System.Data
open System.Threading.Tasks
open Grpc.Core

open BCrypt.Net

open HabitsTracker.Helpers.Pipelines

open UserService.Host.DataAccess.Users
open UserService.Host.Domain.Common.Types
open UserService.Host.Domain.Common.Operations

type UserRegistrationRequest =
    { Email: string
      Name: string
      Password: string }

type RegistrationError =
    | UserAlreadyExists
    | PasswordHashingError of string
    | DbError of string
    | TokenError of string

    member this.ToErrMessage () =
        match this with
        | UserAlreadyExists -> "User with id {} already exist."
        | PasswordHashingError detail -> $"Error hashing the password: {detail}"
        | DbError detail -> $"Error working with db: {detail}"
        | TokenError detail -> $"Failed to generate token: {detail}"

type PipelineResult<'Ok> = PipelineResult<'Ok, RegistrationError>

module private Steps =
    let ensureUserNotExists (email: string) (conn: IDbConnection) : Task<PipelineResult<unit>> =
        task {
            try
                match! getByEmailAsync email conn with
                | None -> return Ok ()
                | Some _ -> return Error UserAlreadyExists
            with ex ->
                return Error (DbError ex.Message)
        }

    let hashPassword (plainPassword: string) : PipelineResult<string> =
        try
            let salt = BCrypt.GenerateSalt ()
            Ok <| BCrypt.HashPassword (plainPassword, salt)
        with ex ->
            Error (PasswordHashingError ex.Message)

    let saveNewUserAsync
        (hash: string)
        (registrationRequest: UserRegistrationRequest)
        (conn: IDbConnection)
        : Task<PipelineResult<User>> =
        task {
            try
                let! newUser =
                    insertSingleAsync
                        { Name = registrationRequest.Name
                          PasswordHash = hash
                          Email = registrationRequest.Email }
                        conn

                return Ok newUser
            with ex ->
                return Error (DbError ex.Message)
        }

let private registration = PipelineBuilder ()

let runPipelineAsync
    (request: UserRegistrationRequest)
    (jwtSettings: JwtSettings)
    (conn: IDbConnection)
    : Task<PipelineResult<AuthResponse>> =
    registration {
        do! Steps.ensureUserNotExists request.Email conn
        let! hash = Steps.hashPassword request.Password
        let! newUser = Steps.saveNewUserAsync hash request conn
        let! rtObj = createRefreshToken newUser.Id jwtSettings
        let! rtString = saveNewTokenAsync rtObj conn
        let! authResp = generateJwt rtString rtObj.UserId jwtSettings
        return authResp
    }

let toRpcError (err: RegistrationError) : RpcException =
    let status =
        match err with
        | UserAlreadyExists -> Status(StatusCode.AlreadyExists, "User already exists.")
        | PasswordHashingError detail -> Status(StatusCode.Internal, $"Error hashing the password: {detail}")
        | DbError detail -> Status(StatusCode.Internal, $"Error working with db: {detail}")
        | TokenError detail -> Status(StatusCode.Internal, $"Failed to generate token: {detail}")
    RpcException(status)
