namespace HabitsTracker.Gateway.Host.Infrastructure.Clients

open System
open System.Threading.Tasks

open HabitsService.V1

type HabitDto =
    { Id: int
      Name: string
      OwnerUserId: int
      Description: string option }

type CreateHabitDto =
    { Name: string
      OwnerUserId: int
      Description: string option }

type IHabitsClient =
    abstract GetByUserIdsAsync: int list -> Task<HabitDto list>
    abstract GetByIdsAsync: int list -> Task<HabitDto list>
    abstract CreateAsync: CreateHabitDto -> Task<HabitDto>
    abstract DeleteAsync: int -> Task<int>

type HabitsClient (grpcClient: HabitsService.HabitsServiceClient) =
    let toHabitDto (habit: HabitsService.V1.Habit) : HabitDto =
        { Id = habit.Id
          Name = habit.Name
          OwnerUserId = habit.OwnerUserId
          Description =
            if System.String.IsNullOrWhiteSpace habit.Description then
                None
            else
                Some habit.Description }

    interface IHabitsClient with
        member this.CreateAsync (request: CreateHabitDto) : Task<HabitDto> =
            task {
                let description =
                    request.Description
                    |> Option.defaultValue String.Empty

                let request: HabitsService.V1.CreateHabitRequest =
                    CreateHabitRequest (
                        Name = request.Name,
                        OwnerUserId = request.OwnerUserId,
                        Description = description
                    )

                let! response = grpcClient.CreateAsync request

                return toHabitDto response.Habit
            }

        member this.DeleteAsync (habitId: int) : Task<int> =
            task {
                let request = DeleteHabitRequest (Id = habitId)
                let! response = grpcClient.DeleteAsync request
                return response.DeletedRowsCount
            }

        member this.GetByIdsAsync (habitIds: int list) : Task<HabitDto list> =
            task {
                let request = GetByIdsRequest ()
                request.HabitIds.AddRange habitIds
                let! response = grpcClient.GetByIdsAsync request

                return
                    response.Habits
                    |> Seq.map toHabitDto
                    |> List.ofSeq
            }

        member this.GetByUserIdsAsync (userIds: int list) : Task<HabitDto list> =
            task {
                let request = GetByUserIdsRequest ()
                do request.UserIds.AddRange userIds
                let! response = grpcClient.GetByUserIdsAsync request

                return
                    response.Habits
                    |> Seq.map toHabitDto
                    |> List.ofSeq
            }
