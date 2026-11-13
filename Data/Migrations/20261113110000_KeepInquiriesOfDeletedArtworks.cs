using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeArtGallery.Data.Migrations
{
    /// <summary>
    /// Le richieste di informazioni sopravvivono all'eliminazione
    /// dell'opera: ArtworkId diventa facoltativo (SET NULL) e la
    /// richiesta conserva una copia del titolo e dell'artista.
    /// </summary>
    public partial class KeepInquiriesOfDeletedArtworks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArtworkInquiries_Artworks_ArtworkId",
                table: "ArtworkInquiries");

            // SQL Server non permette di cambiare una colonna indicizzata:
            // l'indice viene tolto e ricreato.
            migrationBuilder.Sql(
                "DROP INDEX [IX_ArtworkInquiries_ArtworkId] ON [ArtworkInquiries];");

            migrationBuilder.Sql(
                "ALTER TABLE [ArtworkInquiries] ALTER COLUMN [ArtworkId] int NULL;");

            migrationBuilder.Sql(
                "CREATE INDEX [IX_ArtworkInquiries_ArtworkId] ON [ArtworkInquiries] ([ArtworkId]);");

            migrationBuilder.AddColumn<string>(
                name: "ArtistName",
                table: "ArtworkInquiries",
                type: "nvarchar(201)",
                maxLength: 201,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArtworkTitle",
                table: "ArtworkInquiries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            // Copia titolo e artista nelle richieste già esistenti.
            migrationBuilder.Sql(@"
UPDATE i
SET i.[ArtworkTitle] = a.[Title],
    i.[ArtistName] = LEFT(ar.[FirstName] + N' ' + ar.[LastName], 201)
FROM [ArtworkInquiries] AS i
INNER JOIN [Artworks] AS a ON a.[Id] = i.[ArtworkId]
LEFT JOIN [Artists] AS ar ON ar.[Id] = a.[ArtistId];");

            migrationBuilder.AddForeignKey(
                name: "FK_ArtworkInquiries_Artworks_ArtworkId",
                table: "ArtworkInquiries",
                column: "ArtworkId",
                principalTable: "Artworks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArtworkInquiries_Artworks_ArtworkId",
                table: "ArtworkInquiries");

            // Le richieste di opere eliminate non possono restare
            // senza opera nello schema precedente.
            migrationBuilder.Sql(
                "DELETE FROM [ArtworkInquiries] WHERE [ArtworkId] IS NULL;");

            migrationBuilder.DropColumn(
                name: "ArtistName",
                table: "ArtworkInquiries");

            migrationBuilder.DropColumn(
                name: "ArtworkTitle",
                table: "ArtworkInquiries");

            migrationBuilder.Sql(
                "DROP INDEX [IX_ArtworkInquiries_ArtworkId] ON [ArtworkInquiries];");

            migrationBuilder.Sql(
                "ALTER TABLE [ArtworkInquiries] ALTER COLUMN [ArtworkId] int NOT NULL;");

            migrationBuilder.Sql(
                "CREATE INDEX [IX_ArtworkInquiries_ArtworkId] ON [ArtworkInquiries] ([ArtworkId]);");

            migrationBuilder.AddForeignKey(
                name: "FK_ArtworkInquiries_Artworks_ArtworkId",
                table: "ArtworkInquiries",
                column: "ArtworkId",
                principalTable: "Artworks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
