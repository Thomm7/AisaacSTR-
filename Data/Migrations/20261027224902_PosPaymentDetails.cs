using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AisaacSTR.Data.Migrations
{
    /// <inheritdoc />
    public partial class PosPaymentDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "banco",
                table: "ventas",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "cambio_entregado",
                table: "ventas",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "codigo_aprobacion",
                table: "ventas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "efectivo_recibido",
                table: "ventas",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "imagen_url",
                table: "productos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cierre_caja_id",
                table: "compras_proveedor",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "descontar_caja",
                table: "compras_proveedor",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "medio_pago",
                table: "compras_proveedor",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "total_pagos_proveedores_efectivo",
                table: "cierres_caja",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_compras_proveedor_cierre_caja_id",
                table: "compras_proveedor",
                column: "cierre_caja_id");

            migrationBuilder.AddForeignKey(
                name: "FK_compras_proveedor_cierres_caja_cierre_caja_id",
                table: "compras_proveedor",
                column: "cierre_caja_id",
                principalTable: "cierres_caja",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compras_proveedor_cierres_caja_cierre_caja_id",
                table: "compras_proveedor");

            migrationBuilder.DropIndex(
                name: "IX_compras_proveedor_cierre_caja_id",
                table: "compras_proveedor");

            migrationBuilder.DropColumn(
                name: "banco",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "cambio_entregado",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "codigo_aprobacion",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "efectivo_recibido",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "imagen_url",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "cierre_caja_id",
                table: "compras_proveedor");

            migrationBuilder.DropColumn(
                name: "descontar_caja",
                table: "compras_proveedor");

            migrationBuilder.DropColumn(
                name: "medio_pago",
                table: "compras_proveedor");

            migrationBuilder.DropColumn(
                name: "total_pagos_proveedores_efectivo",
                table: "cierres_caja");
        }
    }
}
