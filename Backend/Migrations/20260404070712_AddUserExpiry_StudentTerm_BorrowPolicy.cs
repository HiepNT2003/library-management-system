using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUserExpiry_StudentTerm_BorrowPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.AddColumn<string>(
                name: "AspNetRoleId",
                table: "AspNetUsers",
                type: "varchar(255)",
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredDate",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);

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

            migrationBuilder.CreateTable(
                name: "BorrowPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AspNetRoleId = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    MaxBorrowDays = table.Column<int>(type: "int", nullable: false),
                    MaxBooks = table.Column<int>(type: "int", nullable: false),
                    MaxExtention = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BorrowPolicies_AspNetRole_AspNetRoleId",
                        column: x => x.AspNetRoleId,
                        principalTable: "AspNetRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BorrowPolicies_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AspNetRoleId",
                table: "AspNetUsers",
                column: "AspNetRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowPolicies_AspNetRoleId_DocumentTypeId",
                table: "BorrowPolicies",
                columns: new[] { "AspNetRoleId", "DocumentTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BorrowPolicies_DocumentTypeId",
                table: "BorrowPolicies",
                column: "DocumentTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetRole_AspNetRoleId",
                table: "AspNetUsers",
                column: "AspNetRoleId",
                principalTable: "AspNetRole",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetRole_AspNetRoleId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "BorrowPolicies");

            migrationBuilder.DropTable(
                name: "AspNetRole");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AspNetRoleId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AspNetRoleId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ExpiredDate",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_unicode_ci",
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
