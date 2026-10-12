using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeArtGallery.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompleteArtworkFavoritesAndInquiries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArtworkFavorites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArtworkId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtworkFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArtworkFavorites_Artworks_ArtworkId",
                        column: x => x.ArtworkId,
                        principalTable: "Artworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArtworkFavorites_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArtworkInquiries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArtworkId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtworkInquiries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArtworkInquiries_Artworks_ArtworkId",
                        column: x => x.ArtworkId,
                        principalTable: "Artworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArtworkInquiries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArtworkFavorites_ArtworkId",
                table: "ArtworkFavorites",
                column: "ArtworkId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtworkFavorites_UserId_ArtworkId",
                table: "ArtworkFavorites",
                columns: new[] { "UserId", "ArtworkId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArtworkInquiries_ArtworkId",
                table: "ArtworkInquiries",
                column: "ArtworkId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtworkInquiries_UserId",
                table: "ArtworkInquiries",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtworkFavorites");

            migrationBuilder.DropTable(
                name: "ArtworkInquiries");
        }
    }
}
