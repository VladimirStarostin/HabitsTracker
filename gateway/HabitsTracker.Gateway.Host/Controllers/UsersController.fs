namespace HabitTrackerApi.Controllers

open System.Threading.Tasks

open Microsoft.AspNetCore.Mvc

open HabitTrackerApi.GrpcClients

[<ApiController>]
[<Route("api/v1/")>]
type UsersController (client: IUserGrpcClient) =
    inherit ControllerBase ()

    [<HttpDelete("users/{userId}")>]
    member _.Delete (userId: int64) : Task<ActionResult> =
        task {
            do! client.DeleteUserAsync userId
            return NoContentResult ()
        }
