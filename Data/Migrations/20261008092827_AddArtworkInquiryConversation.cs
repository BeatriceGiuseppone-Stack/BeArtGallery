using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeArtGallery.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddArtworkInquiryConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArtworkInquiryMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArtworkInquiryId = table.Column<int>(type: "int", nullable: false),
                    SenderUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SenderType = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtworkInquiryMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArtworkInquiryMessages_ArtworkInquiries_ArtworkInquiryId",
                        column: x => x.ArtworkInquiryId,
                        principalTable: "ArtworkInquiries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArtworkInquiryMessages_AspNetUsers_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArtworkInquiryMessages_ArtworkInquiryId_SenderType_ReadAt",
                table: "ArtworkInquiryMessages",
                columns: new[] { "ArtworkInquiryId", "SenderType", "ReadAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ArtworkInquiryMessages_SenderUserId",
                table: "ArtworkInquiryMessages",
                column: "SenderUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtworkInquiryMessages");
        }
    }
}
