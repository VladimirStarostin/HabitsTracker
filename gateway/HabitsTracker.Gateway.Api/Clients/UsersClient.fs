module HabitsTracker.Clients.Gateway.Clients.Users

open System.Net
open System.Net.Http
open System.Threading.Tasks

let deleteUser (client: HttpClient) (userId: int) =
    task {
        let url = $"/users/{userId}"
        return! client.DeleteAsync (url)
    }
