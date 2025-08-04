module TrackingService.Host.DataAccess.HabitEvents

open System
open System.Data
open System.Threading.Tasks

open Dapper.FSharp.PostgreSQL

open HabitsTracker.Helpers

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

let getByHabitIdsAsync (habitIds: int Set) (conn: IDbConnection) : Task<HabitEvent Set> =
    if habitIds.IsEmpty then
        Task.FromResult Set.empty
    else
        let habitIdsList = habitIds |> Set.toList

        select {
            for e in habitEvents do
                where (isIn e.HabitId habitIdsList)
                selectAll
        }
        |> conn.SelectAsync<HabitEvent>
        |> Task.map Set.ofSeq

let getByIdsAsync (ids: int Set) (conn: IDbConnection) : Task<HabitEvent Set> =
    if ids.IsEmpty then
        Task.FromResult Set.empty
    else
        let idsList = ids |> Set.toList

        select {
            for e in habitEvents do
                where (isIn e.Id idsList)
                selectAll
        }
        |> conn.SelectAsync<HabitEvent>
        |> Task.map Set.ofSeq

let insertSingleAsync (insertDto: InsertDto) (conn: IDbConnection) : Task<HabitEvent> =
    insert {
        into (table'<InsertDto> "HabitEvents")
        value insertDto
    }
    |> conn.InsertOutputAsync<InsertDto, HabitEvent>
    |> Task.map Seq.exactlyOne

let deleteByIdAsync (id: int) (conn: IDbConnection) =
    delete {
        for h in habitEvents do
            where (h.Id = id)
    }
    |> conn.DeleteAsync
