using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsAppParser.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductOriginFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginFlag",
                table: "Products",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginFlag",
                table: "Products");
        }
    }
}
