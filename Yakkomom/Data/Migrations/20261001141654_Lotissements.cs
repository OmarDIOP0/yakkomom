using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Yakkomom.Data.Migrations
{
    /// <inheritdoc />
    public partial class Lotissements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "prix_m2max",
                table: "terrains",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "prix_m2min",
                table: "terrains",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "lots",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    terrain_id = table.Column<int>(type: "integer", nullable: false),
                    numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    surface_m2 = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    prix_m2 = table.Column<long>(type: "bigint", nullable: true),
                    prix = table.Column<long>(type: "bigint", nullable: true),
                    position = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    statut = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ordre = table.Column<int>(type: "integer", nullable: false),
                    modifie_le = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lots", x => x.id);
                    table.ForeignKey(
                        name: "fk_lots_terrains_terrain_id",
                        column: x => x.terrain_id,
                        principalTable: "terrains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_lots_terrain_id_numero",
                table: "lots",
                columns: new[] { "terrain_id", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lots_terrain_id_statut",
                table: "lots",
                columns: new[] { "terrain_id", "statut" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lots");

            migrationBuilder.DropColumn(
                name: "prix_m2max",
                table: "terrains");

            migrationBuilder.DropColumn(
                name: "prix_m2min",
                table: "terrains");
        }
    }
}
