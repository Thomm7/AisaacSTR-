using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AisaacSTR.Data.Entities
{
    [Table("usuarios")]
    public class Usuario
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("nombre_completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("rol")]
        public string Rol { get; set; } = "Cajero"; // "Administrador" o "Cajero"

        [MaxLength(30)]
        [Column("telefono")]
        public string? Telefono { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("fecha_creacion", TypeName = "timestamp with time zone")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navegación
        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
        public ICollection<CierreCaja> CierresCaja { get; set; } = new List<CierreCaja>();
        public ICollection<CompraProveedor> Compras { get; set; } = new List<CompraProveedor>();
    }
}
