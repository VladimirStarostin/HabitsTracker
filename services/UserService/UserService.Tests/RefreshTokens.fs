module UserService.Tests.DataAccess.RefreshTokens

open System
open System.Threading.Tasks

open Microsoft.Data.Sqlite

open NUnit.Framework
open Dapper.FSharp.SQLite

open HabitsTracker.Helpers
open UserService.Migrations

module RefreshTokens = UserService.Host.DataAccess.RefreshTokens
module Users = UserService.Host.DataAccess.Users
module Utils = UserService.Tests.CommonUtils

let connectionString = "Data Source=file:memdb1?mode=memory&cache=shared"

[<OneTimeSetUp>]
let Setup () =
    do OptionTypes.register ()
    do MigrationRunner.runMigrations connectionString (typeof<CreateTablesMigration>.Assembly)

[<SetUp>]
let clearDbAsync () =
    task {
        use conn = new SqliteConnection (connectionString)
        let! _ = RefreshTokens.deleteAllAsync conn
        let! _ = Users.deleteAllAsync conn
        return ()
    }

[<Test>]
let ``Select from empty RefreshTokens returns empty`` () =
    task {
        use conn = new SqliteConnection (connectionString)
        do conn.Open ()
        let! result = RefreshTokens.getAllAsync conn
        Assert.That (result, Is.Empty)
    }

[<Test>]
let ``insertSingleAsync - fails on duplicate token`` () =
    task {
        use conn = new SqliteConnection (connectionString)
        do conn.Open ()

        let token = "token to fail"

        let! record1 = Utils.insertRefreshTokenRecordForUser "Alice" (Some token) None conn

        let ex =
            Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException> (fun _ ->
                Utils.insertRefreshTokenRecordForUser "Bob" (Some token) None conn
            )

        Assert.That (ex.Message, Is.EqualTo "SQLite Error 19: 'UNIQUE constraint failed: RefreshTokens.Token'.")
    }

[<Test>]
let ``findRecordByTokenAsync - token selectivity`` () =
    task {
        use conn = new SqliteConnection (connectionString)
        do conn.Open ()
        let! record1 = Utils.insertRefreshTokenRecordForUser "Alice" None None conn
        let! record2 = Utils.insertRefreshTokenRecordForUser "Bob" None None conn
        let! record3 = Utils.insertRefreshTokenRecordForUser "Ceca" None None conn

        let expectedInTable: RefreshTokens.RefreshTokenRecord list =
            [ record1; record2; record3 ]

        let! actualInTable = RefreshTokens.getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let expected: RefreshTokens.RefreshTokenRecord option = Some record2
        let! actual = RefreshTokens.findRecordByTokenAsync record2.Token conn
        Assert.That (actual, Is.EqualTo expected)
    }

[<Test>]
let ``revokeByTokenAsync - token selectivity`` () =
    task {
        use conn = new SqliteConnection (connectionString)
        do conn.Open ()

        let! record1 = Utils.insertRefreshTokenRecordForUser "Alice" None None conn
        let! record2 = Utils.insertRefreshTokenRecordForUser "Bob" None None conn

        let expectedInTable: RefreshTokens.RefreshTokenRecord list = [ record1; record2 ]

        let! actualInTable = RefreshTokens.getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let expectedInTable: RefreshTokens.RefreshTokenRecord list =
            [ { record1 with
                  IsRevoked = true }
              record2 ]

        let! affectedRowsCount = RefreshTokens.revokeByTokenAsync record1.Token conn
        Assert.That (affectedRowsCount, Is.EqualTo 1)

        let! actualInTable = RefreshTokens.getAllAsync conn
        Assert.That (expectedInTable, Is.EquivalentTo actualInTable)
    }

[<Test>]
let ``findRecordByTokenAsync - unexistent token - no rows returned`` () =
    task {
        use conn = new SqliteConnection (connectionString)
        do conn.Open ()

        let! record1 = Utils.insertRefreshTokenRecordForUser "Alice" None None conn
        let! record2 = Utils.insertRefreshTokenRecordForUser "Bob" None None conn

        let expectedInTable: RefreshTokens.RefreshTokenRecord list = [ record1; record2 ]

        let! actualInTable = RefreshTokens.getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let! actual = RefreshTokens.findRecordByTokenAsync "" conn
        Assert.That (Option.isNone actual)
    }
