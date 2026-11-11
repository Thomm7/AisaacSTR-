using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AisaacSTR.Data;
using AisaacSTR.Data.Entities;
using AisaacSTR.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace AisaacSTR.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class ProveedoresController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProveedoresController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetProveedores([FromQuery] string? busqueda)
        {
            var query = _context.Proveedores.Where(p => p.Activo);

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var q = busqueda.Trim().ToLower();
                query = query.Where(p =>
                    p.RazonSocial.ToLower().Contains(q) ||
                    p.DocumentoNit.ToLower().Contains(q) ||
                    (p.Contacto != null && p.Contacto.ToLower().Contains(q)) ||
                    p.Ciudad.ToLower().Contains(q));
            }

            var list = await query
                .OrderBy(p => p.RazonSocial)
                .Select(p => new
                {
                    p.Id,
                    p.DocumentoNit,
                    p.RazonSocial,
                    p.Contacto,
                    p.Telefono,
                    p.Email,
                    p.Direccion,
                    p.Ciudad,
                    p.Activo,
                    TotalCompras = p.Compras.Count,
                    TotalMontoComprado = p.Compras.Sum(c => c.Total)
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProveedorById(int id)
        {
            var p = await _context.Proveedores
                .Include(x => x.Compras)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p == null) return NotFound(new { mensaje = "Proveedor no encontrado." });

            return Ok(p);
        }

        [HttpPost]
        public async Task<IActionResult> CrearProveedor([FromBody] ProveedorCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.DocumentoNit) || string.IsNullOrWhiteSpace(dto.RazonSocial))
                return BadRequest(new { mensaje = "El NIT/Documento y la Razón Social son obligatorios." });

            var nitExiste = await _context.Proveedores.AnyAsync(p => p.DocumentoNit.ToLower() == dto.DocumentoNit.Trim().ToLower());
            if (nitExiste) return BadRequest(new { mensaje = $"El NIT '{dto.DocumentoNit}' ya existe en el sistema." });

            var proveedor = new Proveedor
            {
                DocumentoNit = dto.DocumentoNit.Trim(),
                RazonSocial = dto.RazonSocial.Trim(),
                Contacto = dto.Contacto?.Trim(),
                Telefono = dto.Telefono?.Trim(),
                Email = dto.Email?.Trim(),
                Direccion = dto.Direccion?.Trim(),
                Ciudad = string.IsNullOrWhiteSpace(dto.Ciudad) ? "Neiva" : dto.Ciudad.Trim(),
                Activo = true,
                FechaRegistro = DateTime.UtcNow
            };

            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Proveedor registrado exitosamente.", proveedorId = proveedor.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarProveedor(int id, [FromBody] ProveedorCreateDto dto)
        {
            var p = await _context.Proveedores.FindAsync(id);
            if (p == null) return NotFound(new { mensaje = "Proveedor no encontrado." });

            p.RazonSocial = dto.RazonSocial.Trim();
            p.Contacto = dto.Contacto?.Trim();
            p.Telefono = dto.Telefono?.Trim();
            p.Email = dto.Email?.Trim();
            p.Direccion = dto.Direccion?.Trim();
            p.Ciudad = dto.Ciudad.Trim();

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Proveedor actualizado correctamente." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarProveedor(int id)
        {
            var p = await _context.Proveedores.FindAsync(id);
            if (p == null) return NotFound(new { mensaje = "Proveedor no encontrado." });

            p.Activo = false;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Proveedor desactivado exitosamente." });
        }
    }
}
