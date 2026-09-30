using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yakkomom.Data.Migrations
{
    /// <inheritdoc />
    public partial class Panoramas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "hauteur",
                table: "panoramas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "id_envoi",
                table: "panoramas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "largeur",
                table: "panoramas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "vaov",
                table: "panoramas",
                type: "double precision",
                nullable: false,
                defaultValue: 180.0);

            migrationBuilder.CreateIndex(
                name: "ix_panoramas_terrain_id_id_envoi",
                table: "panoramas",
                columns: new[] { "terrain_id", "id_envoi" },
                unique: true,
                filter: "id_envoi IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_panoramas_terrain_id_id_envoi",
                table: "panoramas");

            migrationBuilder.DropColumn(
                name: "hauteur",
                table: "panoramas");

            migrationBuilder.DropColumn(
                name: "id_envoi",
                table: "panoramas");

            migrationBuilder.DropColumn(
                name: "largeur",
                table: "panoramas");

            migrationBuilder.DropColumn(
                name: "vaov",
                table: "panoramas");
        }
    }
}
