module HabitsTracker.Helpers.Tests.SQLite

open System.Data

open Microsoft.Data.Sqlite

let createInMemoryConnection () =
    let conn = new SqliteConnection("Data Source=:memory:")
    conn.Open()
    conn :> IDbConnection