[<RequireQualifiedAccessAttribute>]
module HabitsTracker.Helpers.Task

open System.Threading.Tasks

let mapAsync (mapper: 'a -> Task<'b>) (comp: Task<'a>) : Task<'b> =
    task {
        let! result = comp
        return! mapper result
    }

let map (mapper: 'a -> 'b) (comp: Task<'a>) : Task<'b> =
    task {
        let! result = comp
        return mapper result
    }
