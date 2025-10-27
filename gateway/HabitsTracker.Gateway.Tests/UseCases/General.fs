module HabitsTracker.Gateway.Tests.General.E2eUseCases

open System
open System.IO

open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.Hosting

open NUnit.Framework

open HabitsTracker.Gateway.Api.V1
open HabitsTracker.Gateway.Clients
open HabitsTracker.Helpers

module HabitsCommon = HabitsService.Tests.DataAccess.Utils.Common
module TrackingCommon = TrackingService.Tests.DataAccess.Utils.Common
module UsersCommon = UserService.Tests.DataAccess.Utils.Common

module HabitsUtils = HabitsService.Tests.DataAccess.Utils.Habits
module RefreshTokensUtils = UserService.Tests.DataAccess.Utils.RefreshTokens
module TrackingUtils = TrackingService.Tests.DataAccess.Utils.HabitEventsUtils
module UsersUtils = UserService.Tests.DataAccess.Utils.Users

type TestAppFactory() =
    inherit WebApplicationFactory<HabitsTracker.Gateway.Host.Infrastructure.Clients.IAuthClient>()

    override _.CreateHost(builder: IHostBuilder) =
        let envFile = Path.Combine(AppContext.BaseDirectory, ".env.tests")

        if File.Exists(envFile) then
            File.ReadAllLines(envFile)
            |> Array.iter (fun line ->
                if not (line.StartsWith("#") || String.IsNullOrWhiteSpace line) then
                    let parts = line.Split('=', 2)
                    if parts.Length = 2 then
                        Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim()))

        base.CreateHost(builder)

let factory = new TestAppFactory()
let client = factory.CreateClient()

[<OneTimeTearDown>]
let tearDown () =
    do client.Dispose()
    do factory.Dispose()

let defaultRegisterParameters: Auth.RegisterParameters =
    { Email = "test@email.com"
      Password = "p@$$w0rd"
      Name = "Bob" }

[<SetUp>]
let Setup () =
    task {
        do!
            //"USER_DB_CONNECTION_STRING"
            //|> System.Environment.GetEnvironmentVariable
            //|> Option.ofObj
            //|> Option.defaultValue
            UsersCommon.connectionStringForTests
            |> UsersCommon.clearDbAsync

        do!
            //"HABITS_DB_CONNECTION_STRING"
            //|> System.Environment.GetEnvironmentVariable
            //|> Option.ofObj
            //|> Option.defaultValue
            HabitsCommon.connectionStringForTests
            |> HabitsCommon.clearDbAsync

        do!
            //"TRACKING_DB_CONNECTION_STRING"
            //|> System.Environment.GetEnvironmentVariable
            //|> Option.ofObj
            //|> Option.defaultValue
            TrackingCommon.connectionStringForTests
            |> TrackingCommon.clearDbAsync

        do Dapper.addRequiredTypeHandlers ()
        return ()
    }

type RefreshTokenTestDto =
    { Token: string
      IsRevoked: bool }

type UserTestDto =
    { Email: string
      Name: string }

type HabitsTestDto =
    { OwnerUserId: int
      Name: string
      Description: string option }

type EventsTestDto =
    { HabitId: int
      UserId: int
      Date: DateTimeOffset }

let getRawDbStatesAsync () =
    task {
        let! refreshTokensActual =
            RefreshTokensUtils.getAllAsync
            |> Postgres.executeWithConnection UsersCommon.connectionStringForTests

        let! usersActual =
            UsersUtils.getAllAsync
            |> Postgres.executeWithConnection UsersCommon.connectionStringForTests

        let! habitsActual =
            HabitsUtils.getAllAsync
            |> Postgres.executeWithConnection HabitsCommon.connectionStringForTests

        let! eventsActual =
            TrackingUtils.getAllAsync
            |> Postgres.executeWithConnection TrackingCommon.connectionStringForTests

        return refreshTokensActual, usersActual, habitsActual, eventsActual
    }

