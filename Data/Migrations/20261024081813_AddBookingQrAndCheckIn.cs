using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeArtGallery.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingQrAndCheckIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedInAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckedInByUserId",
                table: "Bookings",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrToken",
                table: "Bookings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CheckedInByUserId",
                table: "Bookings",
                column: "CheckedInByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_QrToken",
                table: "Bookings",
                column: "QrToken",
                unique: true,
                filter: "[QrToken] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_CheckedInByUserId",
                table: "Bookings",
                column: "CheckedInByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_CheckedInByUserId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CheckedInByUserId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_QrToken",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CheckedInByUserId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "QrToken",
                table: "Bookings");
        }
    }
}
