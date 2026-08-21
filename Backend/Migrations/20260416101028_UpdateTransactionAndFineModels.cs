using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTransactionAndFineModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "Borrowed/Returned/Overdue/Cancelled",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldDefaultValueSql: "'Borrowed'",
                oldComment: "Borrowed/Returned/Overdue/Cancelled")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.AddColumn<int>(
                name: "RequestId",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnCondition",
                table: "Transactions",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ReturnLibrarianId",
                table: "Transactions",
                type: "varchar(255)",
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Fines",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedBorrowDate",
                table: "BorrowRequests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectedReason",
                table: "BorrowRequests",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_RequestId",
                table: "Transactions",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ReturnLibrarianId",
                table: "Transactions",
                column: "ReturnLibrarianId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_AspNetUsers_ReturnLibrarianId",
                table: "Transactions",
                column: "ReturnLibrarianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_BorrowRequests_RequestId",
                table: "Transactions",
                column: "RequestId",
                principalTable: "BorrowRequests",
                principalColumn: "RequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_AspNetUsers_ReturnLibrarianId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_BorrowRequests_RequestId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_RequestId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ReturnLibrarianId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ReturnCondition",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ReturnLibrarianId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Fines");

            migrationBuilder.DropColumn(
                name: "ExpectedBorrowDate",
                table: "BorrowRequests");

            migrationBuilder.DropColumn(
                name: "RejectedReason",
                table: "BorrowRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Transactions",
                type: "varchar(20)",
                nullable: true,
                defaultValue: 0,
                comment: "Borrowed/Returned/Overdue/Cancelled",
                collation: "utf8mb4_unicode_ci",
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 20,
                oldDefaultValueSql: "'Borrowed'",
                oldComment: "Borrowed/Returned/Overdue/Cancelled")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