let getDbStatesAsync () =
    task {
        let! refreshTokensActual, usersActual, habitsActual, eventsActual = getRawDbStatesAsync ()

        return
            refreshTokensActual
            |> List.map (fun rt ->
                { Token = rt.Token
                  IsRevoked = rt.IsRevoked }
            ),
            usersActual
            |> List.map (fun u ->
                { Email = u.Email
                  Name = u.Name }
            ),
            habitsActual
            |> List.map (fun h ->
                { OwnerUserId = h.OwnerUserId
                  Name = h.Name
                  Description = h.Description }
            ),
            eventsActual
            |> List.map (fun e ->
                { HabitId = e.HabitId
                  UserId = e.UserId
                  Date = e.Date }
            )
    }

[<Test>]
let ``Db empty - user login fails`` () =
    task {
        match!
            Auth.login
                client
                { Email = defaultRegisterParameters.Email
                  Password = defaultRegisterParameters.Password }
        with
        | Ok _ -> Assert.Fail ()
        | Error err ->
            Assert.That (
                err,
                Is.EqualTo
                    { status = 404
                      title = "NotFound"
                      raw = Some """{"detail":"User not found.","status":404,"title":"NotFound"}"""
                      detail = "User not found." }
            )
    }

let normalizeToMicroSeconds (dto: System.DateTimeOffset) =
    let utcTicks = dto.UtcTicks - (dto.UtcTicks % 10L)
    System.DateTimeOffset(utcTicks, System.TimeSpan.Zero).ToOffset (dto.Offset)

