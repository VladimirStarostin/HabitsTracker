namespace HabitTrackerApi.Controllers

open System.Threading.Tasks

open Microsoft.AspNetCore.Mvc

open HabitTracker.Host.Infrastructure.GrpcClients

[<ApiController>]
[<Route("api/v1")>]
type TrackingController (client: ITrackingGrpcClient) =
    inherit ControllerBase ()

    [<HttpGet("events/{habitId}")>]
    member _.GetEvents (habitId: int64) : ActionResult<HabitEventDto list> = ActionResult<HabitEventDto list> (null)

    [<HttpGet("events/{eventId}")>]
    member _.GetEventById (eventId: int64) : Task<ActionResult<HabitEventDto>> =
        task {
            let! event = client.GetEventByIdAsync eventId
            let res = ActionResult<HabitEventDto>(event)
            return res
        }

    [<HttpPost("events/{habitId}")>]
    member _.CreateEvent
        ([<FromRoute>] habitId: int64, [<FromBody>] payload: CreateEventRequest)
        : Task<ActionResult<HabitEventDto>> =
        task {
            let! response = client.CreateEventAsync habitId payload
            return ActionResult<HabitEventDto> response
        }

    [<HttpDelete("events/{eventId}")>]
    member _.DeleteEvent (eventId: int64) : ActionResult =
        task {
            do! client.DeleteEventAsync eventId
            return NoContentResult()
        }
