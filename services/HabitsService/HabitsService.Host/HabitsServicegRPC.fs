namespace HabitsService.Host.Grpc

open HabitsService.V1
open HabitsTracker.Helpers

open HabitsService.Host.DataAccess.Habits

type HabitsServiceImpl(connectionString: string) =
    inherit HabitsService.HabitsServiceBase()

    override _.GetAll(_, _) =
        task {
            let! habits = getAllAsync |> SQLite.executeWithConnection connectionString
            let response = GetAllResponse()

            for h in habits do
                response.Habits.Add(Habit(Id = h.Id, Name = h.Name))

            return response
        }

    override _.GetByIds(request: GetByIdsRequest, _) =
        task {
            let! habits =
                getByIdsAsync (request.HabitIds |> Seq.toList)
                |> SQLite.executeWithConnection connectionString

            let response = GetByIdsResponse()

            for h in habits do
                response.Habits.Add(Habit(Id = h.Id, Name = h.Name))

            return response
        }

    override _.Add(request: AddHabitRequest, _) =
        task {
            let! id =
                insertSingleAsync { Name = request.Name }
                |> SQLite.executeWithConnection connectionString

            let! result =
                getByIdsAsync (List.singleton id)
                |> SQLite.executeWithConnection connectionString

            let habit = result |> Seq.exactlyOne
            return AddHabitResponse(Habit = Habit(Id = habit.Id, Name = habit.Name))
        }

    override _.Delete(request: DeleteHabitRequest, _) =
        task {
            let! _ =
                deleteByIdAsync request.Id
                |> SQLite.executeWithConnection connectionString

            return DeleteHabitResponse()
        }