[<Test>]
let ``General use case, ends with habit deletion - habits and events are erased`` () =
    task {
        // 1. registering a new user
        let! refreshTokensActual, usersActual, habitsActual, eventsActual = getDbStatesAsync ()

        Assert.That (refreshTokensActual, Is.Empty)
        Assert.That (usersActual, Is.Empty)
        Assert.That (habitsActual, Is.Empty)
        Assert.That (eventsActual, Is.Empty)

        let! refreshTokenAfterRegistration, accessToken =
            task {
                match! Auth.register client defaultRegisterParameters with
                | Ok resp ->
                    Assert.That (
                        resp.AccessToken
                        |> String.IsNullOrWhiteSpace
                        |> not
                    )

                    Assert.That (
                        resp.RefreshToken
                        |> String.IsNullOrWhiteSpace
                        |> not
                    )

                    let expectedUsers =
                        List.singleton
                            { Name = defaultRegisterParameters.Name
                              Email = defaultRegisterParameters.Email }

                    let expectedRefreshTokens =
                        List.singleton
                            { Token = resp.RefreshToken
                              IsRevoked = false }

                    let! refreshTokensActual, usersActual, habitsActual, eventsActual = getDbStatesAsync ()

                    Assert.That (usersActual, Is.EqualTo expectedUsers)
                    Assert.That (refreshTokensActual, Is.EqualTo expectedRefreshTokens)
                    Assert.That (habitsActual, Is.Empty)
                    Assert.That (eventsActual, Is.Empty)

                    return resp.RefreshToken, resp.AccessToken

                | _ -> return failwith "Registering failed"
            }

        let! _, rawUsers, _, _ = getRawDbStatesAsync ()
        let userId = rawUsers |> List.exactlyOne |> (fun e -> e.Id)

        // 2.
        let! refreshTokens, newAccessToken =
            task {
                match!
                    Auth.login
                        client
                        { Email = defaultRegisterParameters.Email
                          Password = defaultRegisterParameters.Password }
                with
                | Ok resp ->
                    Assert.That (not <| String.IsNullOrWhiteSpace resp.AccessToken)
                    Assert.That (not <| String.IsNullOrWhiteSpace resp.RefreshToken)
                    Assert.That (resp.RefreshToken, Is.Not.EqualTo refreshTokenAfterRegistration)

                    let expectedUsers =
                        List.singleton
                            { Name = defaultRegisterParameters.Name
                              Email = defaultRegisterParameters.Email }

                    let expectedRefreshTokens =
                        [ { Token = refreshTokenAfterRegistration
                            IsRevoked = true }
                          { Token = resp.RefreshToken
                            IsRevoked = false } ]

                    let! refreshTokensActual, usersActual, habitsActual, eventsActual = getDbStatesAsync ()

                    Assert.That (usersActual, Is.EqualTo expectedUsers)
                    Assert.That (refreshTokensActual, Is.EquivalentTo expectedRefreshTokens)
                    Assert.That (habitsActual, Is.Empty)
                    Assert.That (eventsActual, Is.Empty)

                    return refreshTokensActual, resp.AccessToken
                | err -> return failwith $"Login failed unexpectedly: {err.ToString()}"
            }

        // 3. Creating a new habit
        let! habitId, expectedHabits =
            task {
                let newHabit =
                    { Name = "Some name"
                      Description = "Some descriptive description" }
                    : Habits.CreateHabitParameters

                match! Habits.createHabit client newAccessToken newHabit with
                | Ok resp ->
                    let expectedHabits: HabitsTestDto list =
                        List.singleton
                            { OwnerUserId = userId
                              Name = newHabit.Name
                              Description = Some newHabit.Description }

                    let! refreshTokensActual, usersActual, habitsActual, eventsActual = getDbStatesAsync ()

                    Assert.That (usersActual, Is.EqualTo usersActual)
                    Assert.That (refreshTokensActual, Is.EquivalentTo refreshTokens)
                    Assert.That (habitsActual, Is.EqualTo expectedHabits)
                    Assert.That (eventsActual, Is.Empty)

                    return resp.Id, expectedHabits
                | Error err -> return failwith $"Habit creating failed unexpectedly: {err}"
            }

        // 4.1
        let! event1 =
            task {
                match! Events.createEvent client newAccessToken { Events.HabitId = habitId } with
                | Ok newEvent ->
                    Assert.That (newEvent.HabitId, Is.EqualTo habitId)
                    let! refreshTokensActual, usersActual, habitsActual, eventsActual = getDbStatesAsync ()

                    let expectedEvents: EventsTestDto list =
                        List.singleton
                            { UserId = userId
                              HabitId = habitId
                              Date = normalizeToMicroSeconds newEvent.Date }

                    Assert.That (usersActual, Is.EqualTo usersActual)
                    Assert.That (refreshTokensActual, Is.EquivalentTo refreshTokens)
                    Assert.That (habitsActual, Is.EqualTo expectedHabits)
                    Assert.That (eventsActual, Is.EquivalentTo expectedEvents)
                    return newEvent
                | _ -> return failwith "Habit creating failed unexpectedly"
            }

        // 4.2
        let! event2 =
            task {
                match! Events.createEvent client newAccessToken { Events.HabitId = habitId } with
                | Ok newEvent ->
                    Assert.That (newEvent.HabitId, Is.EqualTo habitId)
                    let! refreshTokensActual, usersActual, habitsActual, eventsActual = getDbStatesAsync ()

                    let expectedEvents: EventsTestDto list =
                        [ { UserId = userId
                            HabitId = habitId
                            Date = normalizeToMicroSeconds event1.Date }
                          { UserId = userId
                            HabitId = habitId
                            Date = normalizeToMicroSeconds newEvent.Date } ]

                    Assert.That (usersActual, Is.EqualTo usersActual)
                    Assert.That (refreshTokensActual, Is.EquivalentTo refreshTokens)
                    Assert.That (habitsActual, Is.EqualTo expectedHabits)
                    Assert.That (eventsActual, Is.EquivalentTo expectedEvents)
                    return newEvent
                | _ -> return failwith "Habit creating failed unexpectedly"
            }

        // 5.1
        do!
            task {
                match! Events.deleteEvent client newAccessToken event1.Id with
                | Ok rowsDeleted ->
                    Assert.That (rowsDeleted, Is.EqualTo 1)
                    let! refreshTokensActual, usersActual, habitsActual, eventsActual = getDbStatesAsync ()

                    let expectedEvents: EventsTestDto list =
                        List.singleton
                            { UserId = userId
                              HabitId = habitId
                              Date = normalizeToMicroSeconds event2.Date }

                    Assert.That (usersActual, Is.EqualTo usersActual)
                    Assert.That (refreshTokensActual, Is.EquivalentTo refreshTokens)
                    Assert.That (habitsActual, Is.EqualTo expectedHabits)
                    Assert.That (eventsActual, Is.EqualTo expectedEvents)
                | _ -> return failwith "Event deleting failed unexpectedly"
            }

        // 6.
        do!
            task {
                match! Habits.deleteHabitById client newAccessToken habitId with
                | Ok rowsDeleted ->
                    Assert.That (rowsDeleted, Is.EqualTo 1)
                    let! refreshTokensActual, usersActual, habitsActual, eventsActual = getDbStatesAsync ()

                    Assert.That (usersActual, Is.EqualTo usersActual)
                    Assert.That (refreshTokensActual, Is.EquivalentTo refreshTokens)
                    Assert.That (habitsActual, Is.Empty)
                    Assert.That (eventsActual, Is.Empty)
                | Error err -> return failwith $"Habit deleting failed unexpectedly {err}"
            }
    }

