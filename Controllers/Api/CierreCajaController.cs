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
    [Authorize]
    public class CierreCajaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CierreCajaController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("hoy")]
        [Authorize(Roles = "Administrador,Cajero")]
        public async Task<IActionResult> GetCierreHoy()
        {
            var hoyUtc = DateTime.UtcNow.Date;
            var finHoyUtc = hoyUtc.AddDays(1);

            // Buscar si hay una caja abierta activa
            var cajaAbierta = await _context.CierresCaja
                .Include(c => c.Usuario)
                .Where(c => c.Estado == "Abierta")
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefaultAsync();

            int? cajaId = cajaAbierta?.Id;
            decimal montoInicial = cajaAbierta?.MontoInicial ?? 0;

            // Ventas del turno activo
            var queryVentas = _context.Ventas
                .Include(v => v.MedioPago)
                .Where(v => v.Estado == "Completada");

            if (cajaId.HasValue)
            {
                queryVentas = queryVentas.Where(v => v.CierreCajaId == cajaId || (v.CierreCajaId == null && v.Fecha >= cajaAbierta!.FechaApertura));
            }
            else
            {
                queryVentas = queryVentas.Where(v => v.CierreCajaId == null && v.Fecha >= hoyUtc && v.Fecha < finHoyUtc);
            }
            var ventasTurno = await queryVentas.ToListAsync();

            decimal totalEfectivo = ventasTurno
                .Where(v => v.MedioPago != null && v.MedioPago.CategoriaFlujo == "Efectivo")
                .Sum(v => v.Total);

            decimal totalTransferencias = ventasTurno
                .Where(v => v.MedioPago != null && v.MedioPago.CategoriaFlujo == "Transferencia")
                .Sum(v => v.Total);

            decimal totalCredito = ventasTurno
                .Where(v => v.MedioPago != null && v.MedioPago.CategoriaFlujo == "Credito")
                .Sum(v => v.Total);

            // Egresos por pagos a proveedores en efectivo tomados de la caja
            var queryCompras = _context.Compras
                .Include(c => c.Proveedor)
                .Where(c => c.Estado == "Registrada" && c.DescontarCaja && c.MedioPago == "Efectivo");

            if (cajaId.HasValue)
            {
                queryCompras = queryCompras.Where(c => c.CierreCajaId == cajaId || (c.CierreCajaId == null && c.Fecha >= cajaAbierta!.FechaApertura));
            }
            else
            {
                queryCompras = queryCompras.Where(c => c.CierreCajaId == null && c.Fecha >= hoyUtc && c.Fecha < finHoyUtc);
            }
            var comprasEfectivo = await queryCompras.OrderByDescending(c => c.Fecha).ToListAsync();

            decimal totalPagosProveedores = comprasEfectivo.Sum(c => c.Total);
            var totalDia = totalEfectivo + totalTransferencias + totalCredito;

            // Total esperado en efectivo: Monto Inicial + Ventas Efectivo - Pagos Proveedores
            var totalEsperado = montoInicial + totalEfectivo - totalPagosProveedores;
            var efectivoNetoEsperado = totalEfectivo - totalPagosProveedores;

            var desglose = ventasTurno
                .GroupBy(v => v.MedioPago != null ? v.MedioPago.Nombre : "Otro")
                .Select(g => new
                {
                    Medio = g.Key,
                    Transacciones = g.Count(),
                    Total = g.Sum(x => x.Total)
                })
                .ToList();

            var egresosDetalle = comprasEfectivo.Select(c => new
            {
                c.Id,
                c.NumeroFactura,
                Proveedor = c.Proveedor != null ? c.Proveedor.RazonSocial : "Proveedor",
                c.Total,
                c.Fecha,
                c.Observaciones
            }).ToList();

            var ultimoCierre = await _context.CierresCaja
                .Include(c => c.Usuario)
                .OrderByDescending(c => c.FechaCierre ?? c.FechaApertura)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                fecha = DateTime.UtcNow.Date,
                cajaAbiertaId = cajaId,
                estadoCaja = cajaAbierta != null ? "Abierta" : "SinApertura",
                responsable = cajaAbierta?.Usuario?.NombreCompleto ?? ultimoCierre?.Usuario?.NombreCompleto ?? "Sin asignar",
                fechaApertura = cajaAbierta?.FechaApertura,
                fechaCierre = cajaAbierta != null ? null : ultimoCierre?.FechaCierre,
                ultimoCierre = ultimoCierre != null ? new
                {
                    id = ultimoCierre.Id,
                    fechaApertura = ultimoCierre.FechaApertura,
                    fechaCierre = ultimoCierre.FechaCierre,
                    responsable = ultimoCierre.Usuario?.NombreCompleto ?? "Cajero",
                    montoInicial = ultimoCierre.MontoInicial,
                    totalEsperado = ultimoCierre.TotalEsperado,
                    totalRealContado = ultimoCierre.TotalRealContado,
                    diferencia = ultimoCierre.Diferencia,
                    estado = ultimoCierre.Estado
                } : null,
                montoInicial,
                totalEfectivo,
                totalTransferencias,
                totalCredito,
                totalPagosProveedores,
                efectivoNetoEsperado,
                totalEsperado,
                totalDia,
                totalVentasCount = ventasTurno.Count,
                totalEgresosCount = comprasEfectivo.Count,
                desglose,
                egresosDetalle
            });
        }

        [HttpPost("apertura")]
        [Authorize(Roles = "Administrador,Cajero")]
        public async Task<IActionResult> AperturaCaja([FromBody] CajaAperturaDto dto)
        {
            // Validar si ya hay una caja abierta
            var cajaAbierta = await _context.CierresCaja
                .Include(c => c.Usuario)
                .Where(c => c.Estado == "Abierta")
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefaultAsync();

            int usuarioId = 1;
            if (dto.UsuarioId.HasValue && dto.UsuarioId.Value > 0)
            {
                usuarioId = dto.UsuarioId.Value;
            }
            else
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsedId))
                {
                    usuarioId = parsedId;
                }
                else
                {
                    var primerUsuario = await _context.Usuarios.FirstOrDefaultAsync();
                    if (primerUsuario != null) usuarioId = primerUsuario.Id;
                }
            }

            if (cajaAbierta != null)
            {
                return BadRequest(new { 
                    mensaje = $"Ya existe una caja abierta iniciada por {cajaAbierta.Usuario?.NombreCompleto ?? "un cajero"}. Debe cerrarla antes de realizar una nueva apertura.",
                    cajaId = cajaAbierta.Id,
                    responsable = cajaAbierta.Usuario?.NombreCompleto ?? "Cajero",
                    fechaApertura = cajaAbierta.FechaApertura,
                    montoInicial = cajaAbierta.MontoInicial
                });
            }

            var nuevaCaja = new CierreCaja
            {
                UsuarioId = usuarioId,
                FechaApertura = DateTime.UtcNow,
                MontoInicial = dto.MontoInicial,
                TotalVentasEfectivo = 0,
                TotalVentasTransferencia = 0,
                TotalVentasCredito = 0,
                TotalPagosProveedoresEfectivo = 0,
                TotalEsperado = dto.MontoInicial,
                TotalRealContado = 0,
                Diferencia = 0,
                Observaciones = dto.Observaciones?.Trim(),
                Estado = "Abierta"
            };

            _context.CierresCaja.Add(nuevaCaja);
            await _context.SaveChangesAsync();

            await _context.Entry(nuevaCaja).Reference(c => c.Usuario).LoadAsync();

            return Ok(new { 
                mensaje = "Caja abierta exitosamente. El turno ha iniciado.", 
                cajaId = nuevaCaja.Id, 
                montoInicial = nuevaCaja.MontoInicial,
                responsable = nuevaCaja.Usuario?.NombreCompleto ?? "Cajero",
                fechaApertura = nuevaCaja.FechaApertura
            });
        }

        [HttpPost("confirmar")]
        [Authorize(Roles = "Administrador,Cajero")]
        public async Task<IActionResult> ConfirmarCierre([FromBody] CierreCajaCreateDto dto)
        {
            var hoyUtc = DateTime.UtcNow.Date;
            var finHoyUtc = hoyUtc.AddDays(1);

            var cajaAbierta = await _context.CierresCaja
                .Where(c => c.Estado == "Abierta")
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefaultAsync();

            int? cajaId = cajaAbierta?.Id;

            var queryVentas = _context.Ventas
                .Include(v => v.MedioPago)
                .Where(v => v.Estado == "Completada");

            if (cajaId.HasValue)
            {
                queryVentas = queryVentas.Where(v => v.CierreCajaId == cajaId || (v.CierreCajaId == null && v.Fecha >= cajaAbierta!.FechaApertura));
            }
            else
            {
                queryVentas = queryVentas.Where(v => v.CierreCajaId == null && v.Fecha >= hoyUtc && v.Fecha < finHoyUtc);
            }
            var ventasTurno = await queryVentas.ToListAsync();

            var queryCompras = _context.Compras
                .Where(c => c.Estado == "Registrada" && c.DescontarCaja && c.MedioPago == "Efectivo");

            if (cajaId.HasValue)
            {
                queryCompras = queryCompras.Where(c => c.CierreCajaId == cajaId || (c.CierreCajaId == null && c.Fecha >= cajaAbierta!.FechaApertura));
            }
            else
            {
                queryCompras = queryCompras.Where(c => c.CierreCajaId == null && c.Fecha >= hoyUtc && c.Fecha < finHoyUtc);
            }
            var comprasEfectivo = await queryCompras.ToListAsync();

            decimal totalEfectivo = ventasTurno
                .Where(v => v.MedioPago != null && v.MedioPago.CategoriaFlujo == "Efectivo")
                .Sum(v => v.Total);

            decimal totalTransferencias = ventasTurno
                .Where(v => v.MedioPago != null && v.MedioPago.CategoriaFlujo == "Transferencia")
                .Sum(v => v.Total);

            decimal totalCredito = ventasTurno
                .Where(v => v.MedioPago != null && v.MedioPago.CategoriaFlujo == "Credito")
                .Sum(v => v.Total);

            decimal totalPagosProveedores = comprasEfectivo.Sum(c => c.Total);

            // Monto Inicial a considerar
            decimal montoInicial = dto.MontoInicial > 0 ? dto.MontoInicial : (cajaAbierta?.MontoInicial ?? 0);

            // Total Esperado = Monto Inicial + Ventas Efectivo - Pagos/Egresos Proveedores
            decimal totalEsperado = montoInicial + totalEfectivo - totalPagosProveedores;
            decimal diferencia = dto.EfectivoContadoFisico - totalEsperado;

            // Identificar cajero responsable
            int usuarioId = 1;
            if (dto.UsuarioId.HasValue && dto.UsuarioId.Value > 0)
            {
                usuarioId = dto.UsuarioId.Value;
            }
            else
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsedId))
                {
                    usuarioId = parsedId;
                }
                else
                {
                    var primerUsuario = await _context.Usuarios.FirstOrDefaultAsync();
                    if (primerUsuario != null) usuarioId = primerUsuario.Id;
                }
            }

            CierreCaja cierre;
            if (cajaAbierta != null)
            {
                cierre = cajaAbierta;
                cierre.UsuarioId = usuarioId;
                cierre.FechaCierre = DateTime.UtcNow;
                cierre.MontoInicial = montoInicial;
                cierre.TotalVentasEfectivo = totalEfectivo;
                cierre.TotalVentasTransferencia = totalTransferencias;
                cierre.TotalVentasCredito = totalCredito;
                cierre.TotalPagosProveedoresEfectivo = totalPagosProveedores;
                cierre.TotalEsperado = totalEsperado;
                cierre.TotalRealContado = dto.EfectivoContadoFisico;
                cierre.Diferencia = diferencia;
                cierre.Observaciones = dto.Observaciones?.Trim();
                cierre.Estado = "Cerrada";
            }
            else
            {
                cierre = new CierreCaja
                {
                    UsuarioId = usuarioId,
                    FechaApertura = hoyUtc,
                    FechaCierre = DateTime.UtcNow,
                    MontoInicial = montoInicial,
                    TotalVentasEfectivo = totalEfectivo,
                    TotalVentasTransferencia = totalTransferencias,
                    TotalVentasCredito = totalCredito,
                    TotalPagosProveedoresEfectivo = totalPagosProveedores,
                    TotalEsperado = totalEsperado,
                    TotalRealContado = dto.EfectivoContadoFisico,
                    Diferencia = diferencia,
                    Observaciones = dto.Observaciones?.Trim(),
                    Estado = "Cerrada"
                };
                _context.CierresCaja.Add(cierre);
            }

            await _context.SaveChangesAsync();

            // Asociar las ventas del turno con este cierre
            foreach (var v in ventasTurno)
            {
                v.CierreCajaId = cierre.Id;
            }

            // Asociar los pagos a proveedores en efectivo con este cierre
            foreach (var c in comprasEfectivo)
            {
                c.CierreCajaId = cierre.Id;
            }
            await _context.SaveChangesAsync();
            await _context.Entry(cierre).Reference(c => c.Usuario).LoadAsync();

            return Ok(new
            {
                mensaje = "Cierre de caja completado y guardado correctamente.",
                cierreId = cierre.Id,
                fechaApertura = cierre.FechaApertura,
                fechaCierre = cierre.FechaCierre,
                responsable = cierre.Usuario?.NombreCompleto ?? "Cajero",
                montoInicial = cierre.MontoInicial,
                totalVentasEfectivo = cierre.TotalVentasEfectivo,
                totalTransferencias = cierre.TotalVentasTransferencia,
                totalCredito = cierre.TotalVentasCredito,
                totalPagosProveedores = cierre.TotalPagosProveedoresEfectivo,
                totalEsperado = cierre.TotalEsperado,
                totalReal = cierre.TotalRealContado,
                diferencia = cierre.Diferencia,
                estadoDiferencia = diferencia == 0 ? "CUADRADA" : (diferencia > 0 ? "SOBRANTE" : "FALTANTE")
            });
        }

        [HttpGet("historial")]
        public async Task<IActionResult> GetHistorial()
        {
            var cierres = await _context.CierresCaja
                .Include(c => c.Usuario)
                .OrderByDescending(c => c.FechaCierre ?? c.FechaApertura)
                .Take(30)
                .Select(c => new
                {
                    c.Id,
                    c.FechaApertura,
                    c.FechaCierre,
                    Usuario = c.Usuario != null ? c.Usuario.NombreCompleto : "Cajero",
                    c.MontoInicial,
                    c.TotalVentasEfectivo,
                    c.TotalVentasTransferencia,
                    c.TotalVentasCredito,
                    c.TotalPagosProveedoresEfectivo,
                    TotalVenta = c.TotalVentasEfectivo + c.TotalVentasTransferencia + c.TotalVentasCredito,
                    c.TotalEsperado,
                    c.TotalRealContado,
                    c.Diferencia,
                    c.Observaciones,
                    c.Estado
                })
                .ToListAsync();

            return Ok(cierres);
        }
    }
}
