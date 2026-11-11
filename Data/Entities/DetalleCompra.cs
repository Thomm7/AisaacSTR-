using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AisaacSTR.Data.Entities
{
    [Table("detalle_compras")]
    public class DetalleCompra
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("compra_id")]
        public int CompraId { get; set; }

        [Required]
        [Column("variante_producto_id")]
        public int VarianteProductoId { get; set; }

        [Required]
        [Column("cantidad")]
        public int Cantidad { get; set; }

        [Column("costo_unitario", TypeName = "numeric(12,2)")]
        public decimal CostoUnitario { get; set; }

        [Column("subtotal", TypeName = "numeric(12,2)")]
        public decimal Subtotal { get; set; }

        // Navegación
        [ForeignKey("CompraId")]
        public CompraProveedor? Compra { get; set; }

        [ForeignKey("VarianteProductoId")]
        public VarianteProducto? VarianteProducto { get; set; }
    }
}
