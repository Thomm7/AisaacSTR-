using Microsoft.AspNetCore.Http;

namespace AisaacSTR.DTOs
{
    // ── Productos y Variantes ────────────────────
    public class ProductoCreateDto
    {
        public string CodigoBase { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string Categoria { get; set; } = "Prendas de Vestir";
        public string? ImagenUrl { get; set; }
        public IFormFile? Imagen { get; set; }
        public List<VarianteCreateDto> Variantes { get; set; } = new();
    }

    public class ProductoUpdateDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = "Prendas de Vestir";
        public string? Descripcion { get; set; }
        public string? ImagenUrl { get; set; }
        public IFormFile? Imagen { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class VarianteUpdateDto
    {
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public int StockMinimo { get; set; } = 5;
        public int StockMaximo { get; set; } = 50;
        public string? CodigoBarras { get; set; }
    }

    public class EntradaStockDto
    {
        public int VarianteId { get; set; }
        public int Cantidad { get; set; }
        public int? UsuarioId { get; set; }
        public string? Motivo { get; set; }
    }

    public class VarianteCreateDto
    {
        public int? ProductoId { get; set; }
        public string Talla { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string? CodigoBarras { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public int StockActual { get; set; }
        public int StockMinimo { get; set; } = 5;
        public int StockMaximo { get; set; } = 50;
    }

    // ── Proveedores ──────────────────────────────
    public class ProveedorCreateDto
    {
        public string DocumentoNit { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string? Contacto { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public string Ciudad { get; set; } = "Neiva";
    }

    // ── Compras a Proveedores ────────────────────
    public class ItemCompraDto
    {
        public int VarianteProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
    }

    public class CompraCreateDto
    {
        public int ProveedorId { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public string MedioPago { get; set; } = "Efectivo"; // "Efectivo", "Transferencia", "Credito"
        public bool DescontarCaja { get; set; } = true;
        public List<ItemCompraDto> Items { get; set; } = new();
    }

    // ── Ventas POS ───────────────────────────────
    public class ItemVentaDto
    {
        public int VarianteProductoId { get; set; }
        public int Cantidad { get; set; }
    }

    public class VentaCreateDto
    {
        public int MedioPagoId { get; set; }
        public string? ReferenciaPago { get; set; }
        public string? Banco { get; set; }
        public string? CodigoAprobacion { get; set; }
        public decimal EfectivoRecibido { get; set; }
        public decimal CambioEntregado { get; set; }
        public decimal Descuento { get; set; } = 0;
        public List<ItemVentaDto> Items { get; set; } = new();
    }

    // ── Cierre de Caja ───────────────────────────
    public class CajaAperturaDto
    {
        public int? UsuarioId { get; set; }
        public decimal MontoInicial { get; set; } = 0;
        public string? Observaciones { get; set; }
    }

    public class CierreCajaCreateDto
    {
        public int? UsuarioId { get; set; }
        public decimal MontoInicial { get; set; } = 0;
        public decimal EfectivoContadoFisico { get; set; }
        public string? Observaciones { get; set; }
    }
}
