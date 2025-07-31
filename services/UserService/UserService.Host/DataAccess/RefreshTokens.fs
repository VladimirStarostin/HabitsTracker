module UserService.Host.DataAccess.RefreshTokens

open System
open System.Data
open System.Threading.Tasks

open Dapper
open Dapper.FSharp.SQLite

open HabitsTracker.Helpers

type RefreshTokenRecord =
    { Id: int64
      Token: string
      UserId: int64
      ExpiresAt: DateTime
      IsRevoked: bool
      CreatedAt: DateTime }

type InsertDto =
    { Token: string
      UserId: int64
      ExpiresAt: DateTime
      IsRevoked: bool
      CreatedAt: DateTime }

let refreshTokens = table'<RefreshTokenRecord> "RefreshTokens"

let insertSingleAsync (insertDto: InsertDto) (conn: IDbConnection) : Task<int64> =
    task {
        use tx = conn.BeginTransaction ()

        let! _ =
            insert {
                into (table'<InsertDto> "RefreshTokens")
                value insertDto
            }
            |> conn.InsertAsync

        let! id = conn.ExecuteScalarAsync<int64> ("SELECT last_insert_rowid()", transaction = tx)

        tx.Commit ()

        return id
    }

let findRecordByTokenAsync (token: string) (conn: IDbConnection) : Task<RefreshTokenRecord option> =
    select {
        for rt in refreshTokens do
            where (rt.Token = token)
    }
    |> conn.SelectAsync<RefreshTokenRecord>
    |> Task.map Seq.tryExactlyOne

let revokeByTokenAsync (token: string) (conn: IDbConnection) =
    update {
        for rt in refreshTokens do
            setColumn rt.IsRevoked true
            where (rt.Token = token)
    }
    |> conn.UpdateAsync

let getAllAsync (conn: IDbConnection) : Task<RefreshTokenRecord list> =
    select {
        for rt in refreshTokens do
            selectAll
    }
    |> conn.SelectAsync<RefreshTokenRecord>
    |> Task.map List.ofSeq

let deleteAllAsync (conn: IDbConnection) =
    delete {
        for rt in refreshTokens do
            deleteAll
    }
    |> conn.DeleteAsync

let deleteByIdAsync (id: int64) (conn: IDbConnection) =
    delete {
        for rt in refreshTokens do
            where (rt.Id = id)
    }
    |> conn.DeleteAsync
