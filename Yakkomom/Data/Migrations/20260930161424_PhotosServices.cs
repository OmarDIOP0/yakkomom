using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yakkomom.Data.Migrations
{
    /// <inheritdoc />
    public partial class PhotosServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cle_vignette",
                table: "service_photos",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "id_envoi",
                table: "service_photos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "taille_octets",
                table: "service_photos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "type_mime",
                table: "service_photos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_service_photos_service_id_id_envoi",
                table: "service_photos",
                columns: new[] { "service_id", "id_envoi" },
                unique: true,
                filter: "id_envoi IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_service_photos_service_id_id_envoi",
                table: "service_photos");

            migrationBuilder.DropColumn(
                name: "cle_vignette",
                table: "service_photos");

            migrationBuilder.DropColumn(
                name: "id_envoi",
                table: "service_photos");

            migrationBuilder.DropColumn(
                name: "taille_octets",
                table: "service_photos");

            migrationBuilder.DropColumn(
                name: "type_mime",
                table: "service_photos");
        }
    }
}
