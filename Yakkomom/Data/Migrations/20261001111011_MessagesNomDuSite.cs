using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yakkomom.Data.Migrations
{
    /// <inheritdoc />
    public partial class MessagesNomDuSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Messages WhatsApp : « Bonjour Yakkomom » devient « Bonjour {site} », remplacé au clic par le nom actuel du site.
            migrationBuilder.Sql("UPDATE parametres_site SET message_whats_app_terrain = replace(message_whats_app_terrain, 'Bonjour Yakkomom', 'Bonjour {site}');");
            migrationBuilder.Sql("UPDATE services SET message_whats_app = replace(message_whats_app, 'Bonjour Yakkomom', 'Bonjour {site}') WHERE message_whats_app IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE parametres_site SET message_whats_app_terrain = replace(message_whats_app_terrain, '{site}', 'Yakkomom');");
            migrationBuilder.Sql("UPDATE services SET message_whats_app = replace(message_whats_app, '{site}', 'Yakkomom') WHERE message_whats_app IS NOT NULL;");
        }
    }
}
