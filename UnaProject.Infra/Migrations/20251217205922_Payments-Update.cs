using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnaProject.Infra.Migrations
{
    /// <inheritdoc />
    public partial class PaymentsUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AbacateFeeType",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AbacateFrequency",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AbacateKind",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AbacateMethod",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AbacateStatus",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingId",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DevMode",
                table: "Payments",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Fee",
                table: "Payments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentToken",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentUrl",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrCode",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrCodeImage",
                table: "Payments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbacateFeeType",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AbacateFrequency",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AbacateKind",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AbacateMethod",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AbacateStatus",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "BillingId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DevMode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Fee",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentToken",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentUrl",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "QrCode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "QrCodeImage",
                table: "Payments");
        }
    }
}
