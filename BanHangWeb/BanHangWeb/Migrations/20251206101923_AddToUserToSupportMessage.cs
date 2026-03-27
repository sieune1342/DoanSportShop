using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanHangWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddToUserToSupportMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ToUser",
                table: "SupportMessages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ToUser",
                table: "SupportMessages");
        }
    }
}
