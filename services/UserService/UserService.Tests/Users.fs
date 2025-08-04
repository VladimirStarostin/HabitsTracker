module UserService.Tests.DataAccess.Users

open System

open Npgsql

open Dapper.FSharp.SQLite
open NUnit.Framework

open HabitsTracker.Helpers
open UserService.Host.DataAccess.Users
open UserService.Migrations

open UserService.Tests.DataAccess.Utils
open UserService.Tests.DataAccess.Utils.Users

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
let ``Select from empty Habits returns empty`` () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        do conn.Open ()
        let! result = getAllAsync conn
        Assert.That (result, Is.Empty)
    }

let date = DateTimeOffset.Parse "2025-07-28"

[<Test>]
let ``getByIdsAsync - ids selectivity`` () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        do conn.Open ()

        let! user1 =
            insertSingleAsync
                { Email = "a@test.com"
                  Name = "Alice"
                  PasswordHash = "1" }
                conn

        let! user2 =
            insertSingleAsync
                { Email = "b@test.com"
                  Name = "Bob"
                  PasswordHash = "2" }
                conn

        let! user3 =
            insertSingleAsync
                { Email = "c@test.com"
                  Name = "Ceca"
                  PasswordHash = "3" }
                conn

        let expectedInTable: Record list =
            [ user1; user2; user3 ]
            |> List.map mapToUtilsRecord

        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let expected: User list = [ user2; user3 ]

        let! actual = getByIdsAsync (Set [ user2.Id; user3.Id ]) conn
        Assert.That (actual, Is.EquivalentTo expected)
    }

[<Test>]
let ``getByIdsAsync - empty ids - no rows returned`` () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        do conn.Open ()

        let! user1 =
            insertSingleAsync
                { Email = "a@test.com"
                  Name = "Alice"
                  PasswordHash = "1" }
                conn

        let! user2 =
            insertSingleAsync
                { Email = "b@test.com"
                  Name = "Bob"
                  PasswordHash = "2" }
                conn

        let! user3 =
            insertSingleAsync
                { Email = "c@test.com"
                  Name = "Ceca"
                  PasswordHash = "3" }
                conn

        let expectedInTable: Record list =
            [ user1; user2; user3 ]
            |> List.map mapToUtilsRecord

        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let! actual = getByIdsAsync Set.empty conn
        Assert.That (actual, Is.Empty)
    }

[<Test>]
let ``getByIdsAsync - unexistent id - no rows returned`` () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        do conn.Open ()

        let! user1 =
            insertSingleAsync
                { Email = "a@test.com"
                  Name = "Alice"
                  PasswordHash = "1" }
                conn

        let! user2 =
            insertSingleAsync
                { Email = "b@test.com"
                  Name = "Bob"
                  PasswordHash = "2" }
                conn

        let! user3 =
            insertSingleAsync
                { Email = "c@test.com"
                  Name = "Ceca"
                  PasswordHash = "3" }
                conn

        let expectedInTable: Record list =
            [ user1; user2; user3 ]
            |> List.map mapToUtilsRecord

        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let! actual = getByIdsAsync (Set.singleton 0) conn
        Assert.That (actual, Is.Empty)
    }

[<Test>]
let ``deleteByIdAsync - id selectivity`` () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        do conn.Open ()

        let! user1 =
            insertSingleAsync
                { Email = "a@test.com"
                  Name = "Alice"
                  PasswordHash = "1" }
                conn

        let! user2 =
            insertSingleAsync
                { Email = "b@test.com"
                  Name = "Bob"
                  PasswordHash = "2" }
                conn

        let expectedInTable: Record list = [ user1; user2 ] |> List.map mapToUtilsRecord
        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let! numRowsAffected = deleteByIdAsync user1.Id conn
        Assert.That (numRowsAffected, Is.EqualTo 1)

        let expectedInTable: Record list = user2 |> mapToUtilsRecord |> List.singleton
        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EqualTo expectedInTable)
    }

[<Test>]
let ``getByEmailAsync - emails selectivity`` () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        do conn.Open ()

        let! user1 =
            insertSingleAsync
                { Email = "a@test.com"
                  Name = "Alice"
                  PasswordHash = "1" }
                conn

        let! user2 =
            insertSingleAsync
                { Email = "b@test.com"
                  Name = "Bob"
                  PasswordHash = "2" }
                conn

        let expectedInTable: Record list = [ user1; user2 ] |> List.map mapToUtilsRecord
        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let expected: User option =
            Some
                user2

        let! actual = getByEmailAsync "b@test.com" conn
        Assert.That (actual, Is.EqualTo expected)
    }
