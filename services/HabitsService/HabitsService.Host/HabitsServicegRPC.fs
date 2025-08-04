namespace HabitsService.Host.Grpc

open System

open HabitsService.V1
open HabitsTracker.Helpers

open HabitsService.Host.DataAccess.Habits

type HabitsServiceImpl (connectionString: string) =
    inherit HabitsService.HabitsServiceBase ()

    override _.GetByIds (request: GetByIdsRequest, _) =
        task {
            let! habits =
                getByIdsAsync (request.HabitIds |> Set.ofSeq)
                |> Postgres.executeWithConnection connectionString

            let response = GetByIdsResponse ()

            for h in habits do
                response.Habits.Add (Habit (Id = h.Id, Name = h.Name))

            return response
        }

    override _.GetByUserIds (request: GetByUserIdsRequest, _) =
        task {
            let! habits =
                getByUserIdsAsync (request.UserIds |> Set.ofSeq)
                |> Postgres.executeWithConnection connectionString

            let response = GetByUserIdsResponse ()

            for h in habits do
                response.Habits.Add (Habit (Id = h.Id, Name = h.Name))

            return response
        }

    override _.Create (request: CreateHabitRequest, _) =
        task {
            let! habit =
                insertSingleAsync
                    { OwnerUserId = request.OwnerUserId
                      Name = request.Name
                      Description =
                        if System.String.IsNullOrWhiteSpace request.Description then
                            None
                        else
                            Some request.Description }
                |> Postgres.executeWithConnection connectionString

            let description =
                habit.Description
                |> Option.defaultValue String.Empty

            return
                CreateHabitResponse (
                    Habit =
                        HabitsService.V1.Habit (
                            Id = habit.Id,
                            Name = habit.Name,
                            Description = description,
                            OwnerUserId = habit.OwnerUserId
                        )
                )
        }

    override _.Delete (request: DeleteHabitRequest, _) =
        task {
            let! deletedRowsCount =
                deleteByIdAsync request.Id
                |> Postgres.executeWithConnection connectionString

            return DeleteHabitResponse (DeletedRowsCount = deletedRowsCount)
        }
