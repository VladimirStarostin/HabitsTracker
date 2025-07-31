module internal UserService.Host.Domain.Services.Authentication

open System
open System.IdentityModel.Tokens.Jwt
open System.Security.Claims
open System.Text
open System.Threading.Tasks

open Microsoft.IdentityModel.Tokens

open HabitsTracker.Helpers

open UserService.Host.DataAccess.RefreshTokens

type AuthResponse =
    { AccessToken: string
      RefreshToken: string }

type JwtSettings =
    { Secret: string
      Issuer: string
      Audience: string
      AccessTokenValidityMinutes: int
      RefreshTokenValidityDays: int }

type private RefreshToken =
    { UserId: int64
      Token: string
      ExpiresAt: DateTime
      IsRevoked: bool }

type private AuthError =
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

type private PipelineResult<'t> =
    | Ok of 't
    | Error of AuthError

module private Steps =
    let validateInput (requestedToken: string) : PipelineResult<string> =
        if String.IsNullOrWhiteSpace requestedToken then
            Error MissingToken
        else
            PipelineResult.Ok requestedToken

    let tryFindExistingInDbAsync (okToken: string) (connectionString: string) : Task<PipelineResult<RefreshToken>> =
        findRecordByTokenAsync okToken
        |> SQLite.executeWithConnection connectionString
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

    let revokeOld (token: RefreshToken) (connectionString: string) : Task<PipelineResult<unit>> =
        revokeByTokenAsync token.Token
        |> SQLite.executeWithConnection connectionString
        |> Task.map (fun _ -> Ok ())

    let createNewToken (existingToken: RefreshToken) (jwtSettings: JwtSettings) : PipelineResult<RefreshToken> =
        let newRefreshToken = Guid.NewGuid().ToString "N"
        let newExpiry = DateTime.UtcNow.AddDays (float jwtSettings.RefreshTokenValidityDays)

        Ok
            { Token = newRefreshToken
              UserId = existingToken.UserId
              ExpiresAt = newExpiry
              IsRevoked = false }

    let saveNewTokenAsync (newToken: RefreshToken) (connectionString: string) : Task<PipelineResult<string>> =
        task {
            let! _ =
                insertSingleAsync
                    { Token = newToken.Token
                      UserId = newToken.UserId
                      ExpiresAt = newToken.ExpiresAt
                      IsRevoked = newToken.IsRevoked
                      CreatedAt = DateTime.UtcNow }
                |> SQLite.executeWithConnection connectionString

            return Ok newToken.Token
        }

    let generateJwt
        (newRefreshToken: string)
        (userId: int64)
        (jwtSettings: JwtSettings)
        : PipelineResult<AuthResponse> =
        let claims =
            [ Claim (JwtRegisteredClaimNames.Sub, userId.ToString ())
              Claim (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString ()) ]

        let key = SymmetricSecurityKey (Encoding.UTF8.GetBytes (jwtSettings.Secret))
        let creds = SigningCredentials (key, SecurityAlgorithms.HmacSha256)
        let now = DateTime.UtcNow
        let expires = now.AddMinutes (float jwtSettings.AccessTokenValidityMinutes)

        let jwt =
            JwtSecurityToken (
                issuer = jwtSettings.Issuer,
                audience = jwtSettings.Audience,
                claims = claims,
                notBefore = Nullable now,
                expires = Nullable expires,
                signingCredentials = creds
            )

        Ok
            { AccessToken = JwtSecurityTokenHandler().WriteToken (jwt)
              RefreshToken = newRefreshToken }

type private ValidationBuilder () =
    member _.Bind (wrappedValue: PipelineResult<'a>, func: 'a -> 'b) : 'b =
        match wrappedValue with
        | Ok value -> func value
        | Error err -> failwith <| err.ToErrMessage ()

    member _.Bind (wrappedValueTask: Task<PipelineResult<'a>>, func: 'a -> Task<'b>) : Task<'b> =
        task {
            let! wrappedValue = wrappedValueTask

            match wrappedValue with
            | Ok value -> return! func value
            | Error err -> return failwith <| err.ToErrMessage ()
        }

    member _.Return (x: 'b) : Task<'b> = Task.FromResult x
    member _.ReturnFrom (t: Task<'b>) = t

let private validation = ValidationBuilder ()

let validateAndRefresh
    (initialToken: string)
    (connectionString: string)
    (jwtSettings: JwtSettings)
    : Task<AuthResponse> =
    validation {
        let! validToken = Steps.validateInput initialToken
        let! existingRecord = Steps.tryFindExistingInDbAsync validToken connectionString
        let! existingValidRecord = Steps.validateExistingRefreshToken existingRecord
        do! Steps.revokeOld existingValidRecord connectionString
        let! newToken = Steps.createNewToken existingValidRecord jwtSettings
        let! savedToken = Steps.saveNewTokenAsync newToken connectionString
        let! authResp = Steps.generateJwt savedToken newToken.UserId jwtSettings

        return authResp
    }
