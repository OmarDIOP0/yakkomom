using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yakkomom.Data.Migrations
{
    /// <inheritdoc />
    public partial class InformationsLegales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "forme_juridique",
                table: "parametres_site",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ninea",
                table: "parametres_site",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "raison_sociale",
                table: "parametres_site",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rccm",
                table: "parametres_site",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "responsable_publication",
                table: "parametres_site",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "forme_juridique",
                table: "parametres_site");

            migrationBuilder.DropColumn(
                name: "ninea",
                table: "parametres_site");

            migrationBuilder.DropColumn(
                name: "raison_sociale",
                table: "parametres_site");

            migrationBuilder.DropColumn(
                name: "rccm",
                table: "parametres_site");

            migrationBuilder.DropColumn(
                name: "responsable_publication",
                table: "parametres_site");
        }
    }
}
