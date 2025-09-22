[<RequireQualifiedAccessAttribute>]
module HabitsTracker.Helpers.Postgres

open System.Data
open System.Threading.Tasks

open Npgsql

let getConnection (connectionString: string) : IDbConnection =
    let conn = new NpgsqlConnection (connectionString)
    conn.Open ()
    conn :> IDbConnection

let executeWithConnection<'a> (connectionString: string) (work: IDbConnection -> Task<'a>) : Task<'a> =
    task {
        use conn = getConnection connectionString
        return! work conn
    }

open System
open Testcontainers.PostgreSql

let private quoteIdent (s: string) = "\"" + s.Replace ("\"", "\"\"") + "\""

type TestPostgresDbContainerHandle =
    { ConnectionString: string
      Container: IAsyncDisposable option }

    interface IAsyncDisposable with
        member this.DisposeAsync () =
            match this.Container with
            | Some c -> c.DisposeAsync ()
            | None -> ValueTask.CompletedTask

let tryCreatePostgresDbAsync
    (port: int)
    (dbName: string)
    (userName: string)
    (password: string)
    : Task<TestPostgresDbContainerHandle> =
    task {
        let adminUser =
            match Environment.GetEnvironmentVariable ("PG_ADMIN_USER") with
            | null
            | "" -> "postgres"
            | u -> u

        let adminPassword =
            match Environment.GetEnvironmentVariable ("PG_ADMIN_PASSWORD") with
            | null
            | "" -> "postgres"
            | p -> p

        let rootConnStr =
            $"Host=127.0.0.1;Port={port};Username={adminUser};Password={adminPassword};Database=postgres"

        let! alreadyRunning =
            task {
                try
                    use c = new NpgsqlConnection (rootConnStr)
                    do! c.OpenAsync ()
                    return true
                with _ ->
                    return false
            }

        let mutable container: PostgreSqlContainer option = None

        if not alreadyRunning then
            let pg =
                PostgreSqlBuilder()
                    .WithImage("postgres:16-alpine")
                    .WithDatabase("postgres")
                    .WithUsername(adminUser)
                    .WithPassword(adminPassword)
                    .WithPortBinding(port, 5432)
                    .Build ()

            do! pg.StartAsync ()
            container <- Some pg

        use adminConn = new NpgsqlConnection (rootConnStr)
        do! adminConn.OpenAsync ()

        do!
            task {
                use check =
                    new NpgsqlCommand ("select 1 from pg_database where datname = @n", adminConn)

                check.Parameters.AddWithValue ("n", dbName)
                |> ignore

                let! ok = check.ExecuteScalarAsync ()

                if isNull ok then
                    use createDb = new NpgsqlCommand ($"create database {quoteIdent dbName}", adminConn)
                    ignore (createDb.ExecuteNonQuery ())
            }

        do!
            task {
                use check =
                    new NpgsqlCommand ("select 1 from pg_roles where rolname = @r", adminConn)

                check.Parameters.AddWithValue ("r", userName)
                |> ignore

                let! ok = check.ExecuteScalarAsync ()

                if isNull ok then
                    use createRole =
                        new NpgsqlCommand ($"create user {quoteIdent userName} with password @pwd", adminConn)

                    createRole.Parameters.AddWithValue ("pwd", password)
                    |> ignore

                    ignore (createRole.ExecuteNonQuery ())
                else
                    use alterPwd =
                        new NpgsqlCommand ($"alter user {quoteIdent userName} with password @pwd", adminConn)

                    alterPwd.Parameters.AddWithValue ("pwd", password)
                    |> ignore

                    ignore (alterPwd.ExecuteNonQuery ())
            }

        let connStr =
            $"Host=127.0.0.1;Port={port};Username={userName};Password={password};Database={dbName}"

        return
            { ConnectionString = connStr
              Container =
                container
                |> Option.map (fun c -> c :> IAsyncDisposable) }
    }
