using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class changeFKBorrowPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetRole_AspNetRoleId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_BorrowPolicies_AspNetRole_AspNetRoleId",
                table: "BorrowPolicies");

            migrationBuilder.DropTable(
                name: "AspNetRole");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AspNetRoleId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AllowBorrow",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "AspNetRoleId",
                table: "AspNetUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_BorrowPolicies_AspNetRoles_AspNetRoleId",
                table: "BorrowPolicies",
                column: "AspNetRoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BorrowPolicies_AspNetRoles_AspNetRoleId",
                table: "BorrowPolicies");

            migrationBuilder.AddColumn<bool>(
                name: "AllowBorrow",
                table: "Warehouses",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AspNetRoleId",
                table: "AspNetUsers",
                type: "varchar(255)",
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetRole",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRole", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AspNetRoleId",
                table: "AspNetUsers",
                column: "AspNetRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetRole_AspNetRoleId",
                table: "AspNetUsers",
                column: "AspNetRoleId",
                principalTable: "AspNetRole",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BorrowPolicies_AspNetRole_AspNetRoleId",
                table: "BorrowPolicies",
                column: "AspNetRoleId",
                principalTable: "AspNetRole",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
