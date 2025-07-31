namespace TrackingService.Migrations

open FluentMigrator

[<Migration(20250728001L)>]
type CreateTablesMigration() =
    inherit AutoReversingMigration()

    override _.Up() =
        base.Create
            .Table("HabitEvents")
            .WithColumn("Id")
            .AsInt32()
            .PrimaryKey()
            .Identity()
            .WithColumn("HabitId")
            .AsInt32()
            .NotNullable()
            .WithColumn("UserId")
            .AsInt32()
            .NotNullable()
            .Indexed()
            .WithColumn("Date")
            .AsDateTime()
            .NotNullable()
        |> ignore
