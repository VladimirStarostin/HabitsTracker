module TrackingService.Tests.DataAccess.Utils.Common

open System
open System.Threading.Tasks

module Habits = TrackingService.Host.DataAccess.Habits
module HabitEventsUtils = TrackingService.Tests.DataAccess.Utils.HabitEventsUtils

let defaultDate = DateTime.Parse "2025-07-28"

let internal connectionStringForTests =
    "Host=localhost;Port=5432;Database=tracking_db;Username=habits_tracker_user;Password=1W@nt70m3J0b"

let clearDbAsync () =
    task {
        do!
            (fun conn ->
                task {
                    let! _ = HabitEventsUtils.deleteAllAsync conn
                    return ()
                }
            )
            |> HabitsTracker.Helpers.Postgres.executeWithConnection connectionStringForTests
    }
    :> Task
