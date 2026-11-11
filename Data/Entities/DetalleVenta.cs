using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AisaacSTR.Data.Entities
{
    [Table("detalle_ventas")]
    public class DetalleVenta
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("venta_id")]
        public int VentaId { get; set; }

        [Required]
        [Column("variante_producto_id")]
        public int VarianteProductoId { get; set; }

        [Required]
        [Column("cantidad")]
        public int Cantidad { get; set; }

        [Column("precio_unitario", TypeName = "numeric(12,2)")]
        public decimal PrecioUnitario { get; set; }

        [Column("subtotal", TypeName = "numeric(12,2)")]
        public decimal Subtotal { get; set; }

        // Navegación
        [ForeignKey("VentaId")]
        public Venta? Venta { get; set; }

        [ForeignKey("VarianteProductoId")]
        public VarianteProducto? VarianteProducto { get; set; }
    }
}
