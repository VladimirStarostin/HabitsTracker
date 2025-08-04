module HabitsService.Host.DataAccess.Tests.Utils.Habits

open System.Data

open Dapper.FSharp.PostgreSQL

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

let deleteAllAsync (conn: IDbConnection) =
    delete {
        for _ in habits do
            deleteAll
    }
    |> conn.DeleteAsync

let getAllAsync (conn: IDbConnection) =
    select {
        for h in habits do
            selectAll
    }
    |> conn.SelectAsync<Record>
