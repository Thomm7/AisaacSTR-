using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AisaacSTR.Data;
using Microsoft.AspNetCore.Authorization;

namespace AisaacSTR.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen()
        {
            var hoyUtc = DateTime.UtcNow.Date;
            var finHoyUtc = hoyUtc.AddDays(1);
            var inicioMesUtc = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // KPIs Principales
            var totalVentasMes = await _context.Ventas
                .Where(v => v.Fecha >= inicioMesUtc && v.Estado == "Completada")
                .SumAsync(v => (decimal?)v.Total) ?? 0;

            var totalVentasHoy = await _context.Ventas
                .Where(v => v.Fecha >= hoyUtc && v.Fecha < finHoyUtc && v.Estado == "Completada")
                .SumAsync(v => (decimal?)v.Total) ?? 0;

            var totalComprasMes = await _context.Compras
                .Where(c => c.Fecha >= inicioMesUtc && c.Estado == "Registrada")
                .SumAsync(c => (decimal?)c.Total) ?? 0;

            var valorInventarioCosto = await _context.Variantes
                .Where(v => v.Activo)
                .SumAsync(v => (decimal?)(v.StockActual * v.PrecioCompra)) ?? 0;

            var valorInventarioVenta = await _context.Variantes
                .Where(v => v.Activo)
                .SumAsync(v => (decimal?)(v.StockActual * v.PrecioVenta)) ?? 0;

            var totalPrendasFisicas = await _context.Variantes
                .Where(v => v.Activo)
                .SumAsync(v => (int?)v.StockActual) ?? 0;

            // 1. TOP DE PRENDAS MÁS VENDIDAS FILTRADO POR VARIANTES
            var topVendidas = await _context.DetallesVenta
                .Include(d => d.VarianteProducto)
                    .ThenInclude(v => v!.Producto)
                .Where(d => d.Venta != null && d.Venta.Estado == "Completada")
                .GroupBy(d => new
                {
                    d.VarianteProductoId,
                    d.VarianteProducto!.Sku,
                    d.VarianteProducto.Talla,
                    d.VarianteProducto.Color,
                    d.VarianteProducto.Marca,
                    NombrePrenda = d.VarianteProducto.Producto != null ? d.VarianteProducto.Producto.Nombre : "Prenda",
                    Categoria = d.VarianteProducto.Producto != null ? d.VarianteProducto.Producto.Categoria : "Moda"
                })
                .Select(g => new
                {
                    VarianteId = g.Key.VarianteProductoId,
                    g.Key.Sku,
                    g.Key.NombrePrenda,
                    g.Key.Talla,
                    g.Key.Color,
                    g.Key.Marca,
                    g.Key.Categoria,
                    UnidadesVendidas = g.Sum(x => x.Cantidad),
                    TotalIngresos = g.Sum(x => x.Subtotal)
                })
                .OrderByDescending(x => x.UnidadesVendidas)
                .Take(10)
                .ToListAsync();

            // 2. SISTEMA DE ALERTAS AUTOMÁTICAS DE STOCK MÍNIMO CON SUGERENCIAS DE REABASTECIMIENTO
            // Condición: stock_actual <= stock_minimo
            var alertasStock = await _context.Variantes
                .Include(v => v.Producto)
                .Where(v => v.Activo && v.StockActual <= v.StockMinimo)
                .OrderBy(v => v.StockActual) // Los más críticos primero
                .Select(v => new
                {
                    v.Id,
                    v.Sku,
                    v.CodigoBarras,
                    NombreProducto = v.Producto != null ? v.Producto.Nombre : "Sin Nombre",
                    Categoria = v.Producto != null ? v.Producto.Categoria : "Prendas",
                    v.Talla,
                    v.Marca,
                    v.Color,
                    v.StockActual,
                    v.StockMinimo,
                    v.StockMaximo,
                    // Sugerencia de compra calculada: stock_maximo - stock_actual
                    UnidadesSugeridasReabastecer = Math.Max(0, v.StockMaximo - v.StockActual),
                    CostoEstimadoReabastecimiento = Math.Max(0, v.StockMaximo - v.StockActual) * v.PrecioCompra,
                    NivelAlerta = v.StockActual == 0 ? "AGOTADO" : (v.StockActual <= v.StockMinimo / 2 ? "CRITICO" : "BAJO"),
                    v.PrecioCompra
                })
                .ToListAsync();

            // 3. DISTRIBUCIÓN POR MEDIOS DE PAGO (Efectivo vs Bancos vs Sistecrédito/Addi)
            var ventasMedios = await _context.Ventas
                .Include(v => v.MedioPago)
                .Where(v => v.Fecha >= inicioMesUtc && v.Estado == "Completada")
                .GroupBy(v => v.MedioPago != null ? v.MedioPago.CategoriaFlujo : "Otros")
                .Select(g => new
                {
                    Categoria = g.Key,
                    Total = g.Sum(x => x.Total),
                    Transacciones = g.Count()
                })
                .ToListAsync();

            var presupuestoProveedores = Math.Round(totalVentasMes * 0.15m, 2);
            var comprasPagadasMes = await _context.Compras
                .Where(c => c.Fecha >= inicioMesUtc && c.Estado == "Registrada" && c.MedioPago != "Credito")
                .SumAsync(c => (decimal?)c.Total) ?? 0;

            var historialVentas = await _context.Ventas
                .Include(v => v.MedioPago)
                .Include(v => v.Usuario)
                .Where(v => v.Estado == "Completada")
                .OrderByDescending(v => v.Fecha)
                .Take(500)
                .Select(v => new
                {
                    v.Id,
                    v.NumeroFactura,
                    v.Fecha,
                    v.Total,
                    MedioPago = v.MedioPago != null ? v.MedioPago.Nombre : "Otro",
                    MedioPagoCodigo = v.MedioPago != null ? v.MedioPago.Codigo : "OTRO",
                    v.Banco,
                    v.ReferenciaPago,
                    Cajero = v.Usuario != null ? v.Usuario.NombreCompleto : "Sistema"
                })
                .ToListAsync();

            return Ok(new
            {
                kpis = new
                {
                    totalVentasMes,
                    totalVentasHoy,
                    totalComprasMes,
                    valorInventarioCosto,
                    valorInventarioVenta,
                    totalPrendasFisicas,
                    totalAlertasCriticas = alertasStock.Count,
                    presupuestoProveedores,
                    comprasPagadasMes,
                    presupuestoDisponible = Math.Max(0, presupuestoProveedores - comprasPagadasMes)
                },
                topVendidas,
                alertasStock,
                ventasMedios,
                historialVentas
            });
        }
    }
}
