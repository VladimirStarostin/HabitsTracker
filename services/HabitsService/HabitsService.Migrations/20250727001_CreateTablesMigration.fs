namespace HabitsService.Migrations

open FluentMigrator

[<Migration(20250727001L)>]
type CreateTablesMigration() =
    inherit AutoReversingMigration()

    override _.Up() =
        base.Create.Table("Habits")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("OwnerUserId").AsInt32().Indexed()
            .WithColumn("Name").AsString(512).NotNullable()
            .WithColumn("Description").AsString(2048).Nullable()
        |> ignore
