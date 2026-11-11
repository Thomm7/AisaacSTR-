using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AisaacSTR.Data.Entities
{
    [Table("proveedores")]
    public class Proveedor
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        [Column("documento_nit")]
        public string DocumentoNit { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Column("razon_social")]
        public string RazonSocial { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("contacto")]
        public string? Contacto { get; set; }

        [MaxLength(30)]
        [Column("telefono")]
        public string? Telefono { get; set; }

        [MaxLength(100)]
        [Column("email")]
        public string? Email { get; set; }

        [MaxLength(200)]
        [Column("direccion")]
        public string? Direccion { get; set; }

        [MaxLength(60)]
        [Column("ciudad")]
        public string Ciudad { get; set; } = "Neiva";

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("fecha_registro", TypeName = "timestamp with time zone")]
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Navegación
        public ICollection<CompraProveedor> Compras { get; set; } = new List<CompraProveedor>();
    }
}
