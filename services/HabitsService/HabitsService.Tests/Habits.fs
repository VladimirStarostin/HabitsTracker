module HabitsService.Tests.DataAccess.Habits

open Npgsql

open Dapper.FSharp.PostgreSQL
open NUnit.Framework

open HabitsTracker.Helpers

open HabitsService.Host.DataAccess.Habits
open HabitsService.Host.DataAccess.Tests.Utils.Habits
open HabitsService.Migrations

let connectionString =
    "Host=localhost;Port=5432;Database=habits_db;Username=habits_tracker_user;Password=1W@nt70m3J0b"

[<OneTimeSetUp>]
let Setup () =
    do OptionTypes.register ()
    do MigrationRunner.runMigrations connectionString (typeof<CreateTablesMigration>.Assembly)

[<SetUp>]
let clearDbAsync () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        let! _ = deleteAllAsync conn
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

[<Test>]
let ``getByUserIdsAsync - ids selectivity`` () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        do conn.Open ()

        let! habit1 =
            insertSingleAsync
                { Name = "Read books"
                  Description = None
                  OwnerUserId = 1 }
                conn

        let! habit2 =
            insertSingleAsync
                { Name = "Exercise"
                  Description = None
                  OwnerUserId = 2 }
                conn

        let! habit3 =
            insertSingleAsync
                { Name = "Run"
                  Description = None
                  OwnerUserId = 3 }
                conn

        let expectedInTable: Record list =
            [ habit1; habit2; habit3 ]
            |> List.map mapToUtilsRecord

        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let expected: Habit list = [ habit2; habit3 ]

        let! actual =
            getByUserIdsAsync
                (Set
                    [ habit2.OwnerUserId
                      habit3.OwnerUserId ])
                conn

        Assert.That (actual, Is.EquivalentTo expected)
    }

[<Test>]
let ``getByIdsAsync - ids selectivity`` () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        do conn.Open ()

        let! habit1 =
            insertSingleAsync
                { Name = "Read books"
                  Description = None
                  OwnerUserId = 1 }
                conn

        let! habit2 =
            insertSingleAsync
                { Name = "Exercise"
                  Description = None
                  OwnerUserId = 1 }
                conn

        let! habit3 =
            insertSingleAsync
                { Name = "Run"
                  Description = None
                  OwnerUserId = 1 }
                conn

        let expectedInTable: Record list =
            [ habit1; habit2; habit3 ]
            |> List.map mapToUtilsRecord

        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let expected: Habit list = [ habit2; habit3 ]

        let! actual = getByIdsAsync (Set [ habit2.Id; habit3.Id ]) conn
        Assert.That (actual, Is.EquivalentTo expected)
    }

[<Test>]
let ``getByIdsAsync - empty ids - no rows returned`` () =
    task {
        use conn = new NpgsqlConnection (connectionString)
        do conn.Open ()

        let! habit1 =
            insertSingleAsync
                { Name = "Read books"
                  Description = None
                  OwnerUserId = 1 }
                conn

        let! habit2 =
            insertSingleAsync
                { Name = "Exercise"
                  Description = None
                  OwnerUserId = 1 }
                conn

        let! habit3 =
            insertSingleAsync
                { Name = "Run"
                  Description = None
                  OwnerUserId = 1 }
                conn

        let expectedInTable: Record list =
            [ habit1; habit2; habit3 ]
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

        let! habit1 =
            insertSingleAsync
                { Name = "Read books"
                  Description = None
                  OwnerUserId = 1 }
                conn

        let! habit2 =
            insertSingleAsync
                { Name = "Exercise"
                  Description = None
                  OwnerUserId = 1 }
                conn

        let! habit3 =
            insertSingleAsync
                { Name = "Run"
                  Description = None
                  OwnerUserId = 1 }
                conn

        let expectedInTable: Record list =
            [ habit1; habit2; habit3 ]
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

        let! habit1 =
            insertSingleAsync
                { Name = "Read books"
                  Description = None
                  OwnerUserId = 1 }
                conn

        let! habit2 =
            insertSingleAsync
                { Name = "Exercise"
                  Description = None
                  OwnerUserId = 1 }
                conn

        let expectedInTable: Record list = [ habit1; habit2 ] |> List.map mapToUtilsRecord

        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let! numRowsAffected = deleteByIdAsync habit1.Id conn
        Assert.That (numRowsAffected, Is.EqualTo 1)

        let expected = habit2 |> mapToUtilsRecord |> List.singleton

        let! actual = getAllAsync conn
        Assert.That (actual, Is.EquivalentTo expected)
    }
