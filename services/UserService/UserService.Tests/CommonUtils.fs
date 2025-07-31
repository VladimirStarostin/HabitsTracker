module UserService.Tests.CommonUtils

open System
open System.Data
open System.Threading.Tasks

module RefreshTokens = UserService.Host.DataAccess.RefreshTokens
module Users = UserService.Host.DataAccess.Users

let defaultDate = DateTime.Parse "2025-07-28"

let defaultRecord: RefreshTokens.InsertDto =
    { Token = ""
      UserId = 0
      ExpiresAt = defaultDate
      IsRevoked = false
      CreatedAt = defaultDate }

let insertRefreshTokenRecordForUser
    (name: string)
    (token: string option)
    (expirationDate: DateTime option)
    (conn: IDbConnection)
    : Task<RefreshTokens.RefreshTokenRecord> =
    task {
        let! userId =
            UserService.Host.DataAccess.Users.insertSingleAsync
                { Email = name
                  Name = name
                  Hash = System.Guid.NewGuid().ToString () }
                conn

        let token =
            token
            |> Option.defaultValue (System.Guid.NewGuid().ToString ())

        let recordForUser =
            { defaultRecord with
                Token = token
                ExpiresAt = expirationDate |> Option.defaultValue defaultDate
                UserId = userId }

        let! insertedRecordId = RefreshTokens.insertSingleAsync recordForUser conn

        return
            ({ Id = insertedRecordId
               Token = token
               UserId = recordForUser.UserId
               ExpiresAt = recordForUser.ExpiresAt
               IsRevoked = false
               CreatedAt = defaultDate }
            : RefreshTokens.RefreshTokenRecord)
    }
