namespace UserService.Migrations

open FluentMigrator

[<Migration(20250727001L)>]
type CreateTablesMigration() =
    inherit Migration()

    override _.Up() =
        base.Create.Table("Users")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Email").AsString(255).NotNullable().Unique()
            .WithColumn("Name").AsString(512).NotNullable()
            .WithColumn("PasswordHash").AsString(200).NotNullable()
        |> ignore

    override _.Down() = base.Delete.Table("Users") |> ignore
