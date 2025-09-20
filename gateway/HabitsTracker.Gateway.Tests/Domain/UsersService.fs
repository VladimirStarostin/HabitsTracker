module Domain.UsersService

open System
open NUnit.Framework

open HabitsTracker.Gateway.Host.Domain.Services.UsersService
open HabitsTracker.Gateway.Host.Domain.Services.HabitsService
open HabitsTracker.Gateway.Host.Infrastructure.Clients

[<Test>]
let ``tryDeleteUserAsync - removes habits, events, refresh tokens and user`` () =
    task {
        let userId = 10

        let habits =
            ref
                [ { Id = 1
                    Name = "Read"
                    OwnerUserId = userId
                    Description = None }
                  { Id = 2
                    Name = "Run"
                    OwnerUserId = userId
                    Description = Some "Morning" } ]

        let events =
            ref
                [ { Id = 1
                    HabitId = 1
                    Date = DateTimeOffset.UtcNow }
                  { Id = 2
                    HabitId = 2
                    Date = DateTimeOffset.UtcNow } ]

        let refreshTokens = ref [ "token-1"; "token-2" ]
        let userDeleted = ref false

        let trackingClient: IHabitsTrackingClient =
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
                        events.Value <- List.filter (fun e -> ids |> List.contains e.Id |> not) events.Value
                        return 1
                    } }

        let habitsClient: IHabitsClient =
            { new IHabitsClient with
                member _.CreateAsync _ = task { return Unchecked.defaultof<_> }

                member _.DeleteAsync habitIds _ =
                    task {
                        let idsSet = habitIds |> Set.ofList

                        let deletedCount =
                            habits.Value
                            |> List.filter (fun h -> Set.contains h.Id idsSet)
                            |> List.length

                        habits.Value <- List.filter (fun h -> not (Set.contains h.Id idsSet)) habits.Value

                        return deletedCount
                    }

                member _.GetByIdsAsync ids =
                    task {
                        return
                            habits.Value
                            |> List.filter (fun h -> List.contains h.Id ids)
                    }

                member _.GetByUserIdsAsync ids =
                    task {
                        return
                            habits.Value
                            |> List.filter (fun h -> List.contains h.OwnerUserId ids)
                    } }

        let usersClient: IUsersClient =
            { new IUsersClient with
                member _.GetByIdsAsync _ = task { return [] }

                member _.DeleteRefreshTokensAsync _ =
                    task {
                        let deleted = refreshTokens.Value.Length
                        refreshTokens.Value <- []
                        return deleted
                    }

                member _.DeleteUsersAsync _ =
                    task {
                        userDeleted.Value <- true
                        return 1
                    } }

        match! tryDeleteUserAsync userId habitsClient trackingClient usersClient with
        | DeleteUserPipelineResult.Ok deleted ->
            Assert.That (deleted, Is.EqualTo 1)
            Assert.That (habits.Value, Is.Empty)
            Assert.That (events.Value, Is.Empty)
            Assert.That (refreshTokens.Value, Is.Empty)
            Assert.That (userDeleted.Value, Is.True)
        | DeleteUserPipelineResult.Error error -> Assert.Fail ($"Unexpected error: {error}")
    }

