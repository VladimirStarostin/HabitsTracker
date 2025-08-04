[<RequireQualifiedAccessAttribute>]
module HabitsTracker.Helpers.Dapper

open System
open System.Data

open Dapper

type private DateTimeOffsetHandler () =
    inherit SqlMapper.TypeHandler<DateTimeOffset> ()

    override _.SetValue (parameter: IDbDataParameter, value: DateTimeOffset) =
        parameter.Value <- value.ToUniversalTime ()

    override _.Parse (value: obj) : DateTimeOffset =
        match value with
        | :? DateTime as dateTime -> DateTimeOffset (dateTime, TimeSpan.Zero)
        | _ -> failwithf $"Cannot convert {value} to DateTimeOffset"

let addRequiredTypeHandlers () =
    DateTimeOffsetHandler ()
    |> SqlMapper.AddTypeHandler
