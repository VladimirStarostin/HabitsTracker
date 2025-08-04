module HabitsTracker.Gateway.Clients.Auth

open System
open System.Net.Http
open System.Net.Http.Json

open HabitsTracker.Helpers

module Api = HabitsTracker.Gateway.Api.V1.Auth

let toUri (baseUri: Uri) (endpointRoute: string) =
    match Uri.TryCreate (baseUri, endpointRoute) with
    | true, uri -> uri
    | _ -> failwith "Smth went wrong"

let register (client: HttpClient) (serviceAddress: Uri) (input: Api.RegisterParameters) =
    client.PostAsJsonAsync (toUri serviceAddress Api.Register, input)
    |> Task.mapAsync (fun r -> r.Content.ReadFromJsonAsync<Api.AuthResponse> ())

let login (client: HttpClient) (input: Api.LoginParameters) =
    client.PostAsJsonAsync ("api/v1/auth/login", input)
    |> Task.mapAsync (fun r ->
        task {
            let! content = r.Content.ReadAsStringAsync ()
            return System.Text.Json.JsonSerializer.Deserialize<Api.AuthResponse> (content)
        }
    )

let refresh (client: HttpClient) (serviceAddress: Uri) (input: Api.RefreshTokenParameters) =
    client.PostAsJsonAsync (toUri serviceAddress Api.Refresh, input)
    |> Task.mapAsync (fun r -> r.Content.ReadFromJsonAsync<Api.AuthResponse> ())
