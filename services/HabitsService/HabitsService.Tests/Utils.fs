module HabitsService.Tests.DataAccess.Utils.Habits

open System.Data
open System.Threading.Tasks

open Dapper.FSharp.PostgreSQL
open HabitsTracker.Helpers

type Record =
    { Id: int
      OwnerUserId: int
      Name: string
      Description: string option }

let habits = table'<Record> "Habits"

let mapToUtilsRecord (habit: HabitsService.Host.DataAccess.Habits.Habit) : Record =
    { Id = habit.Id
      OwnerUserId = habit.OwnerUserId
      Name = habit.Name
      Description = habit.Description }

let deleteAllAsync (conn: IDbConnection) : Task<int> =
    delete {
        for _ in habits do
            deleteAll
    }
    |> conn.DeleteAsync

let getAllAsync (conn: IDbConnection) : Task<Record list> =
    select {
        for h in habits do
            selectAll
    }
    |> conn.SelectAsync<Record>
    |> Task.map List.ofSeq
