namespace TrackingService.Migrations

open FluentMigrator

[<Migration(20250804001L)>]
type AddDateColumnMigration() =
    inherit Migration()

    override _.Up() =
        base.Execute.Sql("""
ALTER TABLE "HabitEvents"
ADD COLUMN "Date" timestamp with time zone NOT NULL DEFAULT now();""")
        |> ignore

    override _.Down() =
        base.Delete.Column("Date").FromTable("HabitEvents")
        |> ignore
