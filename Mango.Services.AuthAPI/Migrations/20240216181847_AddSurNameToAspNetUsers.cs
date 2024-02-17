using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mango.Services.AuthAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSurNameToAspNetUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SurName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SurName",
                table: "AspNetUsers");
        }
    }
}
