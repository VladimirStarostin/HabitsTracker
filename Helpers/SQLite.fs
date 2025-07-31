[<RequireQualifiedAccessAttribute>]
module HabitsTracker.Helpers.SQLite

open System.Data
open System.Threading.Tasks

open Microsoft.Data.Sqlite

let getConnection (connectionString: string) : IDbConnection =
    let conn = new SqliteConnection (connectionString)
    conn.Open ()
    conn :> IDbConnection

let createInMemoryConnection () = getConnection "Data Source=:memory:"

let executeWithConnection<'a> (connectionString: string) (work: IDbConnection -> Task<'a>) : Task<'a> =
    task {
        use conn = getConnection connectionString
        return! work conn
    }
