module HabitsService.Tests.DataAccess.Utils.Common

open System.Threading.Tasks

module Habits = HabitsService.Host.DataAccess.Habits
module HabitsUtils = HabitsService.Tests.DataAccess.Utils.Habits

let connectionStringForTests =
    "Host=localhost;Port=5432;Database=habits_db;Username=habits_tracker_user;Password=1W@nt70m3J0b"

let clearDbAsync (connectionString: string) =
    task {
        let! _ =
            Habits.deleteAllAsync
            |> HabitsTracker.Helpers.Postgres.executeWithConnection connectionString

        return ()
    }
    :> Task
