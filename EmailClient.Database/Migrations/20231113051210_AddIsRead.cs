using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmailClient.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddIsRead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_read",
                table: "emails",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_emails_filter",
                table: "emails",
                column: "filter");

            migrationBuilder.CreateIndex(
                name: "IX_emails_is_read",
                table: "emails",
                column: "is_read");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_emails_filter",
                table: "emails");

            migrationBuilder.DropIndex(
                name: "IX_emails_is_read",
                table: "emails");

            migrationBuilder.DropColumn(
                name: "is_read",
                table: "emails");
        }
    }
}
