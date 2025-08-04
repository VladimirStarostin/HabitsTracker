namespace UserService.Host.Grpc

open System
open System.Threading.Tasks

open Grpc.Core

open HabitsTracker.Helpers
open UserService.V1

open UserService.Host.DataAccess.Users
open UserService.Host.Domain.Common.Types
open UserService.Host.Domain.Operations.Authentication
open UserService.Host.Domain.Operations.Registration

type UserServiceImpl (connectionString: string, jwtSettings: JwtSettings) =
    inherit UserService.UserServiceBase ()

    override _.Register (request: RegisterRequest, _) : Task<UserService.V1.AuthResponse> =
        task {
            match!
                registerAsync
                    { Email = request.Email
                      Name = request.Name
                      Password = request.Password }
                    jwtSettings
                |> Postgres.executeWithConnection connectionString
            with
            | RegistrationPipelineResult.Ok authResponse ->
                return
                    UserService.V1.AuthResponse (
                        AccessToken = authResponse.AccessToken,
                        RefreshToken = authResponse.RefreshToken
                    )
            | RegistrationPipelineResult.Error err -> return failwith (err.ToErrMessage ())
        }

    override _.GetByIds (request: GetByIdsRequest, _) : Task<GetByIdsResponse> =
        task {
            let! users =
                getByIdsAsync (request.UserIds |> Set.ofSeq)
                |> Postgres.executeWithConnection connectionString

            let response = GetByIdsResponse ()

            users
            |> Seq.iter (fun u -> response.Users.Add (User (Email = u.Email, Name = u.Name, Id = u.Id)))

            return response
        }

    override _.DeleteAccount (_, context: ServerCallContext) : Task<DeleteAccountResponse> =
        task {
            let userId = context.RequestHeaders |> Grpc.getUserId

            let! numRowsDeleted =
                deleteByIdAsync userId
                |> Postgres.executeWithConnection connectionString

            return DeleteAccountResponse (NumRowsDeleted = numRowsDeleted)
        }

    override _.Refresh (request, _callContext) : Task<UserService.V1.AuthResponse> =
        task {
            match!
                validateAndRefreshAsync request.RefreshToken jwtSettings
                |> Postgres.executeWithConnection connectionString
            with
            | AuthenticationPipelineResult.Ok authResponse ->
                return
                    UserService.V1.AuthResponse (
                        AccessToken = authResponse.AccessToken,
                        RefreshToken = authResponse.RefreshToken
                    )
            | AuthenticationPipelineResult.Error err -> return failwith (err.ToErrMessage ())
        }
