[<RequireQualifiedAccessAttribute>]
module UserService.Host.DataAccess.RefreshTokens

open System
open System.Data
open System.Threading.Tasks

open Dapper.FSharp.PostgreSQL

open HabitsTracker.Helpers

type RefreshTokenRecord =
    { Id: int
      Token: string
      UserId: int
      ExpiresAt: DateTime
      IsRevoked: bool
      CreatedAt: DateTime }

type InsertDto =
    { Token: string
      UserId: int
      ExpiresAt: DateTime
      IsRevoked: bool
      CreatedAt: DateTime }

let refreshTokens = table'<RefreshTokenRecord> "RefreshTokens"

let insertSingleAsync (insertDto: InsertDto) (conn: IDbConnection) : Task<RefreshTokenRecord> =
    insert {
        into (table'<InsertDto> "RefreshTokens")
        value insertDto
    }
    |> conn.InsertOutputAsync<InsertDto, RefreshTokenRecord>
    |> Task.map Seq.exactlyOne

let findRecordByTokenAsync (token: string) (conn: IDbConnection) : Task<RefreshTokenRecord option> =
    select {
        for rt in refreshTokens do
            where (rt.Token = token)
    }
    |> conn.SelectAsync<RefreshTokenRecord>
    |> Task.map Seq.tryExactlyOne

let revokeByTokenAsync (token: string) (conn: IDbConnection) : Task<int> =
    update {
        for rt in refreshTokens do
            setColumn rt.IsRevoked true
            where (rt.Token = token)
    }
    |> conn.UpdateAsync

let revokeByUserIdAsync (userId: int) (conn: IDbConnection) : Task<int> =
    update {
        for rt in refreshTokens do
            setColumn rt.IsRevoked true
            where (rt.UserId = userId && rt.IsRevoked = false)
    }
    |> conn.UpdateAsync

let deleteByIdAsync (id: int) (conn: IDbConnection) =
    delete {
        for rt in refreshTokens do
            where (rt.Id = id)
    }
    |> conn.DeleteAsync

let deleteByUserIdAsync (userId: int) (conn: IDbConnection) =
    delete {
        for rt in refreshTokens do
            where (rt.UserId = userId)
    }
    |> conn.DeleteAsync
