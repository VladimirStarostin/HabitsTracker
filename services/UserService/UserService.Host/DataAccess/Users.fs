module UserService.Host.DataAccess.Users

open System.Data

open Dapper
open Dapper.FSharp.SQLite

type User =
    { Id: int; Email: string; Name: string }

type InsertDto = { Email: string; Name: string }

let users = table'<User> "Users"

let getAllAsync (conn: IDbConnection) =
    select {
        for h in users do
            selectAll
    }
    |> conn.SelectAsync<User>

let getByIdsAsync (ids: int list) (conn: IDbConnection) =
    select {
        for h in users do
            where (isIn h.Id ids)
            selectAll
    }
    |> conn.SelectAsync<User>

let insertSingleAsync (insertDto: InsertDto) (conn: IDbConnection) =
    task {
        use tx = conn.BeginTransaction()

        let! _ =
            insert {
                into (table'<InsertDto> "Users")
                value insertDto
            }
            |> fun q -> conn.InsertAsync(q, tx)

        let! id = conn.ExecuteScalarAsync<int>("SELECT last_insert_rowid()", transaction = tx)

        tx.Commit()

        return int id
    }

let deleteAllAsync (conn: IDbConnection) =
    delete {
        for _ in users do
            deleteAll
    }
    |> conn.DeleteAsync

let deleteByIdAsync (id: int) (conn: IDbConnection) =
    delete {
        for h in users do
            where (h.Id = id)
    }
    |> conn.DeleteAsync