[<Test>]
let ``General use case, ends with user deletion - habits, events and users are erased`` () =
    task {
        // 1. registering a new user
        let! initialRefreshTokens, initialUsers, initialHabits, initialEvents = getDbStatesAsync ()

        Assert.That (initialRefreshTokens, Is.Empty)
        Assert.That (initialUsers, Is.Empty)
        Assert.That (initialHabits, Is.Empty)
        Assert.That (initialEvents, Is.Empty)

        let! refreshTokenAfterRegistration, usersInDb =
            task {
                match! Auth.register client defaultRegisterParameters with
                | Ok resp ->
                    Assert.That (
                        resp.AccessToken
                        |> String.IsNullOrWhiteSpace
                        |> not
                    )

                    Assert.That (
                        resp.RefreshToken
                        |> String.IsNullOrWhiteSpace
                        |> not
                    )

                    let expectedUsers =
                        List.singleton
                            { Name = defaultRegisterParameters.Name
                              Email = defaultRegisterParameters.Email }

                    let expectedRefreshTokens =
                        List.singleton
                            { Token = resp.RefreshToken
                              IsRevoked = false }

                    let! refreshTokensActual, usersActual, habitsActual, eventsActual = getDbStatesAsync ()

                    Assert.That (usersActual, Is.EqualTo expectedUsers)
                    Assert.That (refreshTokensActual, Is.EqualTo expectedRefreshTokens)
                    Assert.That (habitsActual, Is.Empty)
                    Assert.That (eventsActual, Is.Empty)

                    return resp.RefreshToken, usersActual

                | _ -> return failwith "Registering failed"
            }

        let! _, rawUsers, _, _ = getRawDbStatesAsync ()
        let userId = rawUsers |> List.exactlyOne |> (fun e -> e.Id)

        // 2.
        let! refreshTokens, accessToken =
            task {
                match!
                    Auth.login
                        client
                        { Email = defaultRegisterParameters.Email
                          Password = defaultRegisterParameters.Password }
                with
                | Ok resp ->
                    Assert.That (not <| String.IsNullOrWhiteSpace resp.AccessToken)
                    Assert.That (not <| String.IsNullOrWhiteSpace resp.RefreshToken)
                    Assert.That (resp.RefreshToken, Is.Not.EqualTo refreshTokenAfterRegistration)

                    let expectedRefreshTokens =
                        [ { Token = refreshTokenAfterRegistration
                            IsRevoked = true }
                          { Token = resp.RefreshToken
                            IsRevoked = false } ]

                    let! refreshTokensActual, usersActual, habitsActual, eventsActual = getDbStatesAsync ()

                    Assert.That (usersActual, Is.EqualTo usersInDb)
                    Assert.That (refreshTokensActual, Is.EquivalentTo expectedRefreshTokens)
                    Assert.That (habitsActual, Is.Empty)
                    Assert.That (eventsActual, Is.Empty)

                    return refreshTokensActual, resp.AccessToken
                | _ -> return failwith "Login failed unexpectedly"
            }

        // 3. Creating a new habit
        let! habitId, expectedHabits =
            task {
                let newHabit =
                    { Name = "Some name"
                      Description = "Some descriptive description" }
                    : Habits.CreateHabitParameters

                match! Habits.createHabit client accessToken newHabit with
                | Ok resp ->
                    let expectedHabits: HabitsTestDto list =
                        List.singleton
                            { OwnerUserId = userId
                              Name = newHabit.Name
                              Description = Some newHabit.Description }

                    let! refreshTokensActual, usersActual, habitsActual, eventsActual = getDbStatesAsync ()

                    Assert.That (usersActual, Is.EqualTo usersInDb)
                    Assert.That (refreshTokensActual, Is.EquivalentTo refreshTokens)
                    Assert.That (habitsActual, Is.EqualTo expectedHabits)
                    Assert.That (eventsActual, Is.Empty)

                    return resp.Id, expectedHabits
                | _ -> return failwith "Habit creating failed unexpectedly"
            }

        // 4.1 Event creation
        let! event1 =
            task {
                match! Events.createEvent client accessToken { Events.HabitId = habitId } with
                | Ok newEvent ->
                    Assert.That (newEvent.HabitId, Is.EqualTo habitId)
                    let! refreshTokensActual, usersActual, habitsActual, eventsActual = getDbStatesAsync ()

                    let expectedEvents: EventsTestDto list =
                        List.singleton
                            { UserId = userId
                              HabitId = habitId
                              Date = normalizeToMicroSeconds newEvent.Date }

                    Assert.That (usersActual, Is.EqualTo usersInDb)
                    Assert.That (refreshTokensActual, Is.EquivalentTo refreshTokens)
                    Assert.That (habitsActual, Is.EqualTo expectedHabits)
                    Assert.That (eventsActual, Is.EquivalentTo expectedEvents)
                    return newEvent
                | _ -> return failwith "Habit creating failed unexpectedly"
            }

        // 4.2 Another event creation
        let! event2 =
            task {
                match! Events.createEvent client accessToken { Events.HabitId = habitId } with
                | Ok newEvent ->
                    Assert.That (newEvent.HabitId, Is.EqualTo habitId)
                    let! refreshTokensActual, usersActual, habitsActual, eventsActual = getDbStatesAsync ()

                    let expectedEvents: EventsTestDto list =
                        [ { UserId = userId
                            HabitId = habitId
                            Date = normalizeToMicroSeconds event1.Date }
                          { UserId = userId
                            HabitId = habitId
                            Date = normalizeToMicroSeconds newEvent.Date } ]

                    Assert.That (usersActual, Is.EqualTo usersInDb)
                    Assert.That (refreshTokensActual, Is.EquivalentTo refreshTokens)
                    Assert.That (habitsActual, Is.EqualTo expectedHabits)
                    Assert.That (eventsActual, Is.EquivalentTo expectedEvents)
                    return newEvent
                | _ -> return failwith "Habit creating failed unexpectedly"
            }

        // 5. User deleting
        do!
            task {
                match! Users.deleteUser client accessToken with
                | Ok rowsDeleted ->
                    Assert.That (rowsDeleted, Is.EqualTo 1)
                    let! refreshTokensActual, usersActual, habitsActual, eventsActual = getDbStatesAsync ()

                    Assert.That (usersActual, Is.Empty)
                    Assert.That (refreshTokensActual, Is.Empty)
                    Assert.That (habitsActual, Is.Empty)
                    Assert.That (eventsActual, Is.Empty)
                | Error err -> return failwith $"Habit deleting failed unexpectedly {err}"
            }
    }
