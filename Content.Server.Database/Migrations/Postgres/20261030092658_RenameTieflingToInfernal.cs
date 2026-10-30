using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RenameTieflingToInfernal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            Rename(migrationBuilder, "Tiefling", "Infernal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            Rename(migrationBuilder, "Infernal", "Tiefling");
        }

        // Species, organ and marking prototype IDs (CETiefling, CEOrganTiefling*, CETieflingHorns*) were renamed
        private static void Rename(MigrationBuilder migrationBuilder, string from, string to)
        {
            migrationBuilder.Sql($"UPDATE profile SET species = replace(species, '{from}', '{to}') WHERE species LIKE '%{from}%';");
            migrationBuilder.Sql($"UPDATE profile SET organ_markings = replace(organ_markings::text, '{from}', '{to}')::jsonb WHERE organ_markings::text LIKE '%{from}%';");
            migrationBuilder.Sql($"UPDATE profile SET markings = replace(markings::text, '{from}', '{to}')::jsonb WHERE markings::text LIKE '%{from}%';");
        }
    }
}
