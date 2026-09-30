using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Yakkomom.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "terrain_reference_seq");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    nom_complet = table.Column<string>(type: "text", nullable: true),
                    cree_le = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    derniere_connexion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "journal_actions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    utilisateur_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    utilisateur_nom = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entite_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entite_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    details_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journal_actions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "localites",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nom = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    slug = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    parent_id = table.Column<int>(type: "integer", nullable: true),
                    ordre = table.Column<int>(type: "integer", nullable: false),
                    est_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_localites", x => x.id);
                    table.ForeignKey(
                        name: "fk_localites_localites_parent_id",
                        column: x => x.parent_id,
                        principalTable: "localites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parametres_site",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    nom_site = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slogan = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    cle_logo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    whatsapp1 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    whatsapp1_libelle = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    whatsapp2 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    whatsapp2_libelle = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    whatsapp3 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    whatsapp3_libelle = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    message_whats_app_terrain = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    adresse = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    horaires_ouverture = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    facebook = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    instagram = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    tik_tok = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    you_tube = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    linked_in = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    modifie_le = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parametres_site", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "services",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    titre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    slug = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    resume = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    icone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    cle_image_couverture = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    message_whats_app = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ordre = table.Column<int>(type: "integer", nullable: false),
                    est_actif = table.Column<bool>(type: "boolean", nullable: false),
                    modifie_le = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_services", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_user_claims_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_asp_net_user_logins_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "terrains",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    titre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    statut = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    prix = table.Column<long>(type: "bigint", nullable: true),
                    prix_negociable = table.Column<bool>(type: "boolean", nullable: false),
                    surface_m2 = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    longueur_m = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    largeur_m = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    situation_fonciere = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    region_id = table.Column<int>(type: "integer", nullable: true),
                    departement_id = table.Column<int>(type: "integer", nullable: true),
                    commune_id = table.Column<int>(type: "integer", nullable: true),
                    quartier_village = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    adresse = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    contour_geo_json = table.Column<string>(type: "jsonb", nullable: true),
                    surface_calculee_m2 = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    acces_eau = table.Column<bool>(type: "boolean", nullable: true),
                    acces_electricite = table.Column<bool>(type: "boolean", nullable: true),
                    route_acces = table.Column<bool>(type: "boolean", nullable: true),
                    route_acces_detail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    video_youtube_url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    whatsapp1 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    whatsapp1_libelle = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    whatsapp2 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    whatsapp2_libelle = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    whatsapp3 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    whatsapp3_libelle = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    est_mis_en_avant = table.Column<bool>(type: "boolean", nullable: false),
                    ordre_mise_en_avant = table.Column<int>(type: "integer", nullable: false),
                    cree_le = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modifie_le = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    publie_le = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cree_par_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    modifie_par_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    commodites = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_terrains", x => x.id);
                    table.ForeignKey(
                        name: "fk_terrains_localites_commune_id",
                        column: x => x.commune_id,
                        principalTable: "localites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_terrains_localites_departement_id",
                        column: x => x.departement_id,
                        principalTable: "localites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_terrains_localites_region_id",
                        column: x => x.region_id,
                        principalTable: "localites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_photos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_id = table.Column<int>(type: "integer", nullable: false),
                    cle_stockage = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    largeur = table.Column<int>(type: "integer", nullable: false),
                    hauteur = table.Column<int>(type: "integer", nullable: false),
                    couleur_dominante = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    legende = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ordre = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_photos_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clics_whatsapp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    terrain_id = table.Column<int>(type: "integer", nullable: true),
                    service_id = table.Column<int>(type: "integer", nullable: true),
                    numero_index = table.Column<short>(type: "smallint", nullable: false),
                    source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clics_whatsapp", x => x.id);
                    table.ForeignKey(
                        name: "fk_clics_whatsapp_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_clics_whatsapp_terrains_terrain_id",
                        column: x => x.terrain_id,
                        principalTable: "terrains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "documents_fonciers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    terrain_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    titre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cle_stockage = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    nom_fichier_original = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    type_mime = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    taille_octets = table.Column<long>(type: "bigint", nullable: false),
                    est_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    cree_le = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cree_par_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documents_fonciers", x => x.id);
                    table.ForeignKey(
                        name: "fk_documents_fonciers_terrains_terrain_id",
                        column: x => x.terrain_id,
                        principalTable: "terrains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "panoramas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    terrain_id = table.Column<int>(type: "integer", nullable: false),
                    titre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    cle_stockage_hd = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    taille_octets_hd = table.Column<long>(type: "bigint", nullable: false),
                    cle_stockage_bd = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    taille_octets_bd = table.Column<long>(type: "bigint", nullable: false),
                    cle_vignette = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ordre = table.Column<int>(type: "integer", nullable: false),
                    est_depart = table.Column<bool>(type: "boolean", nullable: false),
                    yaw_initial = table.Column<double>(type: "double precision", nullable: false),
                    pitch_initial = table.Column<double>(type: "double precision", nullable: false),
                    hfov_initial = table.Column<double>(type: "double precision", nullable: false),
                    cree_le = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_panoramas", x => x.id);
                    table.ForeignKey(
                        name: "fk_panoramas_terrains_terrain_id",
                        column: x => x.terrain_id,
                        principalTable: "terrains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "terrain_photos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    terrain_id = table.Column<int>(type: "integer", nullable: false),
                    cle_stockage = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    largeur = table.Column<int>(type: "integer", nullable: false),
                    hauteur = table.Column<int>(type: "integer", nullable: false),
                    taille_octets = table.Column<long>(type: "bigint", nullable: false),
                    couleur_dominante = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    legende = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ordre = table.Column<int>(type: "integer", nullable: false),
                    est_couverture = table.Column<bool>(type: "boolean", nullable: false),
                    cree_le = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_terrain_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_terrain_photos_terrains_terrain_id",
                        column: x => x.terrain_id,
                        principalTable: "terrains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hotspots",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    panorama_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    panorama_cible_id = table.Column<int>(type: "integer", nullable: true),
                    pitch = table.Column<double>(type: "double precision", nullable: false),
                    yaw = table.Column<double>(type: "double precision", nullable: false),
                    texte = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hotspots", x => x.id);
                    table.ForeignKey(
                        name: "fk_hotspots_panoramas_panorama_cible_id",
                        column: x => x.panorama_cible_id,
                        principalTable: "panoramas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_hotspots_panoramas_panorama_id",
                        column: x => x.panorama_id,
                        principalTable: "panoramas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_role_claims_role_id",
                table: "AspNetRoleClaims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_claims_user_id",
                table: "AspNetUserClaims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_logins_user_id",
                table: "AspNetUserLogins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_roles_role_id",
                table: "AspNetUserRoles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_clics_whatsapp_date",
                table: "clics_whatsapp",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_clics_whatsapp_service_id",
                table: "clics_whatsapp",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_clics_whatsapp_terrain_id_date",
                table: "clics_whatsapp",
                columns: new[] { "terrain_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_documents_fonciers_terrain_id_type",
                table: "documents_fonciers",
                columns: new[] { "terrain_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_hotspots_panorama_cible_id",
                table: "hotspots",
                column: "panorama_cible_id");

            migrationBuilder.CreateIndex(
                name: "ix_hotspots_panorama_id",
                table: "hotspots",
                column: "panorama_id");

            migrationBuilder.CreateIndex(
                name: "ix_journal_actions_date",
                table: "journal_actions",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_journal_actions_entite_type_entite_id",
                table: "journal_actions",
                columns: new[] { "entite_type", "entite_id" });

            migrationBuilder.CreateIndex(
                name: "ix_localites_parent_id",
                table: "localites",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_localites_type_parent_id_slug",
                table: "localites",
                columns: new[] { "type", "parent_id", "slug" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_panoramas_depart_unique",
                table: "panoramas",
                column: "terrain_id",
                unique: true,
                filter: "est_depart");

            migrationBuilder.CreateIndex(
                name: "ix_panoramas_terrain_id_ordre",
                table: "panoramas",
                columns: new[] { "terrain_id", "ordre" });

            migrationBuilder.CreateIndex(
                name: "ix_service_photos_service_id_ordre",
                table: "service_photos",
                columns: new[] { "service_id", "ordre" });

            migrationBuilder.CreateIndex(
                name: "ix_services_ordre",
                table: "services",
                column: "ordre");

            migrationBuilder.CreateIndex(
                name: "ix_services_slug",
                table: "services",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_terrain_photos_couverture_unique",
                table: "terrain_photos",
                column: "terrain_id",
                unique: true,
                filter: "est_couverture");

            migrationBuilder.CreateIndex(
                name: "ix_terrain_photos_terrain_id_ordre",
                table: "terrain_photos",
                columns: new[] { "terrain_id", "ordre" });

            migrationBuilder.CreateIndex(
                name: "ix_terrains_commune_id",
                table: "terrains",
                column: "commune_id");

            migrationBuilder.CreateIndex(
                name: "ix_terrains_departement_id",
                table: "terrains",
                column: "departement_id");

            migrationBuilder.CreateIndex(
                name: "ix_terrains_est_mis_en_avant_ordre_mise_en_avant",
                table: "terrains",
                columns: new[] { "est_mis_en_avant", "ordre_mise_en_avant" });

            migrationBuilder.CreateIndex(
                name: "ix_terrains_prix",
                table: "terrains",
                column: "prix");

            migrationBuilder.CreateIndex(
                name: "ix_terrains_publie_le",
                table: "terrains",
                column: "publie_le");

            migrationBuilder.CreateIndex(
                name: "ix_terrains_reference",
                table: "terrains",
                column: "reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_terrains_region_id",
                table: "terrains",
                column: "region_id");

            migrationBuilder.CreateIndex(
                name: "ix_terrains_statut",
                table: "terrains",
                column: "statut");

            migrationBuilder.CreateIndex(
                name: "ix_terrains_surface_m2",
                table: "terrains",
                column: "surface_m2");

            migrationBuilder.CreateIndex(
                name: "ix_terrains_type",
                table: "terrains",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "clics_whatsapp");

            migrationBuilder.DropTable(
                name: "documents_fonciers");

            migrationBuilder.DropTable(
                name: "hotspots");

            migrationBuilder.DropTable(
                name: "journal_actions");

            migrationBuilder.DropTable(
                name: "parametres_site");

            migrationBuilder.DropTable(
                name: "service_photos");

            migrationBuilder.DropTable(
                name: "terrain_photos");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "panoramas");

            migrationBuilder.DropTable(
                name: "services");

            migrationBuilder.DropTable(
                name: "terrains");

            migrationBuilder.DropTable(
                name: "localites");

            migrationBuilder.DropSequence(
                name: "terrain_reference_seq");
        }
    }
}
