[<RequireQualifiedAccessAttribute>]
module UserService.Host.DataAccess.Users

open System.Data
open System.Threading.Tasks

open Dapper.FSharp.PostgreSQL

open HabitsTracker.Helpers

type User =
    { Id: int
      Email: string
      Name: string
      PasswordHash: string }

type InsertDto =
    { Email: string
      Name: string
      PasswordHash: string }

let users = table'<User> "Users"

let getByIdsAsync (ids: int Set) (conn: IDbConnection) : Task<User list> =
    if ids.IsEmpty then
        Task.FromResult List.empty
    else
        let usersList = ids |> Set.toList

        select {
            for u in users do
                where (isIn u.Id usersList)
                selectAll
        }
        |> conn.SelectAsync<User>
        |> Task.map Seq.toList

let getByEmailAsync (email: string) (conn: IDbConnection) : Task<User option> =
    select {
        for u in users do
            where (u.Email = email)
    }
    |> conn.SelectAsync<User>
    |> Task.map Seq.tryExactlyOne

let insertSingleAsync (insertDto: InsertDto) (conn: IDbConnection) : Task<User> =
    insert {
        into (table'<InsertDto> "Users")
        value insertDto
    }
    |> conn.InsertOutputAsync<InsertDto, User>
    |> Task.map Seq.exactlyOne

let deleteByIdAsync (id: int) (conn: IDbConnection) =
    delete {
        for h in users do
            where (h.Id = id)
    }
    |> conn.DeleteAsync
