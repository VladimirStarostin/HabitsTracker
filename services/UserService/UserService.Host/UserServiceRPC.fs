namespace UserService.Host.Grpc

open System

open Grpc.Core

open HabitsTracker.Helpers
open UserService.V1

open UserService.Host.DataAccess.Users

type UserServiceImpl(connectionString: string) =
    inherit UserService.UserServiceBase()

    override _.Register(request: RegisterRequest, _) : Threading.Tasks.Task<RegisterResponse> =
        task {
            let! registeredUserId =
                insertSingleAsync
                    { Email = request.Email
                      Name = request.Name }
                |> SQLite.executeWithConnection connectionString

            return RegisterResponse(User = User(Id = registeredUserId, Email = request.Email, Name = request.Name))
        }

    override _.GetCurrent(_, context: ServerCallContext) =
        task {
            let userId =
                context.RequestHeaders
                |> HeadersParser.getUserId
                |> List.singleton

            let! currentUser =
                getByIdsAsync userId
                |> SQLite.executeWithConnection connectionString
                |> fun t ->
                    task {
                        let! users = t
                        return users |> Seq.exactlyOne
                    }

            return
                GetCurrentResponse(User = User(Id = currentUser.Id, Email = currentUser.Email, Name = currentUser.Name))
        }

    override _.DeleteAccount(_, context: ServerCallContext) =
        task {
            let userId = context.RequestHeaders |> HeadersParser.getUserId

            let! _ = deleteByIdAsync userId |> SQLite.executeWithConnection connectionString

            return DeleteAccountResponse()
        }
