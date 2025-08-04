module HabitsTracker.Gateway.Clients.Auth

open System.Net.Http
open System.Net.Http.Json

open HabitsTracker.Helpers

module Api = HabitsTracker.Gateway.Api.V1.Auth

let readStraight = fun (r: HttpResponseMessage) -> r.Content.ReadFromJsonAsync<Api.AuthResponse> ()
let readDebug = fun (r: HttpResponseMessage) ->
    task {
        let! content = r.Content.ReadAsStringAsync ()
        return System.Text.Json.JsonSerializer.Deserialize<Api.AuthResponse> (content)
    }

let register (client: HttpClient) (input: Api.RegisterParameters) =
    client.PostAsJsonAsync ("api/v1/auth/register", input)
    |> Task.mapAsync readStraight

let login (client: HttpClient) (input: Api.LoginParameters) =
    client.PostAsJsonAsync ("api/v1/auth/login", input)
    |> Task.mapAsync readStraight

let refresh (client: HttpClient) (input: Api.RefreshTokenParameters) =
    client.PostAsJsonAsync ("/api/v1/auth/refresh", input)
    |> Task.mapAsync readStraight
