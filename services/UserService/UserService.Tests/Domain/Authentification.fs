module UserService.Tests.Domain.Services.Authentication

open System
open Microsoft.Data.Sqlite

open NUnit.Framework
open Dapper.FSharp.SQLite

open HabitsTracker.Helpers
open UserService.Host.DataAccess.RefreshTokens
open UserService.Host.Domain.Services.Authentication
open UserService.Migrations
open UserService.Tests

let connectionString = "Data Source=file:memdb_auth?mode=memory&cache=shared"

let defaultJwtSettings =
    { Secret = "a_very_long_test_secret_key_for_hmacsha256"
      Issuer = "test_issuer"
      Audience = "test_audience"
      AccessTokenValidityMinutes = 60
      RefreshTokenValidityDays = 1 }

[<OneTimeSetUp>]
let Setup () =
    do OptionTypes.register ()
    do MigrationRunner.runMigrations connectionString (typeof<CreateTablesMigration>.Assembly)

[<SetUp>]
let clearDbAsync () =
    task {
        use conn = new SqliteConnection (connectionString)
        let! _ = deleteAllAsync conn
        return ()
    }

[<Test>]
let ``validateInput - missing token returns MissingToken error`` () =
    match Steps.validateInput "" with
    | Error err ->
        Assert.That (err, Is.EqualTo AuthError.MissingToken)
        Assert.That (err.ToErrMessage (), Is.EqualTo "Refresh token was not provided.")
    | Ok _ -> Assert.Fail ("Expected MissingToken error, but got Ok")

[<Test>]
let ``validateInput - valid token returns Ok`` () =
    match Steps.validateInput "some-token" with
    | Ok v -> Assert.That (v, Is.EqualTo "some-token")
    | Error e -> Assert.Fail ($"Expected Ok, but got Error {e}")

[<Test>]
let ``validateExistingRefreshToken - expired token returns TokenExpired`` () =
    let expired =
        { UserId = 1L
          Token = "t"
          ExpiresAt = DateTime.UtcNow.AddDays (-1.0)
          IsRevoked = false }

    match Steps.validateExistingRefreshToken expired with
    | Error AuthError.TokenExpired -> ()
    | Error e -> Assert.Fail ($"Expected TokenExpired, but got {e}")
    | Ok _ -> Assert.Fail ("Expected TokenExpired error, but got Ok")

[<Test>]
let ``validateExistingRefreshToken - revoked token returns TokenRevoked`` () =
    let revoked =
        { UserId = 2L
          Token = "t2"
          ExpiresAt = DateTime.UtcNow.AddDays (1.0)
          IsRevoked = true }

    match Steps.validateExistingRefreshToken revoked with
    | Error AuthError.TokenRevoked -> ()
    | Error e -> Assert.Fail ($"Expected TokenRevoked, but got {e}")
    | Ok _ -> Assert.Fail ("Expected TokenRevoked error, but got Ok")

[<Test>]
let ``createNewToken - generates new token with updated expiry`` () =
    let now = DateTime.UtcNow

    let existing =
        { UserId = 3L
          Token = "old"
          ExpiresAt = now
          IsRevoked = false }

    match Steps.createNewToken existing defaultJwtSettings with
    | Ok newToken ->
        Assert.That (newToken.UserId, Is.EqualTo existing.UserId)
        Assert.That (newToken.Token, Is.Not.EqualTo existing.Token)
        Assert.That (newToken.ExpiresAt, Is.GreaterThan now)
        Assert.That (newToken.IsRevoked, Is.False)
    | Error e -> Assert.Fail ($"Expected Ok, but got Error {e}")

[<Test>]
let ``generateJwt - returns AuthResponse with access and refresh tokens`` () =
    let refreshTok = "sample-refresh-token"
    let userId = 42L

    match Steps.generateJwt refreshTok userId defaultJwtSettings with
    | Ok resp ->
        Assert.That (resp.RefreshToken, Is.EqualTo refreshTok)
        Assert.That (resp.AccessToken, Is.Not.Empty)
    | Error e -> Assert.Fail ($"Expected Ok, but got Error {e}")

[<Test>]
let ``validateAndRefresh - full integration creates new token and returns AuthResponse`` () =
    task {
        use conn = new SqliteConnection (connectionString)
        do conn.Open ()
        let initialToken = "refresh token"

        let! _ =
            CommonUtils.insertRefreshTokenRecordForUser
                "Alice"
                (Some initialToken)
                (Some (DateTime.UtcNow + TimeSpan.FromDays (1)))
                conn

        let! authResp = validateAndRefresh initialToken connectionString defaultJwtSettings

        Assert.That (authResp.RefreshToken, Is.Not.EqualTo initialToken)
        Assert.That (authResp.AccessToken, Is.Not.Empty)

        let! allTokens = getAllAsync conn
        let persisted = allTokens |> List.map (fun r -> r.Token)
        Assert.That (persisted, Does.Contain authResp.RefreshToken)
    }
