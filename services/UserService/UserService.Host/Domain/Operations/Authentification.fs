[<RequireQualifiedAccess>]
module internal UserService.Host.Domain.Operations.Authentication

open System
open System.Data
open System.Threading.Tasks

open HabitsTracker.Helpers
open HabitsTracker.Helpers.Pipelines

open UserService.Host.DataAccess.RefreshTokens
open UserService.Host.Domain.Common.Types
open UserService.Host.Domain.Common.Operations

type AuthError =
    | MissingToken
    | TokenNotFound
    | TokenExpired
    | TokenRevoked
    | DbError of string
    | JwtError of string

    member this.ToErrMessage () =
        match this with
        | MissingToken -> "Refresh token was not provided."
        | TokenNotFound -> "Refresh token not found in the database."
        | TokenExpired -> "Refresh token has expired."
        | TokenRevoked -> "Refresh token has been revoked."
        | DbError msg -> $"Database error while handling refresh token: {msg}"
        | JwtError msg -> $"JWT error (signing or validation failed): {msg}"

type PipelineResult<'Ok> = PipelineResult<'Ok, AuthError>

module internal Steps =
    let validateInput (requestedToken: string) : PipelineResult<string> =
        if String.IsNullOrWhiteSpace requestedToken then
            Error MissingToken
        else
            Ok requestedToken

    let tryFindExistingInDbAsync
        (okToken: string)
        (conn: IDbConnection)
        : Task<PipelineResult<RefreshToken>> =
        findRecordByTokenAsync okToken conn
        |> Task.map (fun res ->
            res
            |> Option.map (fun dbRec ->
                Ok
                    { UserId = dbRec.UserId
                      Token = dbRec.Token
                      ExpiresAt = dbRec.ExpiresAt
                      IsRevoked = dbRec.IsRevoked }
            )
            |> Option.defaultValue (Error AuthError.TokenNotFound)
        )

    let validateExistingRefreshToken (token: RefreshToken) : PipelineResult<RefreshToken> =
        if token.IsRevoked then
            Error TokenRevoked
        elif token.ExpiresAt < DateTime.UtcNow then
            Error TokenExpired
        else
            Ok token

    let revokeOld (token: RefreshToken) (conn: IDbConnection) : Task<PipelineResult<unit>> =
        revokeByTokenAsync token.Token conn
        |> Task.map (fun _ -> Ok ())

let internal validation = PipelineBuilder ()

let runPipelineAsync
    (initialToken: string)
    (jwtSettings: JwtSettings)
    (conn: IDbConnection)
    : Task<PipelineResult<AuthResponse>> =
    validation {
        let! validToken = Steps.validateInput initialToken
        let! existingRecord = Steps.tryFindExistingInDbAsync validToken conn
        let! existingValidRecord = Steps.validateExistingRefreshToken existingRecord
        do! Steps.revokeOld existingValidRecord conn
        let! newToken = createRefreshToken existingValidRecord.UserId jwtSettings
        let! savedToken = saveNewTokenAsync newToken conn
        let! authResp = generateJwt savedToken newToken.UserId jwtSettings

        return authResp
    }
