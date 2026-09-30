using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yakkomom.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenommageTablesIdentite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_claims_asp_net_users_user_id",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_logins_asp_net_users_user_id",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_roles_asp_net_users_user_id",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_tokens",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_users",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_roles",
                table: "AspNetUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_logins",
                table: "AspNetUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_claims",
                table: "AspNetUserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_roles",
                table: "AspNetRoles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_role_claims",
                table: "AspNetRoleClaims");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "utilisateur_jetons");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "utilisateurs");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "utilisateur_roles");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "utilisateur_connexions_externes");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "utilisateur_claims");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "roles");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "role_claims");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_roles_role_id",
                table: "utilisateur_roles",
                newName: "ix_utilisateur_roles_role_id");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_logins_user_id",
                table: "utilisateur_connexions_externes",
                newName: "ix_utilisateur_connexions_externes_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_claims_user_id",
                table: "utilisateur_claims",
                newName: "ix_utilisateur_claims_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_role_claims_role_id",
                table: "role_claims",
                newName: "ix_role_claims_role_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_utilisateur_jetons",
                table: "utilisateur_jetons",
                columns: new[] { "user_id", "login_provider", "name" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_utilisateurs",
                table: "utilisateurs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_utilisateur_roles",
                table: "utilisateur_roles",
                columns: new[] { "user_id", "role_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_utilisateur_connexions_externes",
                table: "utilisateur_connexions_externes",
                columns: new[] { "login_provider", "provider_key" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_utilisateur_claims",
                table: "utilisateur_claims",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_roles",
                table: "roles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_role_claims",
                table: "role_claims",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_role_claims_roles_role_id",
                table: "role_claims",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_utilisateur_claims_utilisateurs_user_id",
                table: "utilisateur_claims",
                column: "user_id",
                principalTable: "utilisateurs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_utilisateur_connexions_externes_utilisateurs_user_id",
                table: "utilisateur_connexions_externes",
                column: "user_id",
                principalTable: "utilisateurs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_utilisateur_jetons_utilisateurs_user_id",
                table: "utilisateur_jetons",
                column: "user_id",
                principalTable: "utilisateurs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_utilisateur_roles_roles_role_id",
                table: "utilisateur_roles",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_utilisateur_roles_utilisateurs_user_id",
                table: "utilisateur_roles",
                column: "user_id",
                principalTable: "utilisateurs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_role_claims_roles_role_id",
                table: "role_claims");

            migrationBuilder.DropForeignKey(
                name: "fk_utilisateur_claims_utilisateurs_user_id",
                table: "utilisateur_claims");

            migrationBuilder.DropForeignKey(
                name: "fk_utilisateur_connexions_externes_utilisateurs_user_id",
                table: "utilisateur_connexions_externes");

            migrationBuilder.DropForeignKey(
                name: "fk_utilisateur_jetons_utilisateurs_user_id",
                table: "utilisateur_jetons");

            migrationBuilder.DropForeignKey(
                name: "fk_utilisateur_roles_roles_role_id",
                table: "utilisateur_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_utilisateur_roles_utilisateurs_user_id",
                table: "utilisateur_roles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_utilisateurs",
                table: "utilisateurs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_utilisateur_roles",
                table: "utilisateur_roles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_utilisateur_jetons",
                table: "utilisateur_jetons");

            migrationBuilder.DropPrimaryKey(
                name: "pk_utilisateur_connexions_externes",
                table: "utilisateur_connexions_externes");

            migrationBuilder.DropPrimaryKey(
                name: "pk_utilisateur_claims",
                table: "utilisateur_claims");

            migrationBuilder.DropPrimaryKey(
                name: "pk_roles",
                table: "roles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_role_claims",
                table: "role_claims");

            migrationBuilder.RenameTable(
                name: "utilisateurs",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "utilisateur_roles",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "utilisateur_jetons",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "utilisateur_connexions_externes",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "utilisateur_claims",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "roles",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "role_claims",
                newName: "AspNetRoleClaims");

            migrationBuilder.RenameIndex(
                name: "ix_utilisateur_roles_role_id",
                table: "AspNetUserRoles",
                newName: "ix_asp_net_user_roles_role_id");

            migrationBuilder.RenameIndex(
                name: "ix_utilisateur_connexions_externes_user_id",
                table: "AspNetUserLogins",
                newName: "ix_asp_net_user_logins_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_utilisateur_claims_user_id",
                table: "AspNetUserClaims",
                newName: "ix_asp_net_user_claims_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_role_claims_role_id",
                table: "AspNetRoleClaims",
                newName: "ix_asp_net_role_claims_role_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_users",
                table: "AspNetUsers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_roles",
                table: "AspNetUserRoles",
                columns: new[] { "user_id", "role_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_tokens",
                table: "AspNetUserTokens",
                columns: new[] { "user_id", "login_provider", "name" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_logins",
                table: "AspNetUserLogins",
                columns: new[] { "login_provider", "provider_key" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_claims",
                table: "AspNetUserClaims",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_roles",
                table: "AspNetRoles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_role_claims",
                table: "AspNetRoleClaims",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                table: "AspNetRoleClaims",
                column: "role_id",
                principalTable: "AspNetRoles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_claims_asp_net_users_user_id",
                table: "AspNetUserClaims",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_logins_asp_net_users_user_id",
                table: "AspNetUserLogins",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                table: "AspNetUserRoles",
                column: "role_id",
                principalTable: "AspNetRoles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_roles_asp_net_users_user_id",
                table: "AspNetUserRoles",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                table: "AspNetUserTokens",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
