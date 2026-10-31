using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
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
        // Markings are stored as JSON blobs, so they are cast back to BLOB after the text replace
        private static void Rename(MigrationBuilder migrationBuilder, string from, string to)
        {
            migrationBuilder.Sql($"UPDATE profile SET species = replace(species, '{from}', '{to}') WHERE species LIKE '%{from}%';");
            migrationBuilder.Sql($"UPDATE profile SET organ_markings = CAST(replace(CAST(organ_markings AS TEXT), '{from}', '{to}') AS BLOB) WHERE CAST(organ_markings AS TEXT) LIKE '%{from}%';");
            migrationBuilder.Sql($"UPDATE profile SET markings = CAST(replace(CAST(markings AS TEXT), '{from}', '{to}') AS BLOB) WHERE CAST(markings AS TEXT) LIKE '%{from}%';");
        }
    }
}
