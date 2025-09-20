[<RequireQualifiedAccess>]
module internal UserService.Host.Domain.Operations.Registration

open System.Data
open System.Threading.Tasks

open BCrypt.Net
open Grpc.Core

open HabitsTracker.Helpers.Pipelines

open UserService.Host.DataAccess
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

    member this.toRpcError () : RpcException =
        let status =
            match this with
            | UserAlreadyExists -> Status (StatusCode.AlreadyExists, "User already exists.")
            | PasswordHashingError detail -> Status (StatusCode.Internal, $"Error hashing the password: {detail}")
            | DbError detail -> Status (StatusCode.Internal, $"Error working with db: {detail}")
            | TokenError detail -> Status (StatusCode.Internal, $"Failed to generate token: {detail}")

        RpcException (status)

type PipelineResult<'Ok> = PipelineResult<'Ok, RegistrationError>

module private Steps =
    let ensureUserNotExists (email: string) (conn: IDbConnection) : Task<PipelineResult<unit>> =
        task {
            try
                match! Users.getByEmailAsync email conn with
                | None -> return Ok ()
                | Some _ -> return Error UserAlreadyExists
            with ex ->
                return Error (DbError ex.Message)
        }

    let hashPassword (plainPassword: string) (hasher: Security.IPasswordHasher) : PipelineResult<string> =
        try
            match hasher.Hash plainPassword with
            | Result.Ok res -> PipelineResult<string>.Ok res
            | Result.Error err -> PipelineResult<string>.Error (RegistrationError.PasswordHashingError err)

        with ex ->
            Error (PasswordHashingError ex.Message)

    let saveNewUserAsync
        (hash: string)
        (registrationRequest: UserRegistrationRequest)
        (conn: IDbConnection)
        : Task<PipelineResult<Users.User>> =
        task {
            try
                let! newUser =
                    Users.insertSingleAsync
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
    (hasher: Security.IPasswordHasher)
    (conn: IDbConnection)
    : Task<PipelineResult<AuthResponse>> =
    registration {
        do! Steps.ensureUserNotExists request.Email conn
        let! hash = Steps.hashPassword request.Password hasher
        let! newUser = Steps.saveNewUserAsync hash request conn
        let! rtObj = createRefreshToken newUser.Id jwtSettings
        let! rtString = saveNewTokenAsync rtObj conn
        let! authResp = generateJwt rtString rtObj.UserId jwtSettings
        return authResp
    }
