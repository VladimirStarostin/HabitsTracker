namespace UserService.Migrations

open FluentMigrator

[<Migration(20250730001L)>]
type CreateRefreshTokensTableMigration() =
    inherit AutoReversingMigration()

    override _.Up() =
        base.Create
            .Table("RefreshTokens").WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("Token").AsString(200).NotNullable().Unique()
            .WithColumn("UserId").AsInt64().NotNullable()
            .ForeignKey("FK_RefreshTokens_Users", "Users", "Id").WithColumn("ExpiresAt").AsDateTime().NotNullable()
            .WithColumn("IsRevoked").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
        |> ignore
