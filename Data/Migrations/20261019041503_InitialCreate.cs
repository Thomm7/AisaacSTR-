using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AisaacSTR.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "medios_pago",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    categoria_flujo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    requiere_referencia = table.Column<bool>(type: "boolean", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medios_pago", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "productos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo_base = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    categoria = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "proveedores",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    documento_nit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    razon_social = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    contacto = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    direccion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ciudad = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fecha_registro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proveedores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre_completo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    rol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Cajero"),
                    telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "variante_productos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    producto_id = table.Column<int>(type: "integer", nullable: false),
                    talla = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    marca = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    color = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    sku = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    codigo_barras = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    precio_compra = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    precio_venta = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    stock_actual = table.Column<int>(type: "integer", nullable: false),
                    stock_minimo = table.Column<int>(type: "integer", nullable: false),
                    stock_maximo = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variante_productos", x => x.id);
                    table.ForeignKey(
                        name: "FK_variante_productos_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cierres_caja",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    fecha_apertura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_cierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    monto_inicial = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_ventas_efectivo = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_ventas_transferencia = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_ventas_credito = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_esperado = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_real_contado = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    diferencia = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cierres_caja", x => x.id);
                    table.ForeignKey(
                        name: "FK_cierres_caja_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "compras_proveedor",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    numero_factura = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    proveedor_id = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras_proveedor", x => x.id);
                    table.ForeignKey(
                        name: "FK_compras_proveedor_proveedores_proveedor_id",
                        column: x => x.proveedor_id,
                        principalTable: "proveedores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_proveedor_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ventas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    numero_factura = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    medio_pago_id = table.Column<int>(type: "integer", nullable: false),
                    referencia_pago = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    descuento = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    iva = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    cierre_caja_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventas", x => x.id);
                    table.ForeignKey(
                        name: "FK_ventas_cierres_caja_cierre_caja_id",
                        column: x => x.cierre_caja_id,
                        principalTable: "cierres_caja",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ventas_medios_pago_medio_pago_id",
                        column: x => x.medio_pago_id,
                        principalTable: "medios_pago",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "detalle_compras",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    compra_id = table.Column<int>(type: "integer", nullable: false),
                    variante_producto_id = table.Column<int>(type: "integer", nullable: false),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    costo_unitario = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detalle_compras", x => x.id);
                    table.ForeignKey(
                        name: "FK_detalle_compras_compras_proveedor_compra_id",
                        column: x => x.compra_id,
                        principalTable: "compras_proveedor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_detalle_compras_variante_productos_variante_producto_id",
                        column: x => x.variante_producto_id,
                        principalTable: "variante_productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "detalle_ventas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    venta_id = table.Column<int>(type: "integer", nullable: false),
                    variante_producto_id = table.Column<int>(type: "integer", nullable: false),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    precio_unitario = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detalle_ventas", x => x.id);
                    table.ForeignKey(
                        name: "FK_detalle_ventas_variante_productos_variante_producto_id",
                        column: x => x.variante_producto_id,
                        principalTable: "variante_productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_detalle_ventas_ventas_venta_id",
                        column: x => x.venta_id,
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cierres_caja_fecha_apertura",
                table: "cierres_caja",
                column: "fecha_apertura");

            migrationBuilder.CreateIndex(
                name: "IX_cierres_caja_usuario_id",
                table: "cierres_caja",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_compras_proveedor_proveedor_id",
                table: "compras_proveedor",
                column: "proveedor_id");

            migrationBuilder.CreateIndex(
                name: "IX_compras_proveedor_usuario_id",
                table: "compras_proveedor",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_detalle_compras_compra_id",
                table: "detalle_compras",
                column: "compra_id");

            migrationBuilder.CreateIndex(
                name: "IX_detalle_compras_variante_producto_id",
                table: "detalle_compras",
                column: "variante_producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_detalle_ventas_variante_producto_id",
                table: "detalle_ventas",
                column: "variante_producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_detalle_ventas_venta_id",
                table: "detalle_ventas",
                column: "venta_id");

            migrationBuilder.CreateIndex(
                name: "IX_medios_pago_codigo",
                table: "medios_pago",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productos_codigo_base",
                table: "productos",
                column: "codigo_base",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proveedores_documento_nit",
                table: "proveedores",
                column: "documento_nit",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_variante_productos_codigo_barras",
                table: "variante_productos",
                column: "codigo_barras");

            migrationBuilder.CreateIndex(
                name: "IX_variante_productos_producto_id_talla_color_marca",
                table: "variante_productos",
                columns: new[] { "producto_id", "talla", "color", "marca" });

            migrationBuilder.CreateIndex(
                name: "IX_variante_productos_sku",
                table: "variante_productos",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ventas_cierre_caja_id",
                table: "ventas",
                column: "cierre_caja_id");

            migrationBuilder.CreateIndex(
                name: "IX_ventas_fecha",
                table: "ventas",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "IX_ventas_medio_pago_id",
                table: "ventas",
                column: "medio_pago_id");

            migrationBuilder.CreateIndex(
                name: "IX_ventas_numero_factura",
                table: "ventas",
                column: "numero_factura",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ventas_usuario_id",
                table: "ventas",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "detalle_compras");

            migrationBuilder.DropTable(
                name: "detalle_ventas");

            migrationBuilder.DropTable(
                name: "compras_proveedor");

            migrationBuilder.DropTable(
                name: "variante_productos");

            migrationBuilder.DropTable(
                name: "ventas");

            migrationBuilder.DropTable(
                name: "proveedores");

            migrationBuilder.DropTable(
                name: "productos");

            migrationBuilder.DropTable(
                name: "cierres_caja");

            migrationBuilder.DropTable(
                name: "medios_pago");

            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