[<Test>]
let ``tryDeleteUserAsync - failing habit deletion stops pipeline`` () =
    task {
        let userId = 20

        let habits =
            ref
                [ { Id = 5
                    Name = "Read"
                    OwnerUserId = userId
                    Description = None } ]

        let events =
            ref
                [ { Id = 7
                    HabitId = 5
                    Date = DateTimeOffset.UtcNow } ]

        let refreshTokens = ref [ "token" ]
        let tokensDeleted = ref false
        let userDeleted = ref false

        let trackingClient: IHabitsTrackingClient =
            { new IHabitsTrackingClient with
                member _.GetByHabitIdsAsync _ = task { return events.Value }
                member _.GetByIdsAsync _ = task { return events.Value }
                member _.TrackHabitAsync _ _ = task { return Unchecked.defaultof<_> }
                member _.DeleteAsync _ _ = task { return 1 } }

        let habitsClient: IHabitsClient =
            { new IHabitsClient with
                member _.CreateAsync _ = task { return Unchecked.defaultof<_> }
                member _.DeleteAsync _ _ = task { return 0 }
                member _.GetByIdsAsync _ = task { return habits.Value }
                member _.GetByUserIdsAsync _ = task { return habits.Value } }

        let usersClient: IUsersClient =
            { new IUsersClient with
                member _.GetByIdsAsync _ = task { return [] }

                member _.DeleteRefreshTokensAsync _ =
                    task {
                        tokensDeleted.Value <- true
                        refreshTokens.Value <- []
                        return 1
                    }

                member _.DeleteUsersAsync _ =
                    task {
                        userDeleted.Value <- true
                        return 1
                    } }

        match! tryDeleteUserAsync userId habitsClient trackingClient usersClient with
        | DeleteUserPipelineResult.Error (DeleteUserError.HabitDeletion habitError) ->
            Assert.That (habitError.HabitId, Is.EqualTo 5)
            Assert.That (habitError.Error, Is.EqualTo DeleteHabitError.HabitNotFound)
            Assert.That (tokensDeleted.Value, Is.False)
            Assert.That (userDeleted.Value, Is.False)
        | other -> Assert.Fail ($"Unexpected result: {other}")
    }

[<Test>]
let ``tryDeleteUserAsync - user not found after cleanup`` () =
    task {
        let userId = 30

        let habits =
            ref
                [ { Id = 11
                    Name = "Meditate"
                    OwnerUserId = userId
                    Description = None } ]

        let events =
            ref
                [ { Id = 12
                    HabitId = 11
                    Date = DateTimeOffset.UtcNow } ]

        let refreshTokens = ref [ "token" ]
        let deleteCalls = ref 0

        let trackingClient: IHabitsTrackingClient =
            { new IHabitsTrackingClient with
                member _.GetByHabitIdsAsync _ = task { return events.Value }
                member _.GetByIdsAsync _ = task { return events.Value }
                member _.TrackHabitAsync _ _ = task { return Unchecked.defaultof<_> }

                member _.DeleteAsync ids _ =
                    task {
                        events.Value <- List.filter (fun e -> ids |> List.contains e.Id |> not) events.Value
                        return 1
                    } }

        let habitsClient: IHabitsClient =
            { new IHabitsClient with
                member _.CreateAsync _ = task { return Unchecked.defaultof<_> }

                member _.DeleteAsync habitIds _ =
                    task {
                        let idsSet = habitIds |> Set.ofList

                        let deletedCount =
                            habits.Value
                            |> List.filter (fun h -> Set.contains h.Id idsSet)
                            |> List.length

                        habits.Value <- List.filter (fun h -> not (Set.contains h.Id idsSet)) habits.Value

                        return deletedCount
                    }

                member _.GetByIdsAsync _ = task { return habits.Value }
                member _.GetByUserIdsAsync _ = task { return habits.Value } }

        let usersClient: IUsersClient =
            { new IUsersClient with
                member _.GetByIdsAsync _ = task { return [] }

                member _.DeleteRefreshTokensAsync _ =
                    task {
                        refreshTokens.Value <- []
                        return 1
                    }

                member _.DeleteUsersAsync _ =
                    task {
                        incr deleteCalls
                        return 0
                    } }

        match! tryDeleteUserAsync userId habitsClient trackingClient usersClient with
        | DeleteUserPipelineResult.Error DeleteUserError.UserNotFound ->
            Assert.That (deleteCalls.Value, Is.EqualTo 1)
            Assert.That (habits.Value, Is.Empty)
            Assert.That (events.Value, Is.Empty)
            Assert.That (refreshTokens.Value, Is.Empty)
        | other -> Assert.Fail ($"Unexpected result: {other}")
    }
