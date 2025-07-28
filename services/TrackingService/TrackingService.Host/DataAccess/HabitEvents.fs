module TrackingService.Host.DataAccess.HabitEvents

open System
open System.Data

open Dapper
open Dapper.FSharp.SQLite

type HabitEvent =
    { Id: int
      HabitId: int
      UserId: int
      Date: DateTimeOffset }

type InsertDto =
    { HabitId: int
      UserId: int
      Date: DateTimeOffset }

let habitEvents = table'<HabitEvent> "HabitEvents"

let getAllAsync (conn: IDbConnection) =
    select {
        for h in habitEvents do
            selectAll
    }
    |> conn.SelectAsync<HabitEvent>

let getByIdsAsync (ids: int list) (conn: IDbConnection) =
    select {
        for h in habitEvents do
            where (isIn h.Id ids)
            selectAll
    }
    |> conn.SelectAsync<HabitEvent>

let insertSingleAsync (insertDto: InsertDto) (conn: IDbConnection) =
    task {
        use tx = conn.BeginTransaction()

        let! _ =
            insert {
                into (table'<InsertDto> "HabitEvents")
                value insertDto
            }
            |> fun q -> conn.InsertAsync(q, tx)

        let! id = conn.ExecuteScalarAsync<int>("SELECT last_insert_rowid()", transaction = tx)

        tx.Commit()

        return int id
    }

let deleteAllAsync (conn: IDbConnection) =
    delete {
        for _ in habitEvents do
            deleteAll
    }
    |> conn.DeleteAsync

let deleteByIdAsync (id: int) (conn: IDbConnection) =
    delete {
        for h in habitEvents do
            where (h.Id = id)
    }
    |> conn.DeleteAsync
