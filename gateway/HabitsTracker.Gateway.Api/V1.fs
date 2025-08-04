namespace HabitsTracker.Gateway.Api

module V1 =
    [<Literal>]
    let Prefix = "api/v1/"

    module Events =
        [<Literal>]
        let Resourse = Prefix + "events/"

        type CreateHabitParameters =
            { Name: string
              Description: string }

        type CreatedHabit =
            { Id: int
              Name: string
              Description: string }
