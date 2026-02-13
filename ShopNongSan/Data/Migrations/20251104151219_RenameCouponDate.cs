using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopNongSan.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameCouponDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExpireDate",
                table: "Coupons",
                newName: "ExpiryDate");

            migrationBuilder.RenameColumn(
                name: "DiscountPercent",
                table: "Coupons",
                newName: "DiscountValue");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Coupons",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "DiscountType",
                table: "Coupons",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "Coupons");

            migrationBuilder.RenameColumn(
                name: "ExpiryDate",
                table: "Coupons",
                newName: "ExpireDate");

            migrationBuilder.RenameColumn(
                name: "DiscountValue",
                table: "Coupons",
                newName: "DiscountPercent");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Coupons",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }
    }
}
