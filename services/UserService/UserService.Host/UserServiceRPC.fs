namespace UserService.Host.Grpc

open System
open System.Threading.Tasks

open Grpc.Core

open HabitsTracker.Helpers
open UserService.V1

open UserService.Host.DataAccess.Users
open UserService.Host.Domain.Services.Authentication

type UserServiceImpl (connectionString: string, jwtSettings: JwtSettings) =
    inherit UserService.UserServiceBase ()

    override _.Register (request: RegisterRequest, _) : Task<UserService.V1.AuthResponse> =
        task {
            let! registeredUserId =
                insertSingleAsync
                    { Email = request.Email
                      Name = request.Name
                      Hash = Guid.NewGuid().ToString () }
                |> SQLite.executeWithConnection connectionString

            return AuthResponse (User = User (Id = registeredUserId, Email = request.Email, Name = request.Name))
        }

    override _.GetCurrent (request: GetCurrentRequest, _) =
        task {
            let! currentUser =
                getByIdsAsync request.
                |> SQLite.executeWithConnection connectionString
                |> fun t ->
                    task {
                        let! users = t
                        return users |> Seq.exactlyOne
                    }

            return
                GetCurrentResponse (
                    User = User (Id = currentUser.Id, Email = currentUser.Email, Name = currentUser.Name)
                )
        }

    override _.DeleteAccount (_, context: ServerCallContext) =
        task {
            let userId = context.RequestHeaders |> HeadersParser.getUserId

            let! _ =
                deleteByIdAsync userId
                |> SQLite.executeWithConnection connectionString

            return DeleteAccountResponse ()
        }

    override _.Refresh (request, _callContext) : Task<UserService.V1.AuthResponse> =
        task {
            validateAndRefresh request.RefreshToken connectionString jwtSettings |> 
        }
