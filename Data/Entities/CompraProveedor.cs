using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AisaacSTR.Data.Entities
{
    [Table("compras_proveedor")]
    public class CompraProveedor
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("numero_factura")]
        public string NumeroFactura { get; set; } = string.Empty;

        [Required]
        [Column("proveedor_id")]
        public int ProveedorId { get; set; }

        [Required]
        [Column("usuario_id")]
        public int UsuarioId { get; set; }

        [Column("fecha", TypeName = "timestamp with time zone")]
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        [Column("total", TypeName = "numeric(12,2)")]
        public decimal Total { get; set; }

        [Required]
        [MaxLength(30)]
        [Column("estado")]
        public string Estado { get; set; } = "Registrada"; // "Registrada", "Cancelada"

        [Required]
        [MaxLength(50)]
        [Column("medio_pago")]
        public string MedioPago { get; set; } = "Efectivo"; // "Efectivo", "Transferencia", "Credito"

        [Column("descontar_caja")]
        public bool DescontarCaja { get; set; } = true; // Si se paga con dinero de caja activa

        [Column("cierre_caja_id")]
        public int? CierreCajaId { get; set; }

        [MaxLength(500)]
        [Column("observaciones")]
        public string? Observaciones { get; set; }

        // Navegación
        [ForeignKey("ProveedorId")]
        public Proveedor? Proveedor { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        [ForeignKey("CierreCajaId")]
        public CierreCaja? CierreCaja { get; set; }

        public ICollection<DetalleCompra> Detalles { get; set; } = new List<DetalleCompra>();
    }
}
