module HabitsTracker.Gateway.Clients.Events

open System.Net.Http
open System.Net.Http.Json

module Api = HabitsTracker.Gateway.Api.V1.Events

let getEventsByHabitId (client: HttpClient) (habitId: int) =
    let url = sprintf Api.GetHabitEventsByHabitId habitId
    client.GetFromJsonAsync<Api.HabitEventInfo list> (url)

let getEventById (client: HttpClient) (eventId: int) =
    let url = sprintf Api.GetHabitEventById eventId
    client.GetFromJsonAsync<Api.HabitEventInfo> (url)

let deleteEvent (client: HttpClient) (eventId: int) =
    let url = sprintf Api.DeleteHabitEventById eventId
    client.DeleteAsync (url)

let createEvent (client: HttpClient) (habitId: int) =
    let url = sprintf Api.CreateHabitEvent habitId
    client.PostAsync (url, null)
