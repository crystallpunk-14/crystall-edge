using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class PlayerAchievements : Migration
    {
        // CrystallEdge: the achievements system originally lived in the crystallpunk-14/crystalledge
        // repo lineage, where migration 20260217112542_PlayerAchievements already created this exact
        // table. Production databases migrated from that lineage still physically contain
        // "player_achievement" (identical schema), but this repo's migration history has no record of
        // it. A plain CreateTable would therefore crash on deploy with "table already exists".
        // The Up() below is idempotent (CREATE TABLE / INDEX IF NOT EXISTS) so it is a no-op on such
        // databases and simply records the migration in __EFMigrationsHistory, while still creating
        // the table normally on fresh databases (dev SQLite, new servers).

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""player_achievement"" (
                    ""player_achievement_id"" INTEGER NOT NULL CONSTRAINT ""PK_player_achievement"" PRIMARY KEY AUTOINCREMENT,
                    ""player_user_id"" TEXT NOT NULL,
                    ""proto_id"" TEXT NOT NULL,
                    CONSTRAINT ""FK_player_achievement_player_player_user_id"" FOREIGN KEY (""player_user_id"") REFERENCES ""player"" (""user_id"") ON DELETE CASCADE
                );
            ");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_player_achievement_player_user_id_proto_id""
                    ON ""player_achievement"" (""player_user_id"", ""proto_id"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_achievement");
        }
    }
}
