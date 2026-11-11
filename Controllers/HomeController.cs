using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AisaacSTR.Data;
using AisaacSTR.Models;

namespace AisaacSTR.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // Datos de ejemplo (en producción vendrían de BD)
        private static readonly List<Producto> _catalogo = new()
        {
            new() { Codigo = "CAM-001", Nombre = "Camisa Oxford",    Variante = "M / Blanco",  Precio = 89300,  Stock = 12, Talla = "M",  Color = "Blanco", Marca = "Arturo Calle" },
            new() { Codigo = "PAN-007", Nombre = "Pantalón Chino",   Variante = "32 / Khaki",  Precio = 145300, Stock = 8,  Talla = "32", Color = "Khaki",  Marca = "Arturo Calle" },
            new() { Codigo = "ZAP-016", Nombre = "Zapato Cuero",     Variante = "42 / Negro",  Precio = 320000, Stock = 5,  Talla = "42", Color = "Negro",  Marca = "Arturo Calle" },
            new() { Codigo = "CIN-004", Nombre = "Cinturón Tejido",  Variante = "Único / Café",Precio = 55300,  Stock = 20, Talla = "Único",Color="Café",   Marca = "Arturo Calle" },
            new() { Codigo = "CAM-012", Nombre = "Camisa Lino",      Variante = "L / Azul",    Precio = 95000,  Stock = 7,  Talla = "L",  Color = "Azul",   Marca = "Arturo Calle" },
            new() { Codigo = "PAN-015", Nombre = "Pantalón Dress",   Variante = "34 / Negro",  Precio = 165000, Stock = 6,  Talla = "34", Color = "Negro",  Marca = "Arturo Calle" },
        };

        private static readonly List<Venta> _ventas = new()
        {
            new() {
                Id = 1,
                Fecha = DateTime.Today,
                MetodoPago = MetodoPago.Efectivo,
                Items = new()
                {
                    new() { Codigo="CAM-001", Producto="Camisa Oxford",   Variante="M / Blanco",  Cantidad=1, Precio=89300  },
                    new() { Codigo="PAN-007", Producto="Pantalón Chino",  Variante="32 / Khaki",  Cantidad=1, Precio=145300 },
                    new() { Codigo="ZAP-016", Producto="Zapato Cuero",    Variante="42 / Negro",  Cantidad=1, Precio=320000 },
                    new() { Codigo="CIN-004", Producto="Cinturón Tejido", Variante="Único / Café", Cantidad=2, Precio=55300  },
                }
            }
        };

        public async Task<IActionResult> Index([FromQuery] int pagina = 1)
        {
            if (pagina < 1) pagina = 1;
            const int limite = 20;

            var totalRegistros = await _context.Productos.CountAsync(p => p.Activo);
            var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)limite);
            if (totalPaginas < 1) totalPaginas = 1;

            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalRegistros = totalRegistros;
            ViewBag.Catalogo = _catalogo;
            return View();
        }

        [HttpGet]
        public IActionResult ObtenerCatalogo(string? busqueda)
        {
            var resultado = _catalogo.AsQueryable();
            if (!string.IsNullOrEmpty(busqueda))
                resultado = resultado.Where(p =>
                    p.Codigo.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                    p.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase));

            return Json(resultado.ToList());
        }

        [HttpGet]
        public IActionResult ObtenerCierreCaja()
        {
            var ventasHoy = _ventas.Where(v => v.Fecha.Date == DateTime.Today).ToList();

            var cierre = new CierreCaja
            {
                TotalEfectivo       = ventasHoy.Where(v => v.MetodoPago == MetodoPago.Efectivo).Sum(v => v.Total),
                TotalTransferencias = ventasHoy.Where(v => v.MetodoPago == MetodoPago.Transferencia).Sum(v => v.Total),
                TotalCredito        = ventasHoy.Where(v => v.MetodoPago == MetodoPago.Credito).Sum(v => v.Total),
            };

            // Para demo usamos datos fijos similares al mockup
            cierre.TotalEfectivo       = 1850000;
            cierre.TotalTransferencias = 2340000;
            cierre.TotalCredito        = 980000;

            return Json(cierre);
        }
    }
}
