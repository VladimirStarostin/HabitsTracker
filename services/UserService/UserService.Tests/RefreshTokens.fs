module UserService.Tests.DataAccess.RefreshTokens

open NUnit.Framework
open Npgsql
open Dapper.FSharp.SQLite

open HabitsTracker.Helpers
open UserService.Migrations

module RefreshTokens = UserService.Host.DataAccess.RefreshTokens
module Users = UserService.Host.DataAccess.Users

module CommonTestMethods = UserService.Tests.DataAccess.Utils.CommonTestMethods
module RefreshTokenUtils = UserService.Tests.DataAccess.Utils.RefreshTokenUtils
module UsersUtils = UserService.Tests.DataAccess.Utils.Users

let connectionString =
    "Host=localhost;Port=5432;Database=users_db;Username=habits_tracker_user;Password=1W@nt70m3J0b"

[<OneTimeSetUp>]
let Setup () =
    do OptionTypes.register ()
    do MigrationRunner.runMigrations connectionString (typeof<CreateTablesMigration>.Assembly)

[<SetUp>]
let prepareDbAsync () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        let! _ = CommonTestMethods.clearDbAsync conn
        return ()
    }

[<Test>]
let ``RefreshTokenUtils.getAllAsync - select from empty RefreshTokens returns empty`` () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        do conn.Open ()
        let! result = RefreshTokenUtils.getAllAsync conn
        Assert.That (result, Is.Empty)
    }

[<Test>]
let ``insertSingleAsync - fails on duplicate token`` () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        do conn.Open ()

        let token = "token to fail"

        let! _ = CommonTestMethods.insertRefreshTokenRecordForUser "Alice" (Some token) None conn

        let ex =
            Assert.ThrowsAsync<Npgsql.PostgresException> (fun _ ->
                CommonTestMethods.insertRefreshTokenRecordForUser "Bob" (Some token) None conn
            )

        Assert.That (
            ex.Message.StartsWith "23505: duplicate key value violates unique constraint \"IX_RefreshTokens_Token\""
        )
    }

[<Test>]
let ``findRecordByTokenAsync - token selectivity`` () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        do conn.Open ()
        let! record1 = CommonTestMethods.insertRefreshTokenRecordForUser "Alice" None None conn
        let! record2 = CommonTestMethods.insertRefreshTokenRecordForUser "Bob" None None conn
        let! record3 = CommonTestMethods.insertRefreshTokenRecordForUser "Ceca" None None conn

        let expectedInTable: RefreshTokenUtils.Record list =
            [ record1; record2; record3 ]
            |> List.map RefreshTokenUtils.mapToUtilsRecord

        let! actualInTable = RefreshTokenUtils.getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let expected: RefreshTokens.RefreshTokenRecord option = Some record2
        let! actual = RefreshTokens.findRecordByTokenAsync record2.Token conn
        Assert.That (actual, Is.EqualTo expected)
    }

[<Test>]
let ``revokeByTokenAsync - token selectivity`` () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        do conn.Open ()

        let! record1 = CommonTestMethods.insertRefreshTokenRecordForUser "Alice" None None conn
        let! record2 = CommonTestMethods.insertRefreshTokenRecordForUser "Bob" None None conn

        let expectedInTable: RefreshTokenUtils.Record list =
            [ record1; record2 ]
            |> List.map RefreshTokenUtils.mapToUtilsRecord

        let! actualInTable = RefreshTokenUtils.getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let! affectedRowsCount = RefreshTokens.revokeByTokenAsync record1.Token conn
        Assert.That (affectedRowsCount, Is.EqualTo 1)

        let expectedInTable: RefreshTokenUtils.Record list =
            [ { record1 with
                  IsRevoked = true }
              record2 ]
            |> List.map RefreshTokenUtils.mapToUtilsRecord

        let! actualInTable = RefreshTokenUtils.getAllAsync conn
        Assert.That (expectedInTable, Is.EquivalentTo actualInTable)
    }

[<Test>]
let ``findRecordByTokenAsync - unexistent token - no rows returned`` () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        do conn.Open ()

        let! record1 = CommonTestMethods.insertRefreshTokenRecordForUser "Alice" None None conn
        let! record2 = CommonTestMethods.insertRefreshTokenRecordForUser "Bob" None None conn

        let expectedInTable: RefreshTokenUtils.Record list =
            [ record1; record2 ]
            |> List.map RefreshTokenUtils.mapToUtilsRecord

        let! actualInTable = RefreshTokenUtils.getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let! actual = RefreshTokens.findRecordByTokenAsync "" conn
        Assert.That (Option.isNone actual)
    }
