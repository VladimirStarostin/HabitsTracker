module TrackingService.Tests.DataAccess.HabitEvents

open System

open Microsoft.Data.Sqlite

open NUnit.Framework
open Dapper.FSharp.SQLite

open HabitsTracker.Helpers
open TrackingService.Host.DataAccess.HabitEvents
open TrackingService.Migrations

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
        let! id1 = insertSingleAsync { HabitId = 1; Date = date; UserId = 1 } conn
        let! id2 = insertSingleAsync { HabitId = 2; Date = date; UserId = 2 } conn
        let! id3 = insertSingleAsync { HabitId = 3; Date = date; UserId = 3 } conn

        let expectedInTable: HabitEvent list =
            [ { Id = id1
                HabitId = 1
                Date = date
                UserId = 1 }
              { Id = id2
                HabitId = 2
                Date = date
                UserId = 2 }
              { Id = id3
                HabitId = 3
                Date = date
                UserId = 3 } ]

        let! actualInTable = getAllAsync conn
        Assert.That(actualInTable, Is.EquivalentTo expectedInTable)

        let expected: HabitEvent list =
            [ { Id = id2
                HabitId = 2
                Date = date
                UserId = 2 }
              { Id = id3
                HabitId = 3
                Date = date
                UserId = 3 } ]

        let! actual = getByIdsAsync [ id2; id3 ] conn
        Assert.That(actual, Is.EquivalentTo expected)
    }

[<Test>]
let ``getByIdsAsync - empty ids - no rows returned`` () =
    task {
        use conn = new SqliteConnection(connectionString)
        do conn.Open()
        let! id1 = insertSingleAsync { HabitId = 1; Date = date; UserId = 1 } conn
        let! id2 = insertSingleAsync { HabitId = 2; Date = date; UserId = 2 } conn
        let! id3 = insertSingleAsync { HabitId = 3; Date = date; UserId = 3 } conn

        let expectedInTable: HabitEvent list =
            [ { Id = id1
                HabitId = 1
                Date = date
                UserId = 1 }
              { Id = id2
                HabitId = 2
                Date = date
                UserId = 2 }
              { Id = id3
                HabitId = 3
                Date = date
                UserId = 3 } ]

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
        let! id1 = insertSingleAsync { HabitId = 1; Date = date; UserId = 1 } conn
        let! id2 = insertSingleAsync { HabitId = 2; Date = date; UserId = 2 } conn
        let! id3 = insertSingleAsync { HabitId = 3; Date = date; UserId = 3 } conn

        let expectedInTable: HabitEvent list =
            [ { Id = id1
                HabitId = 1
                Date = date
                UserId = 1 }
              { Id = id2
                HabitId = 2
                Date = date
                UserId = 2 }
              { Id = id3
                HabitId = 3
                Date = date
                UserId = 3 } ]

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
        let! id1 = insertSingleAsync { HabitId = 1; Date = date; UserId = 1 } conn
        let! id2 = insertSingleAsync { HabitId = 2; Date = date; UserId = 2 } conn

        let expected: HabitEvent list =
            [ { Id = id1
                HabitId = 1
                Date = date
                UserId = 1 }
              { Id = id2
                HabitId = 2
                Date = date
                UserId = 2 } ]

        let! actual = getAllAsync conn
        Assert.That(actual, Is.EquivalentTo expected)
    }

[<Test>]
let ``deleteByIdAsync - id selectivity`` () =
    task {
        use conn = new SqliteConnection(connectionString)
        do conn.Open()
        let! id1 = insertSingleAsync { HabitId = 1; Date = date; UserId = 1 } conn
        let! id2 = insertSingleAsync { HabitId = 2; Date = date; UserId = 2 } conn

        let! numRowsAffected = deleteByIdAsync id1 conn
        Assert.That(numRowsAffected, Is.EqualTo 1)

        let expected =
            List.singleton
                { Id = id2
                  HabitId = 2
                  Date = date
                  UserId = 2 }

        let! actual = getAllAsync conn
        Assert.That(actual, Is.EqualTo expected)
    }
