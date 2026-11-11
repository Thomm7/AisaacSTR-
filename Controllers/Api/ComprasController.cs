using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AisaacSTR.Data;
using AisaacSTR.Data.Entities;
using AisaacSTR.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace AisaacSTR.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class ComprasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ComprasController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCompras()
        {
            var compras = await _context.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.Usuario)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.VarianteProducto)
                        .ThenInclude(v => v!.Producto)
                .OrderByDescending(c => c.Fecha)
                .Select(c => new
                {
                    c.Id,
                    c.NumeroFactura,
                    c.Fecha,
                    ProveedorNombre = c.Proveedor != null ? c.Proveedor.RazonSocial : "Sin Proveedor",
                    ProveedorNit = c.Proveedor != null ? c.Proveedor.DocumentoNit : "",
                    UsuarioNombre = c.Usuario != null ? c.Usuario.NombreCompleto : "Sistema",
                    c.Total,
                    c.MedioPago,
                    c.DescontarCaja,
                    c.Estado,
                    c.Observaciones,
                    ItemsCount = c.Detalles.Count,
                    TotalUnidades = c.Detalles.Sum(d => d.Cantidad),
                    Detalles = c.Detalles.Select(d => new
                    {
                        d.Id,
                        d.VarianteProductoId,
                        Sku = d.VarianteProducto != null ? d.VarianteProducto.Sku : "",
                        Producto = d.VarianteProducto != null && d.VarianteProducto.Producto != null ? d.VarianteProducto.Producto.Nombre : "",
                        Variante = d.VarianteProducto != null ? $"{d.VarianteProducto.Talla} / {d.VarianteProducto.Color} ({d.VarianteProducto.Marca})" : "",
                        d.Cantidad,
                        d.CostoUnitario,
                        d.Subtotal
                    }).ToList()
                })
                .ToListAsync();

            return Ok(compras);
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarCompra([FromBody] CompraCreateDto dto)
        {
            if (dto.ProveedorId <= 0)
                return BadRequest(new { mensaje = "Debe seleccionar un proveedor válido." });

            if (dto.Items == null || dto.Items.Count == 0)
                return BadRequest(new { mensaje = "Debe agregar al menos un artículo a la orden de compra." });

            var proveedor = await _context.Proveedores.FindAsync(dto.ProveedorId);
            if (proveedor == null) return NotFound(new { mensaje = "Proveedor no encontrado." });

            // Obtener usuario actual del token o asignar el primero
            int usuarioId = 1;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsedId))
            {
                usuarioId = parsedId;
            }
            else
            {
                var primerAdmin = await _context.Usuarios.FirstOrDefaultAsync(u => u.Rol == "Administrador") ?? await _context.Usuarios.FirstOrDefaultAsync();
                if (primerAdmin != null) usuarioId = primerAdmin.Id;
            }

            var numeroFactura = string.IsNullOrWhiteSpace(dto.NumeroFactura)
                ? $"COM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}"
                : dto.NumeroFactura.Trim();

            var medioPago = string.IsNullOrWhiteSpace(dto.MedioPago) ? "Efectivo" : dto.MedioPago.Trim();
            var esEfectivo = medioPago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase);

            var compra = new CompraProveedor
            {
                ProveedorId = dto.ProveedorId,
                UsuarioId = usuarioId,
                NumeroFactura = numeroFactura,
                Fecha = DateTime.UtcNow,
                Estado = "Registrada",
                Observaciones = dto.Observaciones?.Trim(),
                MedioPago = medioPago,
                DescontarCaja = esEfectivo,
                Total = 0
            };

            decimal totalCalculado = 0;

            foreach (var item in dto.Items)
            {
                if (item.Cantidad <= 0) continue;

                var variante = await _context.Variantes.FindAsync(item.VarianteProductoId);
                if (variante == null)
                    return BadRequest(new { mensaje = $"La variante con ID {item.VarianteProductoId} no existe." });

                decimal subtotal = item.Cantidad * item.CostoUnitario;
                totalCalculado += subtotal;

                // Agregar línea de detalle
                compra.Detalles.Add(new DetalleCompra
                {
                    VarianteProductoId = variante.Id,
                    Cantidad = item.Cantidad,
                    CostoUnitario = item.CostoUnitario,
                    Subtotal = subtotal
                });

                // REGLA CLAVE: ACTUALIZACIÓN AUTOMÁTICA DE EXISTENCIAS / STOCK EN INVENTARIO
                variante.StockActual += item.Cantidad;

                // Si el costo de compra cambió, actualizamos también el precio de costo de la variante
                if (item.CostoUnitario > 0)
                {
                    variante.PrecioCompra = item.CostoUnitario;
                }
            }

            compra.Total = totalCalculado;

            // Toda compra pagada en efectivo afecta automáticamente la caja abierta.
            // Las compras a crédito quedan pendientes para el proveedor y no generan egreso de caja.
            if (esEfectivo)
            {
                var cajaAbierta = await _context.CierresCaja
                    .Where(c => c.Estado == "Abierta")
                    .OrderByDescending(c => c.FechaApertura)
                    .FirstOrDefaultAsync();

                if (cajaAbierta == null)
                {
                    return BadRequest(new { mensaje = "No hay una caja abierta. Abra el turno antes de pagar una compra en efectivo." });
                }

                compra.CierreCajaId = cajaAbierta.Id;
                cajaAbierta.TotalPagosProveedoresEfectivo += totalCalculado;
                cajaAbierta.TotalEsperado = cajaAbierta.MontoInicial + cajaAbierta.TotalVentasEfectivo - cajaAbierta.TotalPagosProveedoresEfectivo;
                if (cajaAbierta.TotalEsperado < 0)
                {
                    return BadRequest(new { mensaje = "Fondos insuficientes en la caja activa para realizar este pago." });
                }
            }

            _context.Compras.Add(compra);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Compra registrada, stock actualizado y egreso de caja procesado exitosamente.",
                compraId = compra.Id,
                numeroFactura = compra.NumeroFactura,
                total = compra.Total,
                descontadoCaja = compra.DescontarCaja && string.Equals(compra.MedioPago, "Efectivo", StringComparison.OrdinalIgnoreCase)
            });
        }
    }
}
