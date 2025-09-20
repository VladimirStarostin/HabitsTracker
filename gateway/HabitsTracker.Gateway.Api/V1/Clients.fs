module HabitsTracker.Gateway.Clients

open System.Threading.Tasks
open System.Net.Http
open System.Net.Http.Json
open System.Net.Http.Headers
open System.Text.Json

open HabitsTracker.Helpers

type HttpProblem =
    { status: int
      title: string
      detail: string
      raw: string option }

type ApiResponse<'a> = Task<Result<'a, HttpProblem>>

let private jsonOpts =
    let o = JsonSerializerOptions ()
    o.PropertyNameCaseInsensitive <- true
    o

let readAs<'T> (resp: HttpResponseMessage) : ApiResponse<'T> =
    task {
        let! body = resp.Content.ReadAsStringAsync ()

        if resp.IsSuccessStatusCode then
            try
                let value = JsonSerializer.Deserialize<'T> (body, jsonOpts)
                return Ok value
            with _ ->
                return
                    Error
                        { status = int resp.StatusCode
                          title = "DeserializationError"
                          detail = "Failed to deserialize success response."
                          raw = Some body }
        else
            let tryProblem () =
                try
                    let p = JsonSerializer.Deserialize<HttpProblem> (body, jsonOpts)

                    if isNull (box p) then
                        None
                    else
                        Some
                            { p with
                                status = int resp.StatusCode
                                raw = Some body }
                with _ ->
                    None

            let problem =
                match tryProblem () with
                | Some p -> p
                | None ->
                    { status = int resp.StatusCode
                      title = resp.ReasonPhrase
                      detail =
                        if System.String.IsNullOrWhiteSpace body then
                            "HTTP error"
                        else
                            body
                      raw = Some body }

            return Error problem
    }

module Auth =
    module Api = HabitsTracker.Gateway.Api.V1.Auth

    let register (client: HttpClient) (input: Api.RegisterParameters) : ApiResponse<Api.AuthResponse> =
        client.PostAsJsonAsync ("api/v1/auth/register", input)
        |> Task.mapAsync readAs

    let login (client: HttpClient) (input: Api.LoginParameters) : ApiResponse<Api.AuthResponse> =
        client.PostAsJsonAsync ("api/v1/auth/login", input)
        |> Task.mapAsync readAs

    let refresh (client: HttpClient) (input: Api.RefreshTokenParameters) : ApiResponse<Api.AuthResponse> =
        client.PostAsJsonAsync ("api/v1/auth/refresh", input)
        |> Task.mapAsync readAs

module Habits =
    module Api = HabitsTracker.Gateway.Api.V1.Habits

    let createHabit
        (client: HttpClient)
        (token: string)
        (input: Api.CreateHabitParameters)
        : ApiResponse<Api.Habit> =
        client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue ("Bearer", token)

        client.PostAsJsonAsync ("api/v1/habits", input)
        |> Task.mapAsync readAs

    let getCurrentUserHabits (client: HttpClient) (token: string) : ApiResponse<Api.Habit list> =
        client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue ("Bearer", token)

        client.GetFromJsonAsync "api/v1/habits"
        |> Task.mapAsync readAs

    let getById (client: HttpClient) (token: string) (habitId: int) : ApiResponse<Api.Habit option> =
        client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue ("Bearer", token)

        client.GetAsync $"api/v1/habits/{habitId}"
        |> Task.mapAsync readAs

    let deleteHabitById (client: HttpClient) (token: string) (habitId: int) : ApiResponse<int> =
        client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue ("Bearer", token)

        client.DeleteAsync $"api/v1/habits/{habitId}"
        |> Task.mapAsync readAs

module Events =
    module Api = HabitsTracker.Gateway.Api.V1.Events

    let getEventsByHabitId
        (client: HttpClient)
        (token: string)
        (habitId: int)
        : ApiResponse<Api.HabitEventInfo list> =
        client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue ("Bearer", token)

        client.GetAsync $"api/v1/events/by-habit/{habitId}"
        |> Task.mapAsync readAs

    let getEventById (client: HttpClient) (token: string) (eventId: int) : ApiResponse<Api.HabitEventInfo> =
        client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue ("Bearer", token)

        client.GetAsync $"api/v1/events/{eventId}"
        |> Task.mapAsync readAs

    let deleteEvent (client: HttpClient) (token: string) (eventId: int) : ApiResponse<int> =
        client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue ("Bearer", token)

        client.DeleteAsync $"api/v1/events/{eventId}"
        |> Task.mapAsync readAs

    let createEvent
        (client: HttpClient)
        (token: string)
        (parameters: Api.V1.Events.CreateHabitEventParameters)
        : ApiResponse<Api.HabitEventInfo> =
        client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue ("Bearer", token)

        client.PostAsJsonAsync ("api/v1/events", parameters)
        |> Task.mapAsync readAs

module Users =
    let deleteUser (client: HttpClient) (token: string) : ApiResponse<int> =
        client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue ("Bearer", token)

        client.DeleteAsync "api/v1/users"
        |> Task.mapAsync readAs
