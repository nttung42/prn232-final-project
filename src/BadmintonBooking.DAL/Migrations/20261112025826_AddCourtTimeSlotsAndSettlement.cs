using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BadmintonBooking.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCourtTimeSlotsAndSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourtId",
                table: "TimeSlots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Chuyển dữ liệu cũ: ca giờ theo cơ sở được gán cho sân đầu tiên, rồi sao chép sang các sân còn lại.
            migrationBuilder.Sql(@"
                UPDATE ts SET CourtId = c.Id
                FROM TimeSlots ts
                CROSS APPLY (SELECT TOP 1 Id FROM Courts WHERE FacilityId = ts.FacilityId ORDER BY Id) c
                WHERE ts.CourtId = 0;

                DELETE FROM TimeSlots WHERE CourtId = 0;

                INSERT INTO TimeSlots (FacilityId, CourtId, SlotName, StartTime, EndTime, Price, IsActive)
                SELECT ts.FacilityId, c.Id, ts.SlotName, ts.StartTime, ts.EndTime, ts.Price, ts.IsActive
                FROM TimeSlots ts
                JOIN Courts c ON c.FacilityId = ts.FacilityId AND c.Id <> ts.CourtId;
            ");

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "PaymentOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfirmedByUserId",
                table: "PaymentOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFee",
                table: "PaymentOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProviderAmount",
                table: "PaymentOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReversedAt",
                table: "PaymentOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SettledAt",
                table: "PaymentOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_CourtId_StartTime",
                table: "TimeSlots",
                columns: new[] { "CourtId", "StartTime" });

            migrationBuilder.AddForeignKey(
                name: "FK_TimeSlots_Courts_CourtId",
                table: "TimeSlots",
                column: "CourtId",
                principalTable: "Courts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeSlots_Courts_CourtId",
                table: "TimeSlots");

            migrationBuilder.DropIndex(
                name: "IX_TimeSlots_CourtId_StartTime",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "CourtId",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "ConfirmedByUserId",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "PlatformFee",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "ProviderAmount",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "ReversedAt",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "SettledAt",
                table: "PaymentOrders");
        }
    }
}
