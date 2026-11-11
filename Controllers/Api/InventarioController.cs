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
    [Authorize]
    public class InventarioController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public InventarioController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Obtener productos agrupados con sus variantes
        [HttpGet("productos")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetProductos([FromQuery] string? busqueda, [FromQuery] string? categoria, [FromQuery] int pagina = 1)
        {
            if (pagina < 1) pagina = 1;
            const int limite = 20;

            var query = _context.Productos
                .Include(p => p.Variantes)
                .Where(p => p.Activo);

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var q = busqueda.Trim().ToLower();
                query = query.Where(p =>
                    p.Nombre.ToLower().Contains(q) ||
                    p.CodigoBase.ToLower().Contains(q) ||
                    p.Variantes.Any(v => v.Sku.ToLower().Contains(q) || 
                                         (v.CodigoBarras != null && v.CodigoBarras.ToLower().Contains(q)) ||
                                         v.Marca.ToLower().Contains(q) ||
                                         v.Color.ToLower().Contains(q)));
            }

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                query = query.Where(p => p.Categoria == categoria);
            }

            var totalRegistros = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)limite);
            if (totalPaginas < 1) totalPaginas = 1;

            var list = await query
                .OrderBy(p => p.Nombre)
                .Skip((pagina - 1) * limite)
                .Take(limite)
                .Select(p => new
                {
                    p.Id,
                    p.CodigoBase,
                    p.Nombre,
                    p.Descripcion,
                    p.Categoria,
                    p.ImagenUrl,
                    p.Activo,
                    TotalVariantes = p.Variantes.Count,
                    StockTotal = p.Variantes.Sum(v => v.StockActual),
                    Variantes = p.Variantes.Select(v => new
                    {
                        v.Id,
                        v.Talla,
                        v.Marca,
                        v.Color,
                        v.Sku,
                        v.CodigoBarras,
                        v.PrecioCompra,
                        v.PrecioVenta,
                        v.StockActual,
                        v.StockMinimo,
                        v.StockMaximo,
                        v.Activo,
                        v.RequiereReabastecimiento,
                        v.UnidadesSugeridasReabastecimiento
                    }).ToList()
                })
                .ToListAsync();

            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalRegistros = totalRegistros;

            return Ok(new
            {
                pagina,
                totalPaginas,
                totalRegistros,
                limite,
                productos = list
            });
        }

        // Obtener catálogo plano de variantes para el POS y búsquedas rápidas
        [HttpGet("variantes")]
        [Authorize(Roles = "Administrador,Cajero")]
        public async Task<IActionResult> GetVariantes(
            [FromQuery] string? busqueda,
            [FromQuery] string? categoria,
            [FromQuery] string? talla,
            [FromQuery] string? color,
            [FromQuery] string? marca)
        {
            var query = _context.Variantes
                .Include(v => v.Producto)
                .Where(v => v.Activo && (v.Producto == null || v.Producto.Activo));

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var q = busqueda.Trim().ToLower();
                query = query.Where(v =>
                    v.Sku.ToLower().Contains(q) ||
                    (v.CodigoBarras != null && v.CodigoBarras.ToLower().Contains(q)) ||
                    (v.Producto != null && v.Producto.Nombre.ToLower().Contains(q)) ||
                    (v.Producto != null && v.Producto.CodigoBase.ToLower().Contains(q)) ||
                    v.Marca.ToLower().Contains(q) ||
                    v.Color.ToLower().Contains(q));
            }

            if (!string.IsNullOrWhiteSpace(categoria)) query = query.Where(v => v.Producto != null && v.Producto.Categoria.ToLower() == categoria.Trim().ToLower());
            if (!string.IsNullOrWhiteSpace(talla)) query = query.Where(v => v.Talla == talla);
            if (!string.IsNullOrWhiteSpace(color)) query = query.Where(v => v.Color == color);
            if (!string.IsNullOrWhiteSpace(marca)) query = query.Where(v => v.Marca == marca);

            var variantes = await query
                .OrderBy(v => v.Producto != null ? v.Producto.Nombre : "")
                .ThenBy(v => v.Talla)
                .Select(v => new
                {
                    v.Id,
                    v.ProductoId,
                    NombreProducto = v.Producto != null ? v.Producto.Nombre : "Sin Nombre",
                    Categoria = v.Producto != null ? v.Producto.Categoria : "General",
                    ImagenUrl = v.Producto != null ? v.Producto.ImagenUrl : null,
                    v.Talla,
                    v.Marca,
                    v.Color,
                    v.Sku,
                    v.CodigoBarras,
                    v.PrecioCompra,
                    v.PrecioVenta,
                    v.StockActual,
                    v.StockMinimo,
                    v.StockMaximo,
                    v.RequiereReabastecimiento,
                    v.UnidadesSugeridasReabastecimiento,
                    EtiquetaCompleta = $"{(v.Producto != null ? v.Producto.Nombre : "")} - {v.Talla} / {v.Color} ({v.Marca})"
                })
                .ToListAsync();

            return Ok(variantes);
        }

        // Crear producto base junto con su matriz de variantes
        [HttpPost("producto")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CrearProducto([FromForm] ProductoCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.CodigoBase))
                return BadRequest(new { mensaje = "El nombre y código base del producto son obligatorios." });

            var existe = await _context.Productos.AnyAsync(p => p.CodigoBase.ToLower() == dto.CodigoBase.Trim().ToLower());
            if (existe) return BadRequest(new { mensaje = $"El código base '{dto.CodigoBase}' ya se encuentra registrado." });

            var producto = new Producto
            {
                CodigoBase = dto.CodigoBase.Trim().ToUpper(),
                Nombre = dto.Nombre.Trim(),
                Descripcion = dto.Descripcion?.Trim(),
                Categoria = dto.Categoria.Trim(),
                ImagenUrl = await GuardarImagenProducto(dto.Imagen, null),
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            if (dto.Variantes != null && dto.Variantes.Count > 0)
            {
                foreach (var vDto in dto.Variantes)
                {
                    // Validar SKU único
                    var skuExiste = await _context.Variantes.AnyAsync(v => v.Sku.ToLower() == vDto.Sku.Trim().ToLower());
                    if (skuExiste)
                        return BadRequest(new { mensaje = $"El SKU '{vDto.Sku}' ya está asignado a otra variante." });

                    producto.Variantes.Add(new VarianteProducto
                    {
                        Talla = vDto.Talla.Trim(),
                        Marca = vDto.Marca.Trim(),
                        Color = vDto.Color.Trim(),
                        Sku = vDto.Sku.Trim().ToUpper(),
                        CodigoBarras = string.IsNullOrWhiteSpace(vDto.CodigoBarras) ? null : vDto.CodigoBarras.Trim(),
                        PrecioCompra = vDto.PrecioCompra,
                        PrecioVenta = vDto.PrecioVenta,
                        StockActual = vDto.StockActual,
                        StockMinimo = vDto.StockMinimo,
                        StockMaximo = vDto.StockMaximo,
                        Activo = true
                    });
                }
            }

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Producto y variantes registrados exitosamente.", productoId = producto.Id });
        }

        // Agregar variante individual a un producto existente
        [HttpPost("variante")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AgregarVariante([FromBody] VarianteCreateDto dto)
        {
            if (!dto.ProductoId.HasValue || dto.ProductoId.Value <= 0)
                return BadRequest(new { mensaje = "Debe especificar el ID del producto base." });

            var producto = await _context.Productos.FindAsync(dto.ProductoId.Value);
            if (producto == null) return NotFound(new { mensaje = "Producto base no encontrado." });

            var skuExiste = await _context.Variantes.AnyAsync(v => v.Sku.ToLower() == dto.Sku.Trim().ToLower());
            if (skuExiste) return BadRequest(new { mensaje = $"El SKU '{dto.Sku}' ya existe." });

            var variante = new VarianteProducto
            {
                ProductoId = dto.ProductoId.Value,
                Talla = dto.Talla.Trim(),
                Marca = dto.Marca.Trim(),
                Color = dto.Color.Trim(),
                Sku = dto.Sku.Trim().ToUpper(),
                CodigoBarras = string.IsNullOrWhiteSpace(dto.CodigoBarras) ? null : dto.CodigoBarras.Trim(),
                PrecioCompra = dto.PrecioCompra,
                PrecioVenta = dto.PrecioVenta,
                StockActual = dto.StockActual,
                StockMinimo = dto.StockMinimo,
                StockMaximo = dto.StockMaximo,
                Activo = true
            };

            _context.Variantes.Add(variante);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Variante agregada correctamente.", varianteId = variante.Id });
        }

        // Obtener detalle completo de un producto con sus variantes y entradas recientes
        [HttpGet("producto/{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetProducto(int id)
        {
            var producto = await _context.Productos
                .Include(p => p.Variantes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (producto == null) return NotFound(new { mensaje = "Producto no encontrado." });

            var varianteIds = producto.Variantes.Select(v => v.Id).ToList();
            var entradasRecientes = await _context.EntradasInventario
                .Include(e => e.Usuario)
                .Include(e => e.VarianteProducto)
                .Where(e => varianteIds.Contains(e.VarianteProductoId))
                .OrderByDescending(e => e.Fecha)
                .Take(20)
                .Select(e => new
                {
                    e.Id,
                    e.VarianteProductoId,
                    Sku = e.VarianteProducto != null ? e.VarianteProducto.Sku : "",
                    Talla = e.VarianteProducto != null ? e.VarianteProducto.Talla : "",
                    Color = e.VarianteProducto != null ? e.VarianteProducto.Color : "",
                    e.Cantidad,
                    e.StockAnterior,
                    e.StockNuevo,
                    e.Fecha,
                    e.Motivo,
                    Responsable = e.Usuario != null ? e.Usuario.NombreCompleto : "Usuario"
                })
                .ToListAsync();

            return Ok(new
            {
                producto.Id,
                producto.CodigoBase,
                producto.Nombre,
                producto.Descripcion,
                producto.Categoria,
                producto.ImagenUrl,
                producto.Activo,
                producto.FechaCreacion,
                TotalVariantes = producto.Variantes.Count,
                StockTotal = producto.Variantes.Sum(v => v.StockActual),
                Variantes = producto.Variantes.OrderBy(v => v.Talla).Select(v => new
                {
                    v.Id,
                    v.Talla,
                    v.Marca,
                    v.Color,
                    v.Sku,
                    v.CodigoBarras,
                    v.PrecioCompra,
                    v.PrecioVenta,
                    v.StockActual,
                    v.StockMinimo,
                    v.StockMaximo,
                    v.Activo,
                    v.RequiereReabastecimiento,
                    v.UnidadesSugeridasReabastecimiento
                }).ToList(),
                EntradasRecientes = entradasRecientes
            });
        }

        // Actualizar datos generales de un producto
        [HttpPut("producto/{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ActualizarProducto(int id, [FromForm] ProductoUpdateDto dto)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound(new { mensaje = "Producto no encontrado." });

            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return BadRequest(new { mensaje = "El nombre del producto no puede estar vacío." });

            producto.Nombre = dto.Nombre.Trim();
            producto.Categoria = string.IsNullOrWhiteSpace(dto.Categoria) ? producto.Categoria : dto.Categoria.Trim();
            producto.Descripcion = dto.Descripcion?.Trim();
            producto.ImagenUrl = await GuardarImagenProducto(dto.Imagen, producto.ImagenUrl);
            producto.Activo = dto.Activo;

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Datos del producto actualizados correctamente.", productoId = producto.Id });
        }

        // Eliminar producto base y sus variantes en cascada (si no tiene ventas)
        [HttpPost("producto/{id}/eliminar")]
        [HttpPost("eliminar/{id}")]
        [HttpDelete("producto/{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var producto = await _context.Productos
                .Include(p => p.Variantes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (producto == null)
            {
                return NotFound(new { mensaje = "El producto no fue encontrado o ya ha sido eliminado." });
            }

            var varianteIds = producto.Variantes.Select(v => v.Id).ToList();

            // Verificar si el producto o sus variantes tienen ventas registradas (integridad relacional y trazabilidad)
            var tieneVentas = await _context.DetallesVenta
                .AnyAsync(dv => varianteIds.Contains(dv.VarianteProductoId));

            if (tieneVentas)
            {
                return BadRequest(new
                {
                    mensaje = $"No es posible eliminar el producto '{producto.Nombre}' por trazabilidad e integridad relacional, ya que cuenta con facturas o movimientos de ventas activos en el sistema."
                });
            }

            // Verificar si el producto o sus variantes tienen compras registradas
            var tieneCompras = await _context.DetallesCompra
                .AnyAsync(dc => varianteIds.Contains(dc.VarianteProductoId));

            if (tieneCompras)
            {
                return BadRequest(new
                {
                    mensaje = $"No es posible eliminar el producto '{producto.Nombre}' por trazabilidad e integridad relacional, ya que cuenta con órdenes de compra registradas en el sistema."
                });
            }

            // Eliminar auditorías de entrada de stock asociadas si las hay
            var entradas = await _context.EntradasInventario
                .Where(e => varianteIds.Contains(e.VarianteProductoId))
                .ToListAsync();

            if (entradas.Any())
            {
                _context.EntradasInventario.RemoveRange(entradas);
            }

            // Si tiene imagen física cargada en /uploads/productos/, eliminarla
            if (!string.IsNullOrWhiteSpace(producto.ImagenUrl) && producto.ImagenUrl.StartsWith("/uploads/productos/", StringComparison.OrdinalIgnoreCase))
            {
                var rutaAnterior = Path.Combine(_environment.WebRootPath, producto.ImagenUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(rutaAnterior))
                {
                    try { System.IO.File.Delete(rutaAnterior); } catch { }
                }
            }

            // Eliminar variantes asociadas en cascada y el producto base
            _context.Variantes.RemoveRange(producto.Variantes);
            _context.Productos.Remove(producto);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = $"El producto base '{producto.Nombre}' y sus variantes fueron eliminados exitosamente del sistema."
            });
        }

        // Actualizar variante (precios y niveles min/max)
        [HttpPut("variante/{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ActualizarVariante(int id, [FromBody] VarianteUpdateDto dto)
        {
            var variante = await _context.Variantes.FindAsync(id);
            if (variante == null) return NotFound(new { mensaje = "Variante no encontrada." });

            if (dto.PrecioVenta < 0 || dto.PrecioCompra < 0)
                return BadRequest(new { mensaje = "Los precios no pueden ser negativos." });

            if (dto.StockMinimo < 0 || dto.StockMaximo < dto.StockMinimo)
                return BadRequest(new { mensaje = "El stock máximo debe ser mayor o igual al stock mínimo." });

            variante.PrecioCompra = dto.PrecioCompra;
            variante.PrecioVenta = dto.PrecioVenta;
            variante.StockMinimo = dto.StockMinimo;
            variante.StockMaximo = dto.StockMaximo;
            if (!string.IsNullOrWhiteSpace(dto.CodigoBarras))
                variante.CodigoBarras = dto.CodigoBarras.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Precios y niveles de stock de la variante actualizados exitosamente.",
                varianteId = variante.Id,
                variante.PrecioCompra,
                variante.PrecioVenta,
                variante.StockMinimo,
                variante.StockMaximo
            });
        }

        // Agregar unidades a una variante específica sumando al stock existente y registrando auditoría
        [HttpPost("variante/entrada")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> RegistrarEntradaStock([FromBody] EntradaStockDto dto)
        {
            if (dto.Cantidad <= 0)
                return BadRequest(new { mensaje = "La cantidad a ingresar debe ser un número entero mayor a 0." });

            var variante = await _context.Variantes
                .Include(v => v.Producto)
                .FirstOrDefaultAsync(v => v.Id == dto.VarianteId);

            if (variante == null) return NotFound(new { mensaje = "Variante seleccionada no encontrada." });

            // Identificar usuario responsable
            int usuarioId = 1;
            if (dto.UsuarioId.HasValue && dto.UsuarioId.Value > 0)
            {
                usuarioId = dto.UsuarioId.Value;
            }
            else
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
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

            int stockAnterior = variante.StockActual;
            variante.StockActual += dto.Cantidad;

            var entrada = new EntradaInventario
            {
                VarianteProductoId = variante.Id,
                UsuarioId = usuarioId,
                Fecha = DateTime.UtcNow,
                Cantidad = dto.Cantidad,
                StockAnterior = stockAnterior,
                StockNuevo = variante.StockActual,
                Motivo = string.IsNullOrWhiteSpace(dto.Motivo) ? "Entrada directa / reabastecimiento" : dto.Motivo.Trim()
            };

            _context.EntradasInventario.Add(entrada);
            await _context.SaveChangesAsync();

            await _context.Entry(entrada).Reference(e => e.Usuario).LoadAsync();

            return Ok(new
            {
                mensaje = $"Se sumaron +{dto.Cantidad} unidades al stock de {variante.Talla} / {variante.Color} (SKU: {variante.Sku}). Nuevo stock disponible: {variante.StockActual} unidades.",
                varianteId = variante.Id,
                stockAnterior,
                nuevoStock = variante.StockActual,
                cantidadSumada = dto.Cantidad,
                responsable = entrada.Usuario?.NombreCompleto ?? "Usuario",
                fecha = entrada.Fecha,
                motivo = entrada.Motivo
            });
        }

        // Ajuste manual de stock
        [HttpPut("variante/{id}/stock")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ActualizarStock(int id, [FromBody] int nuevoStock)
        {
            var variante = await _context.Variantes.FindAsync(id);
            if (variante == null) return NotFound(new { mensaje = "Variante no encontrada." });

            variante.StockActual = Math.Max(0, nuevoStock);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Stock actualizado exitosamente.", nuevoStock = variante.StockActual });
        }

        // Actualizar imagen de un producto base
        [HttpPut("producto/{id}/imagen")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ActualizarImagen(int id, [FromBody] ActualizarImagenDto dto)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound(new { mensaje = "Producto base no encontrado." });

            producto.ImagenUrl = string.IsNullOrWhiteSpace(dto.ImagenUrl) ? null : dto.ImagenUrl.Trim();
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Imagen actualizada exitosamente.", imagenUrl = producto.ImagenUrl });
        }

        private async Task<string?> GuardarImagenProducto(IFormFile? imagen, string? imagenActual)
        {
            if (imagen == null || imagen.Length == 0)
                return imagenActual;

            if (imagen.Length > 5 * 1024 * 1024)
                throw new InvalidOperationException("La imagen no puede superar los 5 MB.");

            var extensionPermitida = Path.GetExtension(imagen.FileName).ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extensionPermitida))
                throw new InvalidOperationException("Solo se permiten imágenes JPG, PNG o WEBP.");

            var carpeta = Path.Combine(_environment.WebRootPath, "uploads", "productos");
            Directory.CreateDirectory(carpeta);
            var nombreArchivo = $"{Guid.NewGuid():N}{extensionPermitida}";
            var rutaFisica = Path.Combine(carpeta, nombreArchivo);

            await using var stream = System.IO.File.Create(rutaFisica);
            await imagen.CopyToAsync(stream);

            if (!string.IsNullOrWhiteSpace(imagenActual) && imagenActual.StartsWith("/uploads/productos/", StringComparison.OrdinalIgnoreCase))
            {
                var anterior = Path.Combine(_environment.WebRootPath, imagenActual.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(anterior))
                    System.IO.File.Delete(anterior);
            }

            return $"/uploads/productos/{nombreArchivo}";
        }
    }

    public class ActualizarImagenDto
    {
        public string? ImagenUrl { get; set; }
    }
}
