[<RequireQualifiedAccessAttribute>]
module HabitsTracker.Helpers.Postgres

open System.Data
open System.Threading.Tasks

open Npgsql

let getConnection (connectionString: string) : IDbConnection =
    let conn = new NpgsqlConnection (connectionString)
    conn.Open ()
    conn :> IDbConnection

let executeWithConnection<'a> (connectionString: string) (work: IDbConnection -> Task<'a>) : Task<'a> =
    task {
        use conn = getConnection connectionString
        return! work conn
    }
