module HabitsService.Tests.DataAccess.Habits

open Microsoft.Data.Sqlite

open NUnit.Framework

open Dapper.FSharp.SQLite

open HabitsTracker.Helpers

open HabitsService.Host.DataAccess.Habits
open HabitsService.Migrations

let connectionString = "Data Source=file:memdb1?mode=memory&cache=shared"

[<OneTimeSetUp>]
let Setup () =
    do OptionTypes.register ()
    do MigrationRunner.runMigrations connectionString (typeof<CreateTablesMigration>.Assembly)

[<SetUp>]
let clearDbAsync () =
    task {
        use conn = new SqliteConnection (connectionString)
        let! _ = deleteAllAsync conn
        return ()
    }

[<Test>]
let ``Select from empty Habits returns empty`` () =
    task {
        use conn = new SqliteConnection (connectionString)
        do conn.Open ()
        let! result = getAllAsync conn
        Assert.That (result, Is.Empty)
    }

[<Test>]
let ``getByIdsAsync - ids selectivity`` () =
    task {
        use conn = new SqliteConnection (connectionString)
        do conn.Open ()
        let! id1 = insertSingleAsync { Name = "Read books" } conn
        let! id2 = insertSingleAsync { Name = "Exercise" } conn
        let! id3 = insertSingleAsync { Name = "Run" } conn

        let expectedInTable =
            [ { Id = id1
                Name = "Read books" }
              { Id = id2
                Name = "Exercise" }
              { Id = id3
                Name = "Run" } ]

        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let expected =
            [ { Id = id2
                Name = "Exercise" }
              { Id = id3
                Name = "Run" } ]

        let! actual = getByIdsAsync [ id2; id3 ] conn
        Assert.That (actual, Is.EquivalentTo expected)
    }

[<Test>]
let ``getByIdsAsync - empty ids - no rows returned`` () =
    task {
        use conn = new SqliteConnection (connectionString)
        do conn.Open ()
        let! id1 = insertSingleAsync { Name = "Read books" } conn
        let! id2 = insertSingleAsync { Name = "Exercise" } conn
        let! id3 = insertSingleAsync { Name = "Run" } conn

        let expectedInTable =
            [ { Id = id1
                Name = "Read books" }
              { Id = id2
                Name = "Exercise" }
              { Id = id3
                Name = "Run" } ]

        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let! actual = getByIdsAsync [] conn
        Assert.That (actual, Is.Empty)
    }

[<Test>]
let ``getByIdsAsync - unexistent id - no rows returned`` () =
    task {
        use conn = new SqliteConnection (connectionString)
        do conn.Open ()
        let! id1 = insertSingleAsync { Name = "Read books" } conn
        let! id2 = insertSingleAsync { Name = "Exercise" } conn
        let! id3 = insertSingleAsync { Name = "Run" } conn

        let expectedInTable =
            [ { Id = id1
                Name = "Read books" }
              { Id = id2
                Name = "Exercise" }
              { Id = id3
                Name = "Run" } ]

        let! actualInTable = getAllAsync conn
        Assert.That (actualInTable, Is.EquivalentTo expectedInTable)

        let! actual = getByIdsAsync (List.singleton 0) conn
        Assert.That (actual, Is.Empty)
    }

[<Test>]
let ``getAllAsync - insert two then select all`` () =
    task {
        use conn = new SqliteConnection (connectionString)
        do conn.Open ()
        let! id1 = insertSingleAsync { Name = "Read books" } conn
        let! id2 = insertSingleAsync { Name = "Exercise" } conn

        let expected =
            [ { Id = id1
                Name = "Read books" }
              { Id = id2
                Name = "Exercise" } ]

        let! actual = getAllAsync conn
        Assert.That (actual, Is.EquivalentTo expected)
    }

[<Test>]
let ``deleteByIdAsync - id selectivity`` () =
    task {
        use conn = new SqliteConnection (connectionString)
        do conn.Open ()
        let! id1 = insertSingleAsync { Name = "Read books" } conn
        let! id2 = insertSingleAsync { Name = "Exercise" } conn

        let! numRowsAffected = deleteByIdAsync id1 conn
        Assert.That (numRowsAffected, Is.EqualTo 1)

        let expected =
            List.singleton
                { Id = id2
                  Name = "Exercise" }

        let! actual = getAllAsync conn
        Assert.That (actual, Is.EquivalentTo expected)
    }
