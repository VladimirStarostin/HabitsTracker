namespace HabitsTracker.Helpers.Pipelines

open System.Threading.Tasks

type PipelineResult<'Ok, 'Err> =
    | Ok of 'Ok
    | Error of 'Err

type PipelineBuilder () =
    member _.Bind (wrappedValue: PipelineResult<'a, 'e>, func: 'a -> PipelineResult<'b, 'e>) : PipelineResult<'b, 'e> =
        match wrappedValue with
        | Ok value -> func value
        | Error err -> Error err

    member _.Bind
        (wrappedValue: PipelineResult<'a, 'e>, func: 'a -> Task<PipelineResult<'b, 'e>>)
        : Task<PipelineResult<'b, 'e>> =
        task {
            match wrappedValue with
            | Ok value -> return! func value
            | Error err -> return Error err
        }

    member _.Bind
        (wrappedValueTask: Task<PipelineResult<'a, 'e>>, func: 'a -> Task<PipelineResult<'b, 'e>>)
        : Task<PipelineResult<'b, 'e>> =
        task {
            let! wrappedValue = wrappedValueTask

            match wrappedValue with
            | Ok value -> return! func value
            | Error err -> return Error err
        }

    member _.Bind
        (wrappedValueTask: Task<PipelineResult<'a, 'e>>, func: 'a -> PipelineResult<'b, 'e>)
        : Task<PipelineResult<'b, 'e>> =
        task {
            let! wrappedValue = wrappedValueTask

            match wrappedValue with
            | Ok value -> return func value
            | Error err -> return Error err
        }

    member _.Return (value: 'b) : PipelineResult<'b, 'e> = Ok value
    member _.ReturnFrom (value: Task<PipelineResult<'b, 'e>>) : Task<PipelineResult<'b, 'e>> = value
