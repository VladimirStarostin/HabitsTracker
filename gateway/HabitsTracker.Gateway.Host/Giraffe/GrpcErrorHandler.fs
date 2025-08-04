module HabitsTracker.Gateway.Host.Giraffe.GrpcErrorHandler

open Grpc.Core
open Giraffe
open Microsoft.Extensions.Logging

let private httpStatusOf (ex: RpcException) =
    match ex.StatusCode with
    | StatusCode.InvalidArgument -> 400
    | StatusCode.Unauthenticated -> 401
    | StatusCode.PermissionDenied -> 403
    | StatusCode.NotFound -> 404
    | StatusCode.AlreadyExists -> 409
    | StatusCode.FailedPrecondition -> 412
    | StatusCode.OutOfRange -> 400
    | StatusCode.ResourceExhausted -> 429
    | StatusCode.DeadlineExceeded -> 504
    | StatusCode.Unavailable -> 503
    | _ -> 500

let private problem (status: int) (title: string) (detail: string) : HttpHandler =
    let body =
        {| status = status
           title = title
           detail = detail |}

    setStatusCode status >=> json body

let errorHandler (ex: System.Exception) (logger: ILogger) : HttpHandler =
    match ex with
    | :? RpcException as rex ->
        logger.LogWarning (ex, "gRPC error: {Status} {Detail}", rex.StatusCode, rex.Status.Detail)
        problem (httpStatusOf rex) (rex.StatusCode.ToString ()) rex.Status.Detail
    | _ ->
        logger.LogError (ex, "Unhandled exception")
        problem 500 "InternalServerError" "Unhandled server error"
