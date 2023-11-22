using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmailClient.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_emails_filter",
                table: "emails");

            migrationBuilder.DropColumn(
                name: "filter",
                table: "emails");

            migrationBuilder.CreateTable(
                name: "filters",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_filters", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "EmailEntryFilter",
                columns: table => new
                {
                    EmailsId = table.Column<string>(type: "TEXT", nullable: false),
                    FiltersName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailEntryFilter", x => new { x.EmailsId, x.FiltersName });
                    table.ForeignKey(
                        name: "FK_EmailEntryFilter_emails_EmailsId",
                        column: x => x.EmailsId,
                        principalTable: "emails",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailEntryFilter_filters_FiltersName",
                        column: x => x.FiltersName,
                        principalTable: "filters",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailEntryFilter_FiltersName",
                table: "EmailEntryFilter",
                column: "FiltersName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailEntryFilter");

            migrationBuilder.DropTable(
                name: "filters");

            migrationBuilder.AddColumn<string>(
                name: "filter",
                table: "emails",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_emails_filter",
                table: "emails",
                column: "filter");
        }
    }
}
