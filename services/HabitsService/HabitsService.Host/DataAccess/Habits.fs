module HabitsService.Host.DataAccess.Habits

open System.Data

open Dapper
open Dapper.FSharp.SQLite

type Habit = { Id: int64; Name: string }

type InsertDto = { Name: string }

let habits = table'<Habit> "Habits"

let getAllAsync (conn: IDbConnection) =
    select {
        for h in habits do
            selectAll
    }
    |> conn.SelectAsync<Habit>

let getByIdsAsync (ids: int64 list) (conn: IDbConnection) =
    select {
        for h in habits do
            where (isIn h.Id ids)
            selectAll
    }
    |> conn.SelectAsync<Habit>

let insertSingleAsync (insertDto: InsertDto) (conn: IDbConnection) =
    task {
        use tx = conn.BeginTransaction()

        let! _ =
            insert {
                into (table'<InsertDto> "Habits")
                value insertDto
            }
            |> fun q -> conn.InsertAsync(q, tx)

        let! id = conn.ExecuteScalarAsync<int>("SELECT last_insert_rowid()", transaction = tx)

        tx.Commit()

        return int id
    }

let deleteAllAsync (conn: IDbConnection) =
    delete {
        for _ in habits do
            deleteAll
    }
    |> conn.DeleteAsync

let deleteByIdAsync (id: int64) (conn: IDbConnection) =
    delete {
        for h in habits do
            where (h.Id = id)
    }
    |> conn.DeleteAsync
