using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AisaacSTR.Data.Entities
{
    [Table("medios_pago")]
    public class MedioPago
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        [Column("codigo")]
        public string Codigo { get; set; } = string.Empty; // "EFECTIVO", "TRANSFERENCIA", "SISTECREDITO", "ADDI", "TARJETA"

        [Required]
        [MaxLength(60)]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        [Column("categoria_flujo")]
        public string CategoriaFlujo { get; set; } = "Efectivo"; // "Efectivo", "Transferencia", "Credito"

        [MaxLength(100)]
        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("requiere_referencia")]
        public bool RequiereReferencia { get; set; } = false;

        [Column("activo")]
        public bool Activo { get; set; } = true;

        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
    }
}
