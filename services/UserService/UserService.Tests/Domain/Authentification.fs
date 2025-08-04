module UserService.Tests.Domain.Services.Authentication

open System

open Dapper.FSharp.SQLite
open Npgsql
open NUnit.Framework

open HabitsTracker.Helpers
open UserService.Host.Domain.Common.Types
open UserService.Host.Domain.Common.Operations
open UserService.Host.Domain.Operations.Authentication
open UserService.Migrations

module UsersUtils = UserService.Tests.DataAccess.Utils.Users

open UserService.Tests.DataAccess.Utils
open UserService.Tests.DataAccess.Utils.RefreshTokenUtils
open UserService.Tests.DataAccess.Utils.CommonTestMethods

let connectionString =
    "Host=localhost;Port=5432;Database=users_db;Username=habits_tracker_user;Password=1W@nt70m3J0b"

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
let prepareDbAsync () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        let! _ = CommonTestMethods.clearDbAsync conn
        return ()
    }

[<Test>]
let ``validateInput - missing token returns MissingToken error`` () =
    match Steps.validateInput "" with
    | AuthenticationPipelineResult.Ok _ -> Assert.Fail ("Expected MissingToken error, but got Ok")
    | AuthenticationPipelineResult.Error err ->
        Assert.That (err, Is.EqualTo AuthError.MissingToken)
        Assert.That (err.ToErrMessage (), Is.EqualTo "Refresh token was not provided.")

[<Test>]
let ``validateInput - valid token returns Ok`` () =
    match Steps.validateInput "some-token" with
    | AuthenticationPipelineResult.Ok v -> Assert.That (v, Is.EqualTo "some-token")
    | AuthenticationPipelineResult.Error e -> Assert.Fail ($"Expected Ok, but got Error {e}")

[<Test>]
let ``validateExistingRefreshToken - expired token returns TokenExpired`` () =
    let expired =
        { UserId = 1
          Token = "t"
          ExpiresAt = DateTime.UtcNow.AddDays (-1.0)
          IsRevoked = false }

    match Steps.validateExistingRefreshToken expired with
    | AuthenticationPipelineResult.Error AuthError.TokenExpired -> ()
    | AuthenticationPipelineResult.Error e -> Assert.Fail ($"Expected TokenExpired, but got {e}")
    | AuthenticationPipelineResult.Ok _ -> Assert.Fail ("Expected TokenExpired error, but got Ok")

[<Test>]
let ``validateExistingRefreshToken - revoked token returns TokenRevoked`` () =
    let revoked =
        { UserId = 2
          Token = "t2"
          ExpiresAt = DateTime.UtcNow.AddDays (1.0)
          IsRevoked = true }

    match Steps.validateExistingRefreshToken revoked with
    | AuthenticationPipelineResult.Error AuthError.TokenRevoked -> Assert.Pass ()
    | AuthenticationPipelineResult.Error e -> Assert.Fail ($"Expected TokenRevoked, but got {e}")
    | AuthenticationPipelineResult.Ok _ -> Assert.Fail ("Expected TokenRevoked error, but got Ok")

[<Test>]
let ``createNewToken - generates new token with updated expiry`` () =
    let now = DateTime.UtcNow

    let existing =
        { UserId = 3
          Token = "old"
          ExpiresAt = now
          IsRevoked = false }

    match createRefreshToken existing.UserId defaultJwtSettings with
    | AuthenticationPipelineResult.Ok newToken ->
        Assert.That (newToken.UserId, Is.EqualTo existing.UserId)
        Assert.That (newToken.Token, Is.Not.EqualTo existing.Token)
        Assert.That (newToken.ExpiresAt, Is.GreaterThan now)
        Assert.That (newToken.IsRevoked, Is.False)
    | AuthenticationPipelineResult.Error e -> Assert.Fail ($"Expected Ok, but got Error {e}")

[<Test>]
let ``generateJwt - returns AuthResponse with access and refresh tokens`` () =
    let refreshTok = "sample-refresh-token"
    let userId = 42

    match generateJwt refreshTok userId defaultJwtSettings with
    | AuthenticationPipelineResult.Ok resp ->
        Assert.That (resp.RefreshToken, Is.EqualTo refreshTok)
        Assert.That (resp.AccessToken, Is.Not.Empty)
    | AuthenticationPipelineResult.Error e -> Assert.Fail ($"Expected Ok, but got Error {e}")

[<Test>]
let ``validateAndRefresh - full integration creates new token and returns AuthResponse`` () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        do conn.Open ()
        let initialToken = "refresh token"

        let! _ =
            insertRefreshTokenRecordForUser
                "Alice"
                (Some initialToken)
                (Some (DateTime.UtcNow + TimeSpan.FromDays (1)))
                conn

        match! validateAndRefreshAsync initialToken defaultJwtSettings conn with
        | AuthenticationPipelineResult.Ok authResp ->
            Assert.That (authResp.RefreshToken, Is.Not.EqualTo initialToken)
            Assert.That (authResp.AccessToken, Is.Not.Empty)

            let! allTokens = getAllAsync conn
            let persisted = allTokens |> List.map (fun r -> r.Token)
            Assert.That (persisted, Does.Contain authResp.RefreshToken)
        | _ -> Assert.Fail ()
    }
