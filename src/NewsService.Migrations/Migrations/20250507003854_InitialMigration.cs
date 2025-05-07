using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsService.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "news",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_news", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "news_documents_file",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    files_extension = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    bucket = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    file_path = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "varchar(24)", nullable: false),
                    error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    news_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_news_documents_file", x => x.id);
                    table.ForeignKey(
                        name: "FK_news_documents_file_news_news_id",
                        column: x => x.news_id,
                        principalTable: "news",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "news_images_file",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    files_extension = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    bucket = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    file_path = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "varchar(24)", nullable: false),
                    error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    news_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_news_images_file", x => x.id);
                    table.ForeignKey(
                        name: "FK_news_images_file_news_news_id",
                        column: x => x.news_id,
                        principalTable: "news",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "news_videos_file",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    files_extension = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    bucket = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    file_path = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "varchar(24)", nullable: false),
                    error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    news_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_news_videos_file", x => x.id);
                    table.ForeignKey(
                        name: "FK_news_videos_file_news_news_id",
                        column: x => x.news_id,
                        principalTable: "news",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_news_documents_file_news_id",
                table: "news_documents_file",
                column: "news_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_news_images_file_news_id",
                table: "news_images_file",
                column: "news_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_news_videos_file_news_id",
                table: "news_videos_file",
                column: "news_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "news_documents_file");

            migrationBuilder.DropTable(
                name: "news_images_file");

            migrationBuilder.DropTable(
                name: "news_videos_file");

            migrationBuilder.DropTable(
                name: "news");
        }
    }
}
