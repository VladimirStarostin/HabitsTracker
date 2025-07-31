namespace HabitTrackerApi.Controllers

open Microsoft.AspNetCore.Mvc
open HabitTrackerApi.Models

[<ApiController>]
[<Route("api/auth")>]
type AuthController () =
    inherit ControllerBase ()

    [<HttpPost("register")>]
    member _.Register ([<FromBody>] payload: RegisterRequest) : ActionResult<AuthResponse> =
        // TODO: implement registration
        ActionResult<AuthResponse> (null)

    [<HttpPost("login")>]
    member _.Login ([<FromBody>] payload: LoginRequest) : ActionResult<AuthResponse> =
        // TODO: implement login
        ActionResult<AuthResponse> (null)

    [<HttpPost("refresh")>]
    member _.Refresh ([<FromBody>] payload: RefreshRequest) : ActionResult<AuthResponse> =
        // TODO: implement token refresh
        ActionResult<AuthResponse> (null)

    [<HttpGet("me")>]
    member _.Me () : ActionResult<CurrentUserDto> =
        // TODO: return current user info
        ActionResult<CurrentUserDto> (null)
