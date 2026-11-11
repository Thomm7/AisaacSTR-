using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AisaacSTR.Data.Entities
{
    [Table("entradas_inventario")]
    public class EntradaInventario
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("variante_producto_id")]
        public int VarianteProductoId { get; set; }

        [Required]
        [Column("usuario_id")]
        public int UsuarioId { get; set; }

        [Column("fecha", TypeName = "timestamp with time zone")]
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        [Column("cantidad")]
        public int Cantidad { get; set; }

        [Column("stock_anterior")]
        public int StockAnterior { get; set; }

        [Column("stock_nuevo")]
        public int StockNuevo { get; set; }

        [MaxLength(250)]
        [Column("motivo")]
        public string? Motivo { get; set; }

        // Navegación
        [ForeignKey("VarianteProductoId")]
        public VarianteProducto? VarianteProducto { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }
    }
}

