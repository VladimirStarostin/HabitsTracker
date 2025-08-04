module TrackingService.Tests.DataAccess.Utils.HabitEvents

open System
open System.Data
open System.Threading.Tasks

open Dapper.FSharp.PostgreSQL

open HabitsTracker.Helpers

type Record =
    { Id: int
      HabitId: int
      UserId: int
      Date: DateTimeOffset }

let private habitEvents = table'<Record> "HabitEvents"

let mapToUtilsRecord (habitEent: TrackingService.Host.DataAccess.HabitEvents.HabitEvent) : Record =
    { Id = habitEent.Id
      HabitId = habitEent.HabitId
      UserId = habitEent.UserId
      Date = habitEent.Date }

let getAllAsync (conn: IDbConnection) : Task<Record list> =
    select {
        for _ in habitEvents do
            selectAll
    }
    |> conn.SelectAsync<Record>
    |> Task.map Seq.toList

let deleteAllAsync (conn: IDbConnection) : Task<int> =
    delete {
        for _ in habitEvents do
            deleteAll
    }
    |> conn.DeleteAsync
