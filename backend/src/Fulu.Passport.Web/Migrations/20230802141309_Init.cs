using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fulu.Passport.Web.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "client",
                columns: table => new
                {
                    client_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<string>(type: "text", nullable: true),
                    full_name = table.Column<string>(type: "text", nullable: true),
                    client_secret = table.Column<string>(type: "text", nullable: true),
                    host_url = table.Column<string>(type: "text", nullable: true),
                    redirect_uri = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client", x => x.client_id);
                });

            migrationBuilder.CreateTable(
                name: "external_user",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<string>(type: "text", nullable: true),
                    provider_key = table.Column<string>(type: "text", nullable: true),
                    login_provider = table.Column<string>(type: "text", nullable: true),
                    client_id = table.Column<int>(type: "integer", nullable: false),
                    nickname = table.Column<string>(type: "text", nullable: true),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "operator_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    client_id = table.Column<int>(type: "integer", nullable: false),
                    userid = table.Column<string>(type: "text", nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    ip = table.Column<string>(type: "text", nullable: true),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operator_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sms_record",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    client_id = table.Column<int>(type: "integer", nullable: false),
                    send_type = table.Column<string>(type: "text", nullable: true),
                    send_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    send_result = table.Column<bool>(type: "boolean", nullable: false),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    receiver = table.Column<string>(type: "text", nullable: true),
                    remark = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sms_record", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    username = table.Column<string>(type: "text", nullable: true),
                    nickname = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    gender = table.Column<int>(type: "integer", nullable: false),
                    figure_url = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    password = table.Column<string>(type: "text", nullable: true),
                    birthday = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    salt = table.Column<string>(type: "text", nullable: true),
                    locked = table.Column<bool>(type: "boolean", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_ip = table.Column<string>(type: "text", nullable: true),
                    last_login_address = table.Column<string>(type: "text", nullable: true),
                    login_count = table.Column<int>(type: "integer", nullable: false),
                    login_error_count = table.Column<int>(type: "integer", nullable: false),
                    last_try_login_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    register_client_id = table.Column<int>(type: "integer", nullable: false),
                    register_client_name = table.Column<string>(type: "text", nullable: true),
                    register_ip = table.Column<string>(type: "text", nullable: true),
                    register_address = table.Column<string>(type: "text", nullable: true),
                    register_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    enabled_two_factor = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "client",
                columns: new[] { "client_id", "client_secret", "description", "enabled", "full_name", "host_url", "id", "redirect_uri" },
                values: new object[] { 10000001, "14p9ao1gxu4q3sp8ogk8bq4gkct59t9w", null, true, "葫芦藤", "http://localhost:8080", "d75de7334961432badbb0859fa5615d7", "http://localhost:8080" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client");

            migrationBuilder.DropTable(
                name: "external_user");

            migrationBuilder.DropTable(
                name: "operator_log");

            migrationBuilder.DropTable(
                name: "sms_record");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
