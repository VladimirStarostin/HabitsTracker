module UserService.Tests.DataAccess.Users

open System

open Npgsql

open Dapper.FSharp.SQLite
open NUnit.Framework

open HabitsTracker.Helpers
open UserService.Host.DataAccess
open UserService.Migrations

open UserService.Tests.DataAccess.Utils.Common
open UserService.Tests.DataAccess.Utils.Users

[<OneTimeSetUp>]
let Setup () =
    do OptionTypes.register ()
    do MigrationRunner.runMigrations connectionStringForTests (typeof<CreateTablesMigration>.Assembly)

[<SetUp>]
let prepareDbAsync () = clearDbAsync connectionStringForTests

[<Test>]
let ``Select from empty Habits returns empty`` () =
    task {
        use conn = new NpgsqlConnection (connectionStringForTests)
        do conn.Open ()
        let! result = getAllAsync conn
        Assert.That (result, Is.Empty)
    }

let date = DateTimeOffset.Parse "2025-07-28"

[<Test>]
let ``getByIdsAsync - ids selectivity`` () =
    task {
        use conn = new NpgsqlConnection (connectionStringForTests)
        do conn.Open ()

        let! user1 =
            Users.insertSingleAsync
                { Email = "a@test.com"
                  Name = "Alice"
                  PasswordHash = "1" }
                conn

        let! user2 =
            Users.insertSingleAsync
                { Email = "b@test.com"
                  Name = "Bob"
                  PasswordHash = "2" }
                conn

        let! user3 =
            Users.insertSingleAsync
                { Email = "c@test.com"
                  Name = "Ceca"
                  PasswordHash = "3" }
                conn

        let expectedInTable: Record list =
            [ user1; user2; user3 ]
            |> List.map mapToUtilsRecord

        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let expected: Users.User list = [ user2; user3 ]

        let! actual = Users.getByIdsAsync (Set [ user2.Id; user3.Id ]) conn
        Assert.That (actual, Is.EquivalentTo expected)
    }

[<Test>]
let ``getByIdsAsync - empty ids - no rows returned`` () =
    task {
        use conn = new NpgsqlConnection (connectionStringForTests)
        do conn.Open ()

        let! user1 =
            Users.insertSingleAsync
                { Email = "a@test.com"
                  Name = "Alice"
                  PasswordHash = "1" }
                conn

        let! user2 =
            Users.insertSingleAsync
                { Email = "b@test.com"
                  Name = "Bob"
                  PasswordHash = "2" }
                conn

        let! user3 =
            Users.insertSingleAsync
                { Email = "c@test.com"
                  Name = "Ceca"
                  PasswordHash = "3" }
                conn

        let expectedInTable: Record list =
            [ user1; user2; user3 ]
            |> List.map mapToUtilsRecord

        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let! actual = Users.getByIdsAsync Set.empty conn
        Assert.That (actual, Is.Empty)
    }

[<Test>]
let ``getByIdsAsync - unexistent id - no rows returned`` () =
    task {
        use conn = new NpgsqlConnection (connectionStringForTests)
        do conn.Open ()

        let! user1 =
            Users.insertSingleAsync
                { Email = "a@test.com"
                  Name = "Alice"
                  PasswordHash = "1" }
                conn

        let! user2 =
            Users.insertSingleAsync
                { Email = "b@test.com"
                  Name = "Bob"
                  PasswordHash = "2" }
                conn

        let! user3 =
            Users.insertSingleAsync
                { Email = "c@test.com"
                  Name = "Ceca"
                  PasswordHash = "3" }
                conn

        let expectedInTable: Record list =
            [ user1; user2; user3 ]
            |> List.map mapToUtilsRecord

        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let! actual = Users.getByIdsAsync (Set.singleton 0) conn
        Assert.That (actual, Is.Empty)
    }

[<Test>]
let ``deleteByIdAsync - id selectivity`` () =
    task {
        use conn = new NpgsqlConnection (connectionStringForTests)
        do conn.Open ()

        let! user1 =
            Users.insertSingleAsync
                { Email = "a@test.com"
                  Name = "Alice"
                  PasswordHash = "1" }
                conn

        let! user2 =
            Users.insertSingleAsync
                { Email = "b@test.com"
                  Name = "Bob"
                  PasswordHash = "2" }
                conn

        let expectedInTable: Record list = [ user1; user2 ] |> List.map mapToUtilsRecord
        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let! numRowsAffected = Users.deleteByIdAsync user1.Id conn
        Assert.That (numRowsAffected, Is.EqualTo 1)

        let expectedInTable: Record list = user2 |> mapToUtilsRecord |> List.singleton
        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EqualTo expectedInTable)
    }

[<Test>]
let ``getByEmailAsync - emails selectivity`` () =
    task {
        use conn = new NpgsqlConnection (connectionStringForTests)
        do conn.Open ()

        let! user1 =
            Users.insertSingleAsync
                { Email = "a@test.com"
                  Name = "Alice"
                  PasswordHash = "1" }
                conn

        let! user2 =
            Users.insertSingleAsync
                { Email = "b@test.com"
                  Name = "Bob"
                  PasswordHash = "2" }
                conn

        let expectedInTable: Record list = [ user1; user2 ] |> List.map mapToUtilsRecord
        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let expected: Users.User option = Some user2

        let! actual = Users.getByEmailAsync "b@test.com" conn
        Assert.That (actual, Is.EqualTo expected)
    }
