module HabitsTracker.Gateway.Tests

open System
open NUnit.Framework
open System.Net.Http
open System.Text
open System.Net.Http.Headers
open Microsoft.AspNetCore.Mvc.Testing
open HabitsTracker.Gateway.Host
open HabitsTracker.Gateway.Api.V1
open HabitsTracker.Gateway.Clients

[<SetUp>]
let Setup () = ()

let baseUrl = Uri "http://localhost:5050"

[<Test>]
let Test1 () =
    task {
        let factory = new WebApplicationFactory<Program.Program> ()
        let client = factory.CreateClient () //new HttpClient(BaseAddress = baseUrl) //factory.CreateClient()

        let! resp =
            Auth.login
                client
                { Auth.LoginParameters.Email = "test"
                  Password = "nhgn" }

        Assert.That (not <| String.IsNullOrWhiteSpace resp.AccessToken)
        Assert.That (not <| String.IsNullOrWhiteSpace resp.RefreshToken)
        //// Заглушка вместо ассерта: loginResp.IsSuccessStatusCode должно быть true (200 OK)
        //let! loginBody = loginResp.Content.ReadAsStringAsync ()
        //// TODO: извлечь accessToken из loginBody (например, из JSON поля "token")
        //let accessToken = "<parse JWT token from loginBody>"
        //client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue ("Bearer", accessToken)
        //// 2. Создаём новую привычку
        //let habitJson = """{ "name": "Morning Run", "description": "Run every morning" }"""
        //use habitContent = new StringContent (habitJson, Encoding.UTF8, "application/json")
        //let! createHabResp = client.PostAsync (baseUrl + "/habits", habitContent)
        ////// Заглушка: createHabResp.IsSuccessStatusCode (201 Created или 200 OK)
        ////// Предполагаем, что в ответе возвращается ID новой привычки:
        //let habitId = "<extract habitId from response>"
        //// 3. Создаём два события для этой привычки
        //let eventJson1 = """{ "date": "2025-08-01", "status": "done" }"""

        //use eventContent1 =
        //    new StringContent (eventJson1, Encoding.UTF8, "application/json")

        //let! createEvResp1 = client.PostAsync (sprintf "%s/habits/%s/events" baseUrl habitId, eventContent1)
        //let eventJson2 = """{ "date": "2025-08-02", "status": "done" }"""

        //use eventContent2 =
        //    new StringContent (eventJson2, Encoding.UTF8, "application/json")

        //let! createEvResp2 = client.PostAsync (sprintf "%s/habits/%s/events" baseUrl habitId, eventContent2)
        ////// Заглушка: оба ответа createEvResp1/2 должны указывать на успешное создание (напр. 201)
        ////// Предположим, первый запрос возвращает ID созданного события:
        //let eventId = "<extract ID of first created event>"
        ////// 4. Удаляем одно событие
        //let! deleteEvResp = client.DeleteAsync (sprintf "%s/habits/%s/events/%s" baseUrl habitId eventId)
        ////// Заглушка: deleteEvResp.IsSuccessStatusCode (200 OK или 204 NoContent)
        ////// 5. Удаляем саму привычку
        //let! deleteHabResp = client.DeleteAsync (sprintf "%s/habits/%s" baseUrl habitId)
        ()
    //// Заглушка: deleteHabResp.IsSuccessStatusCode (привычка удалена успешно)
    }
