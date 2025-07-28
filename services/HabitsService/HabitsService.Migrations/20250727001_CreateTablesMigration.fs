namespace HabitsService.Migrations

open FluentMigrator

[<Migration(20250727001L)>]
type CreateTablesMigration() =
    inherit Migration()

    override _.Up() =
        base.Create
            .Table("Habits")
            .WithColumn("Id")
            .AsInt32()
            .PrimaryKey()
            .Identity()
            .WithColumn("Name")
            .AsString(512)
            .NotNullable()
        |> ignore

    override _.Down() = base.Delete.Table("Habits") |> ignore
