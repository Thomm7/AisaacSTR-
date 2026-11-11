using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AisaacSTR.Data;
using AisaacSTR.Data.Entities;
using AisaacSTR.DTOs;
using System.Security.Claims;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace AisaacSTR.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VentasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VentasController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("medios-pago")]
        public async Task<IActionResult> GetMediosPago()
        {
            var medios = await _context.MediosPago
                .Where(m => m.Activo)
                .OrderBy(m => m.Id)
                .Select(m => new
                {
                    m.Id,
                    m.Codigo,
                    m.Nombre,
                    m.CategoriaFlujo,
                    m.Descripcion,
                    m.RequiereReferencia
                })
                .ToListAsync();

            return Ok(medios);
        }

        [HttpGet("hoy")]
        public async Task<IActionResult> GetVentasHoy()
        {
            var hoyUtc = DateTime.UtcNow.Date;
            var finHoyUtc = hoyUtc.AddDays(1);

            var ventas = await _context.Ventas
                .Include(v => v.Usuario)
                .Include(v => v.MedioPago)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.VarianteProducto)
                        .ThenInclude(vp => vp!.Producto)
                .Where(v => v.Fecha >= hoyUtc && v.Fecha < finHoyUtc)
                .OrderByDescending(v => v.Fecha)
                .Select(v => new
                {
                    v.Id,
                    v.NumeroFactura,
                    v.Fecha,
                    Usuario = v.Usuario != null ? v.Usuario.NombreCompleto : "Cajero",
                    MedioPago = v.MedioPago != null ? v.MedioPago.Nombre : "Efectivo",
                    CategoriaPago = v.MedioPago != null ? v.MedioPago.CategoriaFlujo : "Efectivo",
                    v.ReferenciaPago,
                    v.Banco,
                    v.CodigoAprobacion,
                    v.EfectivoRecibido,
                    v.CambioEntregado,
                    v.Subtotal,
                    v.Descuento,
                    v.Iva,
                    v.Total,
                    v.Estado,
                    Detalles = v.Detalles.Select(d => new
                    {
                        d.Id,
                        Sku = d.VarianteProducto != null ? d.VarianteProducto.Sku : "",
                        Producto = d.VarianteProducto != null && d.VarianteProducto.Producto != null ? d.VarianteProducto.Producto.Nombre : "",
                        Variante = d.VarianteProducto != null ? $"{d.VarianteProducto.Talla} / {d.VarianteProducto.Color}" : "",
                        d.Cantidad,
                        d.PrecioUnitario,
                        d.Subtotal
                    }).ToList()
                })
                .ToListAsync();

            return Ok(ventas);
        }

        // Historial de ventas con control de acceso:
        // Administrador: consulta todas las ventas.
        // Cajero: consulta únicamente las ventas asociadas al cierre de caja activo.
        [HttpGet("historial")]
        [Authorize(Roles = "Administrador,Cajero")]
        public async Task<IActionResult> GetHistorialVentas([FromQuery] int? usuarioId)
        {
            var query = _context.Ventas
                .Include(v => v.MedioPago)
                .Include(v => v.Usuario)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.VarianteProducto)
                        .ThenInclude(vp => vp!.Producto)
                .Where(v => v.Estado == "Completada");

            // Identificar rol y usuario desde claims del token JWT
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            bool esCajero = string.Equals(roleClaim, "Cajero", StringComparison.OrdinalIgnoreCase);

            if (esCajero)
            {
                if (int.TryParse(userIdClaim, out var cajeroId))
                {
                    var cierreActivo = await _context.CierresCaja
                        .Where(c => c.UsuarioId == cajeroId && c.Estado == "Abierta")
                        .OrderByDescending(c => c.FechaApertura)
                        .FirstOrDefaultAsync();

                    query = query.Where(v => v.UsuarioId == cajeroId);

                    if (cierreActivo != null)
                    {
                        query = query.Where(v =>
                            v.CierreCajaId == cierreActivo.Id ||
                            (v.CierreCajaId == null && v.Fecha >= cierreActivo.FechaApertura));
                    }
                }
                else
                {
                    return Forbid();
                }
            }
            else if (usuarioId.HasValue && usuarioId.Value > 0)
            {
                // Administrador puede filtrar por un usuario específico si lo desea
                query = query.Where(v => v.UsuarioId == usuarioId.Value);
            }

            var ventas = await query
                .OrderByDescending(v => v.Fecha)
                .Take(500)
                .Select(v => new
                {
                    v.Id,
                    v.NumeroFactura,
                    v.Fecha,
                    v.Total,
                    v.Subtotal,
                    v.Descuento,
                    v.Iva,
                    MedioPago = v.MedioPago != null ? v.MedioPago.Nombre : "Efectivo",
                    MedioPagoCodigo = v.MedioPago != null ? v.MedioPago.Codigo : "EFECTIVO",
                    CategoriaPago = v.MedioPago != null ? v.MedioPago.CategoriaFlujo : "Efectivo",
                    v.Banco,
                    v.ReferenciaPago,
                    v.CodigoAprobacion,
                    v.EfectivoRecibido,
                    v.CambioEntregado,
                    v.UsuarioId,
                    Cajero = v.Usuario != null ? v.Usuario.NombreCompleto : "Cajero",
                    TotalPrendas = v.Detalles.Sum(d => d.Cantidad),
                    Detalles = v.Detalles.Select(d => new
                    {
                        d.Id,
                        d.VarianteProductoId,
                        Sku = d.VarianteProducto != null ? d.VarianteProducto.Sku : "",
                        Producto = d.VarianteProducto != null && d.VarianteProducto.Producto != null ? d.VarianteProducto.Producto.Nombre : "Prenda",
                        Categoria = d.VarianteProducto != null && d.VarianteProducto.Producto != null ? d.VarianteProducto.Producto.Categoria : "",
                        Talla = d.VarianteProducto != null ? d.VarianteProducto.Talla : "",
                        Color = d.VarianteProducto != null ? d.VarianteProducto.Color : "",
                        Marca = d.VarianteProducto != null ? d.VarianteProducto.Marca : "",
                        d.Cantidad,
                        d.PrecioUnitario,
                        d.Subtotal
                    }).ToList()
                })
                .ToListAsync();

            return Ok(ventas);
        }

        // Obtener el detalle completo de una venta específica por ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetalleVenta(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.MedioPago)
                .Include(v => v.Usuario)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.VarianteProducto)
                        .ThenInclude(vp => vp!.Producto)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (venta == null) return NotFound(new { mensaje = "Factura de venta no encontrada." });

            // Si es cajero, verificar que la venta pertenezca estrictamente a su turno activo.
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.Equals(roleClaim, "Cajero", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(userIdClaim, out var cajeroId))
                {
                    return Forbid();
                }

                var perteneceAlTurnoActivo = venta.CierreCajaId.HasValue &&
                    await _context.CierresCaja.AnyAsync(c =>
                        c.Id == venta.CierreCajaId.Value &&
                        c.UsuarioId == cajeroId &&
                        c.Estado == "Abierta");

                if (!perteneceAlTurnoActivo)
                {
                    return Forbid();
                }
            }

            return Ok(new
            {
                venta.Id,
                venta.NumeroFactura,
                venta.Fecha,
                venta.Total,
                venta.Subtotal,
                venta.Descuento,
                venta.Iva,
                venta.Estado,
                venta.CierreCajaId,
                MedioPago = venta.MedioPago != null ? venta.MedioPago.Nombre : "Efectivo",
                CategoriaPago = venta.MedioPago != null ? venta.MedioPago.CategoriaFlujo : "Efectivo",
                venta.Banco,
                venta.ReferenciaPago,
                venta.CodigoAprobacion,
                venta.EfectivoRecibido,
                venta.CambioEntregado,
                Cajero = venta.Usuario != null ? venta.Usuario.NombreCompleto : "Cajero",
                CajeroRol = venta.Usuario != null ? venta.Usuario.Rol : "Cajero",
                Detalles = venta.Detalles.Select(d => new
                {
                    d.Id,
                    d.VarianteProductoId,
                    Sku = d.VarianteProducto != null ? d.VarianteProducto.Sku : "",
                    Producto = d.VarianteProducto != null && d.VarianteProducto.Producto != null ? d.VarianteProducto.Producto.Nombre : "Prenda",
                    Categoria = d.VarianteProducto != null && d.VarianteProducto.Producto != null ? d.VarianteProducto.Producto.Categoria : "",
                    Talla = d.VarianteProducto != null ? d.VarianteProducto.Talla : "",
                    Color = d.VarianteProducto != null ? d.VarianteProducto.Color : "",
                    Marca = d.VarianteProducto != null ? d.VarianteProducto.Marca : "",
                    d.Cantidad,
                    d.PrecioUnitario,
                    d.Subtotal
                }).ToList()
            });
        }

        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> DescargarFacturaPdf(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.MedioPago)
                .Include(v => v.Usuario)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.VarianteProducto)
                        .ThenInclude(vp => vp!.Producto)
                .FirstOrDefaultAsync(v => v.Id == id && v.Estado == "Completada");

            if (venta == null)
            {
                return NotFound(new { mensaje = "Factura de venta no encontrada." });
            }

            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.Equals(roleClaim, "Cajero", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(userIdClaim, out var cajeroId))
                {
                    return Forbid();
                }

                var perteneceAlTurnoActivo = venta.CierreCajaId.HasValue &&
                    await _context.CierresCaja.AnyAsync(c =>
                        c.Id == venta.CierreCajaId.Value &&
                        c.UsuarioId == cajeroId &&
                        c.Estado == "Abierta");

                if (!perteneceAlTurnoActivo)
                {
                    return Forbid();
                }
            }

            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(column =>
                    {
                        column.Item().Text("AISAAC STR")
                            .FontSize(22).Bold().FontColor(Colors.Blue.Darken3);
                        column.Item().Text("FACTURA DE VENTA").FontSize(16).SemiBold();
                        column.Item().Text($"No. {venta.NumeroFactura}").FontSize(12);
                    });

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Fecha: {venta.Fecha.ToLocalTime():dd/MM/yyyy HH:mm}");
                            row.RelativeItem().Text($"Responsable: {venta.Usuario?.NombreCompleto ?? "Cajero"}");
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Medio de pago: {venta.MedioPago?.Nombre ?? "Efectivo"}");
                            row.RelativeItem().Text($"Estado: {venta.Estado}");
                        });

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellHeader).Text("Producto / SKU");
                                header.Cell().Element(CellHeader).AlignCenter().Text("Talla");
                                header.Cell().Element(CellHeader).AlignCenter().Text("Color");
                                header.Cell().Element(CellHeader).AlignRight().Text("Cant.");
                                header.Cell().Element(CellHeader).AlignRight().Text("Subtotal");
                            });

                            foreach (var detalle in venta.Detalles)
                            {
                                var variante = detalle.VarianteProducto;
                                var producto = variante?.Producto?.Nombre ?? "Prenda";
                                var sku = variante?.Sku ?? "";

                                table.Cell().Element(CellBody).Column(item =>
                                {
                                    item.Item().Text(producto).SemiBold();
                                    item.Item().Text(sku).FontSize(8).FontColor(Colors.Grey.Darken1);
                                });
                                table.Cell().Element(CellBody).AlignCenter().Text(variante?.Talla ?? "");
                                table.Cell().Element(CellBody).AlignCenter().Text(variante?.Color ?? "");
                                table.Cell().Element(CellBody).AlignRight().Text(detalle.Cantidad.ToString());
                                table.Cell().Element(CellBody).AlignRight().Text(FormatearPesos(detalle.Subtotal));
                            }
                        });

                        column.Item().AlignRight().Column(totales =>
                        {
                            totales.Item().Text($"Subtotal: {FormatearPesos(venta.Subtotal)}");
                            totales.Item().Text($"Descuento: {FormatearPesos(venta.Descuento)}");
                            totales.Item().Text($"IVA: {FormatearPesos(venta.Iva)}");
                            totales.Item().Text($"Total: {FormatearPesos(venta.Total)}").FontSize(14).Bold();
                        });
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Factura generada por Aisaac STR - Página ");
                        text.CurrentPageNumber();
                    });
                });
            });

            var archivoPdf = documento.GeneratePdf();
            var nombreSeguro = string.Join("_", venta.NumeroFactura.Split(Path.GetInvalidFileNameChars()));
            return File(archivoPdf, "application/pdf", $"Factura_{nombreSeguro}.pdf");
        }

        private static IContainer CellHeader(IContainer container)
        {
            return container.PaddingVertical(5).Background(Colors.Grey.Lighten3);
        }

        private static IContainer CellBody(IContainer container)
        {
            return container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
        }

        private static string FormatearPesos(decimal valor)
        {
            return $"$ {valor:N0}";
        }

        [HttpPost("procesar")]
        [Authorize(Roles = "Administrador,Cajero")]
        public async Task<IActionResult> ProcesarVenta([FromBody] VentaCreateDto dto)
        {
            if (dto.MedioPagoId <= 0)
                return BadRequest(new { mensaje = "Debe seleccionar un método de pago válido." });

            if (dto.Items == null || dto.Items.Count == 0)
                return BadRequest(new { mensaje = "El carrito de compras no contiene artículos." });

            var medioPago = await _context.MediosPago.FindAsync(dto.MedioPagoId);
            if (medioPago == null) return NotFound(new { mensaje = "Método de pago no reconocido." });
            if (!medioPago.Activo) return BadRequest(new { mensaje = "El método de pago seleccionado está inactivo." });

            // Identificar al usuario autenticado; no usar otro usuario como respaldo.
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int usuarioId;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsedId))
            {
                usuarioId = parsedId;
            }
            else
            {
                return Forbid();
            }

            // Validar stock antes de descontar
            foreach (var item in dto.Items)
            {
                var v = await _context.Variantes.Include(x => x.Producto).FirstOrDefaultAsync(x => x.Id == item.VarianteProductoId);
                if (v == null)
                    return BadRequest(new { mensaje = $"La prenda con ID {item.VarianteProductoId} no existe en inventario." });

                if (v.StockActual < item.Cantidad)
                {
                    var nombre = v.Producto != null ? v.Producto.Nombre : v.Sku;
                    return BadRequest(new { mensaje = $"Stock insuficiente para '{nombre} ({v.Talla}/{v.Color})'. Disponibles: {v.StockActual}, solicitados: {item.Cantidad}." });
                }
            }

            // Generar correlativo de factura
            var totalVentasCount = await _context.Ventas.CountAsync();
            var numeroFactura = $"FAC-{(totalVentasCount + 1):D5}";

            var venta = new Venta
            {
                NumeroFactura = numeroFactura,
                Fecha = DateTime.UtcNow,
                UsuarioId = usuarioId,
                MedioPagoId = dto.MedioPagoId,
                ReferenciaPago = dto.ReferenciaPago?.Trim(),
                Banco = dto.Banco?.Trim(),
                CodigoAprobacion = dto.CodigoAprobacion?.Trim(),
                EfectivoRecibido = dto.EfectivoRecibido,
                CambioEntregado = dto.CambioEntregado,
                Descuento = dto.Descuento,
                Iva = 0,
                Estado = "Completada"
            };

            decimal subtotalGeneral = 0;

            foreach (var item in dto.Items)
            {
                var v = await _context.Variantes.FindAsync(item.VarianteProductoId)!;
                decimal itemSubtotal = item.Cantidad * v!.PrecioVenta;
                subtotalGeneral += itemSubtotal;

                // Descontar inventario
                v.StockActual -= item.Cantidad;

                venta.Detalles.Add(new DetalleVenta
                {
                    VarianteProductoId = v.Id,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = v.PrecioVenta,
                    Subtotal = itemSubtotal
                });
            }

            venta.Subtotal = subtotalGeneral;
            venta.Total = Math.Max(0, subtotalGeneral - dto.Descuento);

            if (medioPago.CategoriaFlujo == "Efectivo")
            {
                if (dto.EfectivoRecibido < venta.Total)
                    return BadRequest(new { mensaje = "El efectivo recibido es menor al total a cobrar." });
                venta.EfectivoRecibido = dto.EfectivoRecibido;
                venta.CambioEntregado = dto.EfectivoRecibido - venta.Total;
            }
            else if (medioPago.CategoriaFlujo == "Transferencia")
            {
                var bancosPermitidos = new[] { "Nequi", "Daviplata", "Bancolombia" };
                if (string.IsNullOrWhiteSpace(dto.Banco) || !bancosPermitidos.Contains(dto.Banco.Trim(), StringComparer.OrdinalIgnoreCase))
                    return BadRequest(new { mensaje = "Seleccione un banco o billetera válido." });
                if (string.IsNullOrWhiteSpace(dto.ReferenciaPago))
                    return BadRequest(new { mensaje = "El comprobante o número de referencia es obligatorio." });
                venta.Banco = dto.Banco.Trim();
            }
            else if (medioPago.CategoriaFlujo == "Credito")
            {
                if (string.IsNullOrWhiteSpace(dto.CodigoAprobacion))
                    return BadRequest(new { mensaje = "El código de aprobación del crédito es obligatorio." });
            }

            // Si hay una caja abierta actualmente, vincular la venta
            var cajaAbierta = await _context.CierresCaja
                .Where(c => c.Estado == "Abierta" && c.UsuarioId == usuarioId)
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefaultAsync();

            if (cajaAbierta != null)
            {
                venta.CierreCajaId = cajaAbierta.Id;
                if (medioPago.CategoriaFlujo == "Efectivo")
                {
                    cajaAbierta.TotalVentasEfectivo += venta.Total;
                    cajaAbierta.TotalEsperado = cajaAbierta.MontoInicial + cajaAbierta.TotalVentasEfectivo - cajaAbierta.TotalPagosProveedoresEfectivo;
                }
                else if (medioPago.CategoriaFlujo == "Transferencia")
                {
                    cajaAbierta.TotalVentasTransferencia += venta.Total;
                }
                else if (medioPago.CategoriaFlujo == "Credito")
                {
                    cajaAbierta.TotalVentasCredito += venta.Total;
                }
            }

            _context.Ventas.Add(venta);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Venta procesada con éxito.",
                ventaId = venta.Id,
                numeroFactura = venta.NumeroFactura,
                fecha = venta.Fecha,
                total = venta.Total,
                medioPago = medioPago.Nombre,
                categoriaPago = medioPago.CategoriaFlujo
            });
        }
    }
}
