module TrackingService.Tests.DataAccess.HabitEvents

open System

open Npgsql

open Dapper.FSharp.PostgreSQL
open NUnit.Framework

open HabitsTracker.Helpers
open TrackingService.Host.DataAccess.HabitEvents
open TrackingService.Migrations

open TrackingService.Tests.DataAccess.Utils.HabitEvents

let connectionString =
    "Host=localhost;Port=5432;Database=tracking_db;Username=habits_tracker_user;Password=1W@nt70m3J0b"

[<OneTimeSetUp>]
let Setup () =
    do OptionTypes.register ()
    do MigrationRunner.runMigrations connectionString (typeof<CreateHabitEventsTableMigration>.Assembly)
    do Dapper.addRequiredTypeHandlers ()

[<SetUp>]
let clearDbAsync () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        let! _ = deleteAllAsync conn
        return ()
    }

let date = (DateTimeOffset.Parse "2025-07-28").ToUniversalTime ()

[<Test>]
let ``getByHabitIdsAsync - habit ids selectivity`` () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        do conn.Open ()

        let habitId1 = 1
        let habitId2 = 2

        let today = DateTimeOffset.UtcNow
        let yesterday = today - TimeSpan.FromDays 1

        let insertParams1 =
            { HabitId = habitId1
              Date = yesterday.ToUniversalTime ()
              UserId = 1 }

        let insertParams2 =
            { HabitId = habitId1
              Date = yesterday.ToUniversalTime ()
              UserId = 1 }

        let insertParams3 =
            { HabitId = habitId2
              Date = date
              UserId = 3 }

        let! event1 = insertSingleAsync insertParams1 conn
        let! event2 = insertSingleAsync insertParams2 conn
        let! event3 = insertSingleAsync insertParams3 conn

        let expectedInTable: Record list =
            [ event1; event2; event3 ]
            |> List.map mapToUtilsRecord

        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let expected: HabitEvent list = [ event1; event2 ]
        let! actual = getByHabitIdsAsync (Set [ event2.HabitId ]) conn
        Assert.That (actual, Is.EquivalentTo expected)
    }

[<Test>]
let ``getByIdsAsync - ids selectivity`` () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        do conn.Open ()

        let insertParams1 =
            { HabitId = 1
              Date = date
              UserId = 1 }

        let insertParams2 =
            { HabitId = 1
              Date = date
              UserId = 2 }

        let insertParams3 =
            { HabitId = 2
              Date = date
              UserId = 3 }

        let! event1 = insertSingleAsync insertParams1 conn
        let! event2 = insertSingleAsync insertParams2 conn
        let! event3 = insertSingleAsync insertParams3 conn

        let expectedInTable: Record list =
            [ event1; event2; event3 ]
            |> List.map mapToUtilsRecord

        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let expected: HabitEvent list = [ event2; event3 ]
        let! actual = getByIdsAsync (Set [ event2.Id; event3.Id ]) conn
        Assert.That (actual, Is.EquivalentTo expected)
    }

[<Test>]
let ``getByIdsAsync - empty ids - no rows returned`` () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        do conn.Open ()

        let insertParams1 =
            { HabitId = 1
              Date = date
              UserId = 1 }

        let insertParams2 =
            { HabitId = 1
              Date = date
              UserId = 2 }

        let insertParams3 =
            { HabitId = 2
              Date = date
              UserId = 3 }

        let! event1 = insertSingleAsync insertParams1 conn
        let! event2 = insertSingleAsync insertParams2 conn
        let! event3 = insertSingleAsync insertParams3 conn

        let expectedInTable: Record list =
            [ event1; event2; event3 ]
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

        let insertParams1 =
            { HabitId = 1
              Date = date
              UserId = 1 }

        let insertParams2 =
            { HabitId = 1
              Date = date
              UserId = 2 }

        let insertParams3 =
            { HabitId = 2
              Date = date
              UserId = 3 }

        let! event1 = insertSingleAsync insertParams1 conn
        let! event2 = insertSingleAsync insertParams2 conn
        let! event3 = insertSingleAsync insertParams3 conn

        let expectedInTable: Record list =
            [ event1; event2; event3 ]
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

        let insertParams1 =
            { HabitId = 1
              Date = date
              UserId = 1 }

        let insertParams2 =
            { HabitId = 1
              Date = date
              UserId = 2 }

        let! event1 = insertSingleAsync insertParams1 conn
        let! event2 = insertSingleAsync insertParams2 conn

        let expectedInTable: Record list = [ event1; event2 ] |> List.map mapToUtilsRecord
        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let! numRowsAffected = deleteByIdAsync event1.Id conn
        Assert.That (numRowsAffected, Is.EqualTo 1)

        let expected = event2 |> mapToUtilsRecord |> List.singleton

        let! actual = getAllAsync conn
        Assert.That (actual, Is.EqualTo expected)
    }
