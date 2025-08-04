module HabitsTracker.Gateway.Clients.Habits

open System.Net.Http
open System.Net.Http.Json

type CreateHabitParameters =
    { Name: string
      Description: string }

type Habit =
    { Id: int
      Name: string
      Description: string }

let getHabitById (client: HttpClient) (habitId: int) =
    task {
        let url = $"/habit/{habitId}"
        return! client.GetFromJsonAsync<Habit> (url)
    }

let getUserHabits (client: HttpClient) (userId: int) =
    task {
        let url = $"/habit/{userId}"
        return! client.GetFromJsonAsync<Habit list> (url)
    }

let createHabit (client: HttpClient) (userId: int) (input: CreateHabitParameters) =
    task { return! client.PostAsJsonAsync ("/habit", input) }

let deleteHabit (client: HttpClient) (habitId: int) =
    task {
        let url = $"/habit/{habitId}"
        return! client.DeleteAsync (url)
    }
