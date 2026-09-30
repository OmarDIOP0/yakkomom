using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yakkomom.Data.Migrations
{
    /// <inheritdoc />
    public partial class MediasTerrains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cle_vignette",
                table: "terrain_photos",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "id_envoi",
                table: "terrain_photos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type_mime",
                table: "terrain_photos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "id_envoi",
                table: "documents_fonciers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_terrain_photos_terrain_id_id_envoi",
                table: "terrain_photos",
                columns: new[] { "terrain_id", "id_envoi" },
                unique: true,
                filter: "id_envoi IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_documents_fonciers_terrain_id_id_envoi",
                table: "documents_fonciers",
                columns: new[] { "terrain_id", "id_envoi" },
                unique: true,
                filter: "id_envoi IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_terrain_photos_terrain_id_id_envoi",
                table: "terrain_photos");

            migrationBuilder.DropIndex(
                name: "ix_documents_fonciers_terrain_id_id_envoi",
                table: "documents_fonciers");

            migrationBuilder.DropColumn(
                name: "cle_vignette",
                table: "terrain_photos");

            migrationBuilder.DropColumn(
                name: "id_envoi",
                table: "terrain_photos");

            migrationBuilder.DropColumn(
                name: "type_mime",
                table: "terrain_photos");

            migrationBuilder.DropColumn(
                name: "id_envoi",
                table: "documents_fonciers");
        }
    }
}
