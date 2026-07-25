using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace store.Migrations
{
    /// <inheritdoc />
    public partial class fixspelling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RecieverId",
                table: "ChatMessages",
                newName: "ReceiverId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReceiverId",
                table: "ChatMessages",
                newName: "RecieverId");
        }
    }
}
