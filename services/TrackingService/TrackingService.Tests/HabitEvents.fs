module TrackingService.Tests.DataAccess.HabitEvents

open System

open Npgsql

open NUnit.Framework

open HabitsTracker.Helpers
open TrackingService.Host.DataAccess.Habits
open TrackingService.Migrations

open TrackingService.Tests.DataAccess.Utils.Common
open TrackingService.Tests.DataAccess.Utils.HabitEventsUtils

[<OneTimeSetUp>]
let Setup () =
    do Dapper.addRequiredTypeHandlers ()
    do MigrationRunner.runMigrations connectionStringForTests (typeof<CreateHabitEventsTableMigration>.Assembly)

[<SetUp>]
let setup () = clearDbAsync ()

let date = (DateTimeOffset.Parse "2025-07-28").ToUniversalTime ()

[<Test>]
let ``getByHabitIdsAsync - habit ids selectivity`` () =
    task {
        use conn = new NpgsqlConnection (connectionStringForTests)
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
        use conn = new NpgsqlConnection (connectionStringForTests)
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
        use conn = new NpgsqlConnection (connectionStringForTests)
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
        use conn = new NpgsqlConnection (connectionStringForTests)
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
let ``deleteByIdAsync - id & userId selectivity`` () =
    task {
        use conn = new NpgsqlConnection (connectionStringForTests)
        do conn.Open ()

        let userId1 = 1
        let userId2 = 2

        let insertParams1 =
            { HabitId = 1
              Date = date
              UserId = userId1 }

        let insertParams2 =
            { HabitId = 2
              Date = date
              UserId = userId1 }

        let insertParams3 =
            { HabitId = 3
              Date = date
              UserId = userId2 }

        let insertParams4 =
            { HabitId = 4
              Date = date
              UserId = userId2 }

        let! event1 = insertSingleAsync insertParams1 conn
        let! event2 = insertSingleAsync insertParams2 conn
        let! event3 = insertSingleAsync insertParams3 conn
        let! event4 = insertSingleAsync insertParams4 conn

        let expectedInTable: Record list =
            [ event1; event2; event3; event4 ]
            |> List.map mapToUtilsRecord

        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let! numRowsAffected = deleteByIdsAsync [ event1.Id; event2.Id; event4.Id ] userId1 conn

        Assert.That (numRowsAffected, Is.EqualTo 2)

        let expected = [ event3; event4 ] |> List.map mapToUtilsRecord

        let! actual = getAllAsync conn
        Assert.That (actual, Is.EqualTo expected)
    }
