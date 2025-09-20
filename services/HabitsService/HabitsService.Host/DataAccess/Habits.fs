module HabitsService.Host.DataAccess.Habits

open System.Threading.Tasks
open System.Data

open Dapper
open Dapper.FSharp.PostgreSQL

open HabitsTracker.Helpers

type Habit =
    { Id: int
      OwnerUserId: int
      Name: string
      Description: string option }

type InsertDto =
    { OwnerUserId: int
      Name: string
      Description: string option }

let habits = table'<Habit> "Habits"

let getByIdsAsync (ids: int Set) (conn: IDbConnection) : Task<Habit list> =
    if ids.IsEmpty then
        Task.FromResult List.empty
    else
        let idsList = ids |> Set.toList

        select {
            for h in habits do
                where (isIn h.Id idsList)
                selectAll
        }
        |> conn.SelectAsync<Habit>
        |> Task.map List.ofSeq

let getByUserIdsAsync (userIds: int Set) (conn: IDbConnection) : Task<Habit list> =
    if userIds.IsEmpty then
        Task.FromResult List.empty
    else
        let userIdsList = userIds |> Set.toList

        select {
            for h in habits do
                where (isIn h.OwnerUserId userIdsList)
                selectAll
        }
        |> conn.SelectAsync<Habit>
        |> Task.map List.ofSeq

let insertSingleAsync (insertDto: InsertDto) (conn: IDbConnection) : Task<Habit> =
    insert {
        into (table'<InsertDto> "Habits")
        value insertDto
    }
    |> conn.InsertOutputAsync<InsertDto, Habit>
    |> Task.map Seq.exactlyOne

let deleteByIdsAsync (ids: int Set) (userId: int) (conn: IDbConnection) : Task<int> =
    if ids.IsEmpty then
        Task.FromResult 0
    else
        let idsList = ids |> Set.toList

        delete {
            for h in habits do
                where (isIn h.Id idsList && h.OwnerUserId = userId)
        }
        |> conn.DeleteAsync
