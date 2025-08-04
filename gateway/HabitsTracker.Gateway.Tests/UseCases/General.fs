module General.UseCases

open Microsoft.AspNetCore.Mvc.Testing
open NUnit.Framework

open HabitsTracker.Gateway.Api.V1
open HabitsTracker.Gateway.Clients

module UsersCommon = UserService.Tests.DataAccess.Utils.Common
module HabitsCommon = HabitsService.Tests.DataAccess.Utils.Common
module TrackingCommon = TrackingService.Tests.DataAccess.Utils.Common
module UsersUtils = UserService.Tests.DataAccess.Utils.Users
module RefreshTokens = UserService.Tests.DataAccess.Utils.RefreshTokens

let defaultRegisterParameters: Auth.RegisterParameters =
    { Email = "test@email.com"
      Password = "p@$$w0rd"
      Name = "Bob" }

let factory = new WebApplicationFactory<Program.Program> ()
let client = factory.CreateClient ()

[<SetUp>]
let Setup () =
    task {
        do! UsersCommon.clearDbAsync ()
        do! HabitsCommon.clearDbAsync ()
        return ()
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

[<Test>]
let ``General use case`` () =
    task {
        // 1. registering a new user
        let! registerResp = Auth.register client defaultRegisterParameters

        match registerResp with
        | Ok resp ->
            Assert.That (resp.AccessToken, Is.Not.Empty)
            Assert.That (resp.RefreshToken, Is.Not.Empty)
        | _ -> Assert.Fail ()

        // 2.
        let! authResp =
            Auth.login
                client
                { Email = defaultRegisterParameters.Email
                  Password = defaultRegisterParameters.Password }

        Assert.That (authResp.IsOk)

        let accessToken =
            match authResp with
            | Ok resp ->
                Assert.That (resp.AccessToken, Is.Not.Empty)
                Assert.That (resp.RefreshToken, Is.Not.Empty)
                resp.AccessToken
            | _ ->
                Assert.Fail ()
                ""

        // 3. Creating a new habit
        let newHabit =
            { Name = "Some name"
              Description = "Some descriptive description" }
            : Habits.CreateHabitParameters

        let! createHabResp = Habits.createHabit client accessToken newHabit

        let habitId =
            match createHabResp with
            | Ok resp ->
                Assert.That (resp.Id, Is.GreaterThan 0)
                Assert.That (resp.Name, Is.EqualTo newHabit.Name)
                Assert.That (resp.Description, Is.EqualTo newHabit.Description)

                resp.Id
            | _ ->
                Assert.Fail ()
                failwith ""

        // 4.1
        let! createEventResp1 = Events.createEvent client accessToken { Events.HabitId = habitId }

        let newEvent1 =
            match createEventResp1 with
            | Ok newEvent ->
                Assert.That (newEvent.HabitId, Is.EqualTo habitId)
                newEvent
            | _ ->
                Assert.Fail ()
                failwith ""

        // 4.2
        let newEvent2 =
            match createEventResp1 with
            | Ok newEvent ->
                Assert.That (newEvent.HabitId, Is.EqualTo habitId)
                newEvent
            | _ ->
                Assert.Fail ()
                failwith ""

        // 5.1
        let! deleteEvResp = Events.deleteEvent client accessToken newEvent1.Id

        match deleteEvResp with
        | Ok rowsDeleted -> Assert.That (rowsDeleted, Is.EqualTo 1)
        | _ -> Assert.Fail ()

        // 6.
        let! deleteHabResp = Habits.deleteHabitById client accessToken habitId

        match deleteEvResp with
        | Ok rowsDeleted -> Assert.That (rowsDeleted, Is.EqualTo 1)
        | _ -> Assert.Fail ()

        ()
    }
