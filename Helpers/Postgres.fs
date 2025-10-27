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

type ContainerHandle =
    { RootConnectionString: string
      Port: int
      Container: IAsyncDisposable }

let tryCreatePostgresDbContainerAsync (port: int) : Task<ContainerHandle> =
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

        let pg =
            PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("postgres")
                .WithUsername(adminUser)
                .WithPassword(adminPassword)
                .WithPortBinding(port, 5432)
                .Build ()

        do! pg.StartAsync ()

        return
            { Container = pg
              RootConnectionString = rootConnStr
              Port = port }
    }

let tryCreateDbAsync
    (dbName: string)
    (userName: string)
    (password: string)
    (conainerHandle: ContainerHandle)
    : Task<string> =
    task {
        use adminConn = new NpgsqlConnection (conainerHandle.RootConnectionString)
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

        let correctPassword = password.Replace ("'", "''")

        do!
            task {
                use check =
                    new NpgsqlCommand ("select 1 from pg_roles where rolname = @r", adminConn)

                check.Parameters.AddWithValue ("r", userName)
                |> ignore

                let! ok = check.ExecuteScalarAsync ()

                if isNull ok then
                    use createRole =
                        new NpgsqlCommand (
                            $"create user {quoteIdent userName} with password '{correctPassword}'",
                            adminConn
                        )

                    createRole.ExecuteNonQuery () |> ignore
                else
                    use alterPwd =
                        new NpgsqlCommand (
                            $"alter user {quoteIdent userName} with password '{correctPassword}'",
                            adminConn
                        )

                    alterPwd.ExecuteNonQuery () |> ignore
            }

        return $"Host=127.0.0.1;Port={conainerHandle.Port};Username={userName};Password={password};Database={dbName}"
    }
