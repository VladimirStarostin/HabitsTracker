module Domain.HabitsService

open System
open NUnit.Framework

open HabitsTracker.Gateway.Host.Domain.Services.HabitsService
open HabitsTracker.Gateway.Host.Infrastructure.Clients

[<Test>]
let ``deleteHabitAsync - habit not found returns HabitNotFound`` () =
    task {
        let habitsClient =
            { new IHabitsClient with
                member _.DeleteAsync _ _ = task { return 0 }
                member _.CreateAsync _ = task { return Unchecked.defaultof<_> }
                member _.GetByIdsAsync _ = task { return [] }
                member _.GetByUserIdsAsync _ = task { return [] } }

        match! Steps.deleteHabitAsync 1 42 habitsClient with
        | DeleteHabitPipelineResult.Error DeleteHabitError.HabitNotFound -> Assert.Pass ()
        | _ -> Assert.Fail "Expected HabitNotFound error"
    }

[<Test>]
let ``tryDeleteHabitAsync - removes events and habit`` () =
    task {
        let events =
            ref
                [ { Id = 1
                    HabitId = 1
                    Date = DateTimeOffset.UtcNow }
                  { Id = 2
                    HabitId = 1
                    Date = DateTimeOffset.UtcNow } ]

        let trackingClient =
            { new IHabitsTrackingClient with
                member _.GetByHabitIdsAsync ids =
                    task {
                        return
                            events.Value
                            |> List.filter (fun e -> List.contains e.HabitId ids)
                    }

                member _.GetByIdsAsync ids =
                    task {
                        return
                            events.Value
                            |> List.filter (fun e -> List.contains e.Id ids)
                    }

                member _.TrackHabitAsync _ _ = task { return Unchecked.defaultof<_> }

                member _.DeleteAsync ids _ =
                    task {
                        events.Value <- List.filter (fun e -> ids |> List.contains e.Id |> not) !events
                        return 1
                    } }

        let habitDeleted = ref false

        let habitsClient =
            { new IHabitsClient with
                member _.DeleteAsync _ _ =
                    task {
                        habitDeleted.Value <- true
                        return 1
                    }

                member _.CreateAsync _ = task { return Unchecked.defaultof<_> }
                member _.GetByIdsAsync _ = task { return [] }
                member _.GetByUserIdsAsync _ = task { return [] } }

        match! tryDeleteHabitAsync 1 10 habitsClient trackingClient with
        | DeleteHabitPipelineResult.Ok _ ->
            Assert.That (!events, Is.Empty)
            Assert.That (!habitDeleted, Is.True)
        | _ -> Assert.Fail ()
    }
