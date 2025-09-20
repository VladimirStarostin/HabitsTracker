namespace UserService.Tests.DataAccess.Utils

open System
open System.Data
open System.Threading.Tasks

open Dapper.FSharp.PostgreSQL

open HabitsTracker.Helpers

module Users =
    type Record =
        { Id: int
          Email: string
          Name: string }

    let private users = table'<Record> "Users"

    let mapToUtilsRecord (user: UserService.Host.DataAccess.Users.User) : Record =
        { Id = user.Id
          Name = user.Name
          Email = user.Email }

    let getAllAsync (conn: IDbConnection) : Task<Record list> =
        select {
            for u in users do
                selectAll
        }
        |> conn.SelectAsync<Record>
        |> Task.map Seq.toList

    let deleteAllAsync (conn: IDbConnection) =
        delete {
            for _ in users do
                deleteAll
        }
        |> conn.DeleteAsync

module RefreshTokens =
    type Record =
        { Id: int
          Token: string
          UserId: int
          ExpiresAt: DateTime
          IsRevoked: bool
          CreatedAt: DateTime }

    let private refreshTokens = table'<Record> "RefreshTokens"

    let mapToUtilsRecord (refreshTokenRecord: UserService.Host.DataAccess.RefreshTokens.RefreshTokenRecord) : Record =
        { Id = refreshTokenRecord.Id
          Token = refreshTokenRecord.Token
          UserId = refreshTokenRecord.UserId
          ExpiresAt = refreshTokenRecord.ExpiresAt
          IsRevoked = refreshTokenRecord.IsRevoked
          CreatedAt = refreshTokenRecord.CreatedAt }

    let getAllAsync (conn: IDbConnection) : Task<Record list> =
        select {
            for rt in refreshTokens do
                selectAll
        }
        |> conn.SelectAsync<Record>
        |> Task.map List.ofSeq

    let deleteAllAsync (conn: IDbConnection) =
        delete {
            for rt in refreshTokens do
                deleteAll
        }
        |> conn.DeleteAsync
