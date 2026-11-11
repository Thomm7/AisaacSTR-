using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AisaacSTR.Data.Entities
{
    [Table("ventas")]
    public class Venta
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("numero_factura")]
        public string NumeroFactura { get; set; } = string.Empty;

        [Column("fecha", TypeName = "timestamp with time zone")]
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("usuario_id")]
        public int UsuarioId { get; set; }

        [Required]
        [Column("medio_pago_id")]
        public int MedioPagoId { get; set; }

        [MaxLength(100)]
        [Column("referencia_pago")]
        public string? ReferenciaPago { get; set; } // Ej: Nro transacción Nequi o ref Sistecrédito

        [MaxLength(50)]
        [Column("banco")]
        public string? Banco { get; set; }

        [MaxLength(100)]
        [Column("codigo_aprobacion")]
        public string? CodigoAprobacion { get; set; }

        [Column("efectivo_recibido", TypeName = "numeric(12,2)")]
        public decimal EfectivoRecibido { get; set; }

        [Column("cambio_entregado", TypeName = "numeric(12,2)")]
        public decimal CambioEntregado { get; set; }

        [Column("subtotal", TypeName = "numeric(12,2)")]
        public decimal Subtotal { get; set; }

        [Column("descuento", TypeName = "numeric(12,2)")]
        public decimal Descuento { get; set; }

        [Column("iva", TypeName = "numeric(12,2)")]
        public decimal Iva { get; set; }

        [Column("total", TypeName = "numeric(12,2)")]
        public decimal Total { get; set; }

        [Required]
        [MaxLength(30)]
        [Column("estado")]
        public string Estado { get; set; } = "Completada"; // "Completada", "Anulada"

        [Column("cierre_caja_id")]
        public int? CierreCajaId { get; set; }

        // Navegación
        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        [ForeignKey("MedioPagoId")]
        public MedioPago? MedioPago { get; set; }

        [ForeignKey("CierreCajaId")]
        public CierreCaja? CierreCaja { get; set; }

        public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
    }
}
