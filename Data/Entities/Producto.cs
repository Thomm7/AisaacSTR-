using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AisaacSTR.Data.Entities
{
    [Table("productos")]
    public class Producto
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("codigo_base")]
        public string CodigoBase { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500)]
        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [MaxLength(80)]
        [Column("categoria")]
        public string Categoria { get; set; } = "Prendas de Vestir";

        [Column("imagen_url")]
        public string? ImagenUrl { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("fecha_creacion", TypeName = "timestamp with time zone")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navegación: un producto base tiene múltiples variantes
        public ICollection<VarianteProducto> Variantes { get; set; } = new List<VarianteProducto>();
    }
}
