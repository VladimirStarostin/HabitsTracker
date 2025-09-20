namespace TrackingService.Migrations

open FluentMigrator

[<Migration(20250728001L)>]
type CreateHabitEventsTableMigration() =
    inherit Migration()

    override _.Up() =
        base.Create
            .Table("HabitEvents").WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("HabitId").AsInt32().NotNullable().Indexed()
            .WithColumn("UserId").AsInt32().NotNullable().Indexed()
        |> ignore

    override _.Down (): unit = 
        base.Delete.Table("HabitEvents")
        |> ignore
