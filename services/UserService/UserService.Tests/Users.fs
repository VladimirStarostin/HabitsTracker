module UserService.Tests.DataAccess.Users

open System

open Microsoft.Data.Sqlite

open NUnit.Framework
open Dapper.FSharp.SQLite

open HabitsTracker.Helpers
open UserService.Host.DataAccess.Users
open UserService.Migrations

let connectionString = "Data Source=file:memdb1?mode=memory&cache=shared"

[<OneTimeSetUp>]
let Setup () =
    do OptionTypes.register ()
    do MigrationRunner.runMigrations connectionString (typeof<CreateTablesMigration>.Assembly)

[<SetUp>]
let clearDbAsync () =
    task {
        use conn = new SqliteConnection(connectionString)
        let! _ = deleteAllAsync conn
        return ()
    }

[<Test>]
let ``Select from empty Habits returns empty`` () =
    task {
        use conn = new SqliteConnection(connectionString)
        do conn.Open()
        let! result = getAllAsync conn
        Assert.That(result, Is.Empty)
    }

let date = DateTimeOffset.Parse "2025-07-28"

[<Test>]
let ``getByIdsAsync - ids selectivity`` () =
    task {
        use conn = new SqliteConnection(connectionString)
        do conn.Open()
        let! id1 = insertSingleAsync { Email = "a@test.com"; Name = "Alice" } conn
        let! id2 = insertSingleAsync { Email = "b@test.com"; Name = "Bob" } conn
        let! id3 = insertSingleAsync { Email = "c@test.com"; Name = "Ceca" } conn

        let expectedInTable: User list =
            [ { Id = id1
                Email = "a@test.com"
                Name = "Alice" }
              { Id = id2
                Email = "b@test.com"
                Name = "Bob" }
              { Id = id3
                Email = "c@test.com"
                Name = "Ceca" } ]

        let! actualInTable = getAllAsync conn
        Assert.That(actualInTable, Is.EquivalentTo expectedInTable)

        let expected: User list =
            [ { Id = id2
                Email = "b@test.com"
                Name = "Bob" }
              { Id = id3
                Email = "c@test.com"
                Name = "Ceca" } ]

        let! actual = getByIdsAsync [ id2; id3 ] conn
        Assert.That(actual, Is.EquivalentTo expected)
    }

[<Test>]
let ``getByIdsAsync - empty ids - no rows returned`` () =
    task {
        use conn = new SqliteConnection(connectionString)
        do conn.Open()
        let! id1 = insertSingleAsync { Email = "a@test.com"; Name = "Alice" } conn
        let! id2 = insertSingleAsync { Email = "b@test.com"; Name = "Bob" } conn
        let! id3 = insertSingleAsync { Email = "c@test.com"; Name = "Ceca" } conn

        let expectedInTable: User list =
            [ { Id = id1
                Email = "a@test.com"
                Name = "Alice" }
              { Id = id2
                Email = "b@test.com"
                Name = "Bob" }
              { Id = id3
                Email = "c@test.com"
                Name = "Ceca" } ]

        let! actualInTable = getAllAsync conn
        Assert.That(actualInTable, Is.EquivalentTo expectedInTable)

        let! actual = getByIdsAsync [] conn
        Assert.That(actual, Is.Empty)
    }

[<Test>]
let ``getByIdsAsync - unexistent id - no rows returned`` () =
    task {
        use conn = new SqliteConnection(connectionString)
        do conn.Open()
        let! id1 = insertSingleAsync { Email = "a@test.com"; Name = "Alice" } conn
        let! id2 = insertSingleAsync { Email = "b@test.com"; Name = "Bob" } conn
        let! id3 = insertSingleAsync { Email = "c@test.com"; Name = "Ceca" } conn

        let expectedInTable: User list =
            [ { Id = id1
                Email = "a@test.com"
                Name = "Alice" }
              { Id = id2
                Email = "b@test.com"
                Name = "Bob" }
              { Id = id3
                Email = "c@test.com"
                Name = "Ceca" } ]

        let! actualInTable = getAllAsync conn
        Assert.That(actualInTable, Is.EquivalentTo expectedInTable)

        let! actual = getByIdsAsync (List.singleton 0) conn
        Assert.That(actual, Is.Empty)
    }

[<Test>]
let ``getAllAsync - insert two then select all`` () =
    task {
        use conn = new SqliteConnection(connectionString)
        do conn.Open()
        let! id1 = insertSingleAsync { Email = "a@test.com"; Name = "Alice" } conn
        let! id2 = insertSingleAsync { Email = "b@test.com"; Name = "Bob" } conn

        let expected: User list =
            [ { Id = id1
                Email = "a@test.com"
                Name = "Alice" }
              { Id = id2
                Email = "b@test.com"
                Name = "Bob" } ]

        let! actual = getAllAsync conn
        Assert.That(actual, Is.EquivalentTo expected)
    }

[<Test>]
let ``deleteByIdAsync - id selectivity`` () =
    task {
        use conn = new SqliteConnection(connectionString)
        do conn.Open()
        let! id1 = insertSingleAsync { Email = "a@test.com"; Name = "Alice" } conn
        let! id2 = insertSingleAsync { Email = "b@test.com"; Name = "Bob" } conn

        let! numRowsAffected = deleteByIdAsync id1 conn
        Assert.That(numRowsAffected, Is.EqualTo 1)

        let expected =
            List.singleton
                { Id = id2
                  Email = "b@test.com"
                  Name = "Bob" }

        let! actual = getAllAsync conn
        Assert.That(actual, Is.EquivalentTo expected)
    }
