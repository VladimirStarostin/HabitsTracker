module UserService.Tests.Domain.Services.Authentication

open System

open Dapper.FSharp.SQLite
open Npgsql
open NUnit.Framework

open HabitsTracker.Helpers
open UserService.Host.Domain.Common.Types
open UserService.Host.Domain.Common.Operations
open UserService.Host.Domain.Operations
open UserService.Migrations

module UsersUtils = UserService.Tests.DataAccess.Utils.Users

open UserService.Tests.DataAccess.Utils
open UserService.Tests.DataAccess.Utils.RefreshTokens
open UserService.Tests.DataAccess.Utils.Common

let defaultJwtSettings =
    { Secret = "a_very_long_test_secret_key_for_hmacsha256"
      Issuer = "test_issuer"
      Audience = "test_audience"
      AccessTokenValidityMinutes = 60
      RefreshTokenValidityDays = 1 }

[<OneTimeSetUp>]
let Setup () =
    do OptionTypes.register ()
    do MigrationRunner.runMigrations Common.connectionStringForTests (typeof<CreateTablesMigration>.Assembly)

[<SetUp>]
let prepareDbAsync () =
    Common.clearDbAsync Common.connectionStringForTests

[<Test>]
let ``validateInput - missing token returns MissingToken error`` () =
    match Authentication.Steps.validateInput "" with
    | Authentication.PipelineResult.Ok _ -> Assert.Fail ("Expected MissingToken error, but got Ok")
    | Authentication.PipelineResult.Error err ->
        Assert.That (err, Is.EqualTo Authentication.AuthError.MissingToken)

        Assert.That (
            err.toRpcError().Message,
            Is.EqualTo "Status(StatusCode=\"InvalidArgument\", Detail=\"Refresh token was not provided.\")"
        )

[<Test>]
let ``validateInput - valid token returns Ok`` () =
    match Authentication.Steps.validateInput "some-token" with
    | Authentication.PipelineResult.Ok v -> Assert.That (v, Is.EqualTo "some-token")
    | Authentication.PipelineResult.Error e -> Assert.Fail ($"Expected Ok, but got Error {e}")

[<Test>]
let ``validateExistingRefreshToken - expired token returns TokenExpired`` () =
    let expired =
        { UserId = 1
          Token = "t"
          ExpiresAt = DateTime.UtcNow.AddDays (-1.0)
          IsRevoked = false }

    match Authentication.Steps.validateExistingRefreshToken expired with
    | Authentication.PipelineResult.Error Authentication.AuthError.TokenExpired -> ()
    | Authentication.PipelineResult.Error e -> Assert.Fail ($"Expected TokenExpired, but got {e}")
    | Authentication.PipelineResult.Ok _ -> Assert.Fail ("Expected TokenExpired error, but got Ok")

[<Test>]
let ``validateExistingRefreshToken - revoked token returns TokenRevoked`` () =
    let revoked =
        { UserId = 2
          Token = "t2"
          ExpiresAt = DateTime.UtcNow.AddDays (1.0)
          IsRevoked = true }

    match Authentication.Steps.validateExistingRefreshToken revoked with
    | Authentication.PipelineResult.Error Authentication.AuthError.TokenRevoked -> Assert.Pass ()
    | Authentication.PipelineResult.Error e -> Assert.Fail ($"Expected TokenRevoked, but got {e}")
    | Authentication.PipelineResult.Ok _ -> Assert.Fail ("Expected TokenRevoked error, but got Ok")

[<Test>]
let ``createNewToken - generates new token with updated expiry`` () =
    let now = DateTime.UtcNow

    let existing =
        { UserId = 3
          Token = "old"
          ExpiresAt = now
          IsRevoked = false }

    match createRefreshToken existing.UserId defaultJwtSettings with
    | Authentication.PipelineResult.Ok newToken ->
        Assert.That (newToken.UserId, Is.EqualTo existing.UserId)
        Assert.That (newToken.Token, Is.Not.EqualTo existing.Token)
        Assert.That (newToken.ExpiresAt, Is.GreaterThan now)
        Assert.That (newToken.IsRevoked, Is.False)
    | Authentication.PipelineResult.Error e -> Assert.Fail ($"Expected Ok, but got Error {e}")

[<Test>]
let ``generateJwt - returns AuthResponse with access and refresh tokens`` () =
    let refreshTok = "sample-refresh-token"
    let userId = 42

    match generateJwt refreshTok userId defaultJwtSettings with
    | Authentication.PipelineResult.Ok resp ->
        Assert.That (resp.RefreshToken, Is.EqualTo refreshTok)
        Assert.That (resp.AccessToken, Is.Not.Empty)
    | Authentication.PipelineResult.Error e -> Assert.Fail ($"Expected Ok, but got Error {e}")

[<Test>]
let ``validateAndRefresh - full integration creates new token and returns AuthResponse`` () =
    task {
        use conn = new NpgsqlConnection (Common.connectionStringForTests)
        do conn.Open ()
        let initialToken = "refresh token"

        let! _ =
            insertRefreshTokenRecordForUser
                "Alice"
                (Some initialToken)
                (Some (DateTime.UtcNow + TimeSpan.FromDays (1)))
                conn

        match! Authentication.runPipelineAsync initialToken defaultJwtSettings conn with
        | Authentication.PipelineResult.Ok authResp ->
            Assert.That (authResp.RefreshToken, Is.Not.EqualTo initialToken)
            Assert.That (authResp.AccessToken, Is.Not.Empty)

            let! allTokens = getAllAsync conn
            let persisted = allTokens |> List.map (fun r -> r.Token)
            Assert.That (persisted, Does.Contain authResp.RefreshToken)
        | _ -> Assert.Fail ()
    }
