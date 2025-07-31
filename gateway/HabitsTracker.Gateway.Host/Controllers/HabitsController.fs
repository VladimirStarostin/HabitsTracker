namespace HabitTrackerApi.Controllers

open Microsoft.AspNetCore.Mvc
open HabitTrackerApi.Models

[<ApiController>]
[<Route("api/habits")>]
type HabitsController () =
    inherit ControllerBase ()

    [<HttpGet>]
    member _.GetAll () : ActionResult<HabitDto list> = ActionResult<HabitDto list> (null)

    [<HttpGet("{habitId}")>]
    member _.GetById (habitId: int64) : ActionResult<HabitDto> = ActionResult<HabitDto> (null)

    [<HttpPost>]
    member _.Create ([<FromBody>] payload: CreateHabitRequest) : ActionResult<HabitDto> = ActionResult<HabitDto> (null)

    [<HttpPut("{habitId}")>]
    member _.Update (habitId: int64, [<FromBody>] payload: UpdateHabitRequest) : IActionResult = Ok () :> IActionResult

    [<HttpDelete("{habitId}")>]
    member _.Delete (habitId: int64) : IActionResult = NoContent () :> IActionResult
