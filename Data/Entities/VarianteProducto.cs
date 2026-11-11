using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AisaacSTR.Data.Entities
{
    [Table("variante_productos")]
    public class VarianteProducto
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("producto_id")]
        public int ProductoId { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("talla")]
        public string Talla { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("marca")]
        public string Marca { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        [Column("color")]
        public string Color { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        [Column("sku")]
        public string Sku { get; set; } = string.Empty;

        [MaxLength(60)]
        [Column("codigo_barras")]
        public string? CodigoBarras { get; set; }

        [Column("precio_compra", TypeName = "numeric(12,2)")]
        public decimal PrecioCompra { get; set; }

        [Column("precio_venta", TypeName = "numeric(12,2)")]
        public decimal PrecioVenta { get; set; }

        [Column("stock_actual")]
        public int StockActual { get; set; }

        [Column("stock_minimo")]
        public int StockMinimo { get; set; } = 5;

        [Column("stock_maximo")]
        public int StockMaximo { get; set; } = 50;

        [Column("activo")]
        public bool Activo { get; set; } = true;

        // Propiedad calculada útil para sugerencia de reabastecimiento:
        [NotMapped]
        public bool RequiereReabastecimiento => StockActual <= StockMinimo;

        [NotMapped]
        public int UnidadesSugeridasReabastecimiento => Math.Max(0, StockMaximo - StockActual);

        // Navegación
        [ForeignKey("ProductoId")]
        public Producto? Producto { get; set; }

        public ICollection<DetalleVenta> DetallesVenta { get; set; } = new List<DetalleVenta>();
        public ICollection<DetalleCompra> DetallesCompra { get; set; } = new List<DetalleCompra>();
    }
}
