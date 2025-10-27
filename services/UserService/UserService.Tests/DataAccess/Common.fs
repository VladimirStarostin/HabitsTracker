module UserService.Tests.DataAccess.Utils.Common

open System
open System.Data
open System.Threading.Tasks

open HabitsTracker.Helpers

module RefreshTokens = UserService.Host.DataAccess.RefreshTokens
module Users = UserService.Host.DataAccess.Users

module UsersUtils = UserService.Tests.DataAccess.Utils.Users

let defaultDate = DateTime.Parse "2025-07-28"

let defaultRecord: RefreshTokens.InsertDto =
    { Token = ""
      UserId = 0
      ExpiresAt = defaultDate
      IsRevoked = false
      CreatedAt = defaultDate }

let internal connectionStringForTests =
    "Host=localhost;Port=5432;Database=users_db;Username=habits_tracker_user;Password=1W@nt70m3J0b"

let clearDbAsync (connectionString: string) =
    task {
        do!
            (fun conn ->
                task {
                    let! _ = RefreshTokens.deleteAllAsync conn
                    let! _ = UsersUtils.deleteAllAsync conn
                    return ()
                }
            )
            |> Postgres.executeWithConnection connectionString
    }
    :> Task

let insertRefreshTokenRecordForUser
    (name: string)
    (token: string option)
    (expirationDate: DateTime option)
    (conn: IDbConnection)
    : Task<RefreshTokens.RefreshTokenRecord> =
    task {
        let! user =
            UserService.Host.DataAccess.Users.insertSingleAsync
                { Email = name
                  Name = name
                  PasswordHash = System.Guid.NewGuid().ToString () }
                conn

        let token =
            token
            |> Option.defaultValue (System.Guid.NewGuid().ToString ())

        let recordForUser =
            { defaultRecord with
                Token = token
                ExpiresAt = expirationDate |> Option.defaultValue defaultDate
                UserId = user.Id }

        return! RefreshTokens.insertSingleAsync recordForUser conn
    }
