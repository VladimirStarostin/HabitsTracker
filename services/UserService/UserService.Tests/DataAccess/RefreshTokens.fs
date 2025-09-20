module UserService.Tests.DataAccess.RefreshTokens

open NUnit.Framework
open Npgsql
open Dapper.FSharp.SQLite

open HabitsTracker.Helpers
open UserService.Migrations

module RefreshTokens = UserService.Host.DataAccess.RefreshTokens
module Users = UserService.Host.DataAccess.Users

module Common = UserService.Tests.DataAccess.Utils.Common
module RefreshTokens = UserService.Tests.DataAccess.Utils.RefreshTokens
module UsersUtils = UserService.Tests.DataAccess.Utils.Users

[<OneTimeSetUp>]
let Setup () =
    do OptionTypes.register ()
    do MigrationRunner.runMigrations Common.connectionStringForTests (typeof<CreateTablesMigration>.Assembly)

[<SetUp>]
let prepareDbAsync () = Common.clearDbAsync ()

[<Test>]
let ``RefreshTokenUtils.getAllAsync - select from empty RefreshTokens returns empty`` () =
    task {
        use conn = new NpgsqlConnection (Common.connectionStringForTests)
        do conn.Open ()
        let! result = RefreshTokens.getAllAsync conn
        Assert.That (result, Is.Empty)
    }

[<Test>]
let ``insertSingleAsync - fails on duplicate token`` () =
    task {
        use conn = new NpgsqlConnection (Common.connectionStringForTests)
        do conn.Open ()

        let token = "token to fail"

        let! _ = Common.insertRefreshTokenRecordForUser "Alice" (Some token) None conn

        let ex =
            Assert.ThrowsAsync<Npgsql.PostgresException> (fun _ ->
                Common.insertRefreshTokenRecordForUser "Bob" (Some token) None conn
            )

        Assert.That (
            ex.Message.StartsWith "23505: duplicate key value violates unique constraint \"IX_RefreshTokens_Token\""
        )
    }

[<Test>]
let ``findRecordByTokenAsync - token selectivity`` () =
    task {
        use conn = new NpgsqlConnection (Common.connectionStringForTests)
        do conn.Open ()
        let! record1 = Common.insertRefreshTokenRecordForUser "Alice" None None conn
        let! record2 = Common.insertRefreshTokenRecordForUser "Bob" None None conn
        let! record3 = Common.insertRefreshTokenRecordForUser "Ceca" None None conn

        let expectedInTable: RefreshTokens.Record list =
            [ record1; record2; record3 ]
            |> List.map RefreshTokens.mapToUtilsRecord

        let! actualInTable = RefreshTokens.getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let expected: RefreshTokens.RefreshTokenRecord option = Some record2
        let! actual = RefreshTokens.findRecordByTokenAsync record2.Token conn
        Assert.That (actual, Is.EqualTo expected)
    }

[<Test>]
let ``revokeByTokenAsync - token selectivity`` () =
    task {
        use conn = new NpgsqlConnection (Common.connectionStringForTests)
        do conn.Open ()

        let! record1 = Common.insertRefreshTokenRecordForUser "Alice" None None conn
        let! record2 = Common.insertRefreshTokenRecordForUser "Bob" None None conn

        let expectedInTable: RefreshTokens.Record list =
            [ record1; record2 ]
            |> List.map RefreshTokens.mapToUtilsRecord

        let! actualInTable = RefreshTokens.getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let! affectedRowsCount = RefreshTokens.revokeByTokenAsync record1.Token conn
        Assert.That (affectedRowsCount, Is.EqualTo 1)

        let expectedInTable: RefreshTokens.Record list =
            [ { record1 with
                  IsRevoked = true }
              record2 ]
            |> List.map RefreshTokens.mapToUtilsRecord

        let! actualInTable = RefreshTokens.getAllAsync conn
        Assert.That (expectedInTable, Is.EquivalentTo actualInTable)
    }

[<Test>]
let ``revokeByUserIdAsync - token selectivity`` () =
    task {
        use conn = new NpgsqlConnection (Common.connectionStringForTests)
        do conn.Open ()

        let! record1 = Common.insertRefreshTokenRecordForUser "Alice" None None conn
        let! record2 = Common.insertRefreshTokenRecordForUser "Bob" None None conn

        let expectedInTable: RefreshTokens.Record list =
            [ record1; record2 ]
            |> List.map RefreshTokens.mapToUtilsRecord

        let! actualInTable = RefreshTokens.getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let! affectedRowsCount = RefreshTokens.revokeByUserIdAsync record1.UserId conn
        Assert.That (affectedRowsCount, Is.EqualTo 1)

        let expectedInTable: RefreshTokens.Record list =
            [ { record1 with
                  IsRevoked = true }
              record2 ]
            |> List.map RefreshTokens.mapToUtilsRecord

        let! actualInTable = RefreshTokens.getAllAsync conn
        Assert.That (expectedInTable, Is.EquivalentTo actualInTable)
    }

[<Test>]
let ``findRecordByTokenAsync - unexistent token - no rows returned`` () =
    task {
        use conn = new NpgsqlConnection (Common.connectionStringForTests)
        do conn.Open ()

        let! record1 = Common.insertRefreshTokenRecordForUser "Alice" None None conn
        let! record2 = Common.insertRefreshTokenRecordForUser "Bob" None None conn

        let expectedInTable: RefreshTokens.Record list =
            [ record1; record2 ]
            |> List.map RefreshTokens.mapToUtilsRecord

        let! actualInTable = RefreshTokens.getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let! actual = RefreshTokens.findRecordByTokenAsync "" conn
        Assert.That (Option.isNone actual)
    }
