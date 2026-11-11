using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AisaacSTR.Data.Entities
{
    [Table("cierres_caja")]
    public class CierreCaja
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("usuario_id")]
        public int UsuarioId { get; set; }

        [Column("fecha_apertura", TypeName = "timestamp with time zone")]
        public DateTime FechaApertura { get; set; } = DateTime.UtcNow;

        [Column("fecha_cierre", TypeName = "timestamp with time zone")]
        public DateTime? FechaCierre { get; set; }

        [Column("monto_inicial", TypeName = "numeric(12,2)")]
        public decimal MontoInicial { get; set; }

        [Column("total_ventas_efectivo", TypeName = "numeric(12,2)")]
        public decimal TotalVentasEfectivo { get; set; }

        [Column("total_ventas_transferencia", TypeName = "numeric(12,2)")]
        public decimal TotalVentasTransferencia { get; set; }

        [Column("total_ventas_credito", TypeName = "numeric(12,2)")]
        public decimal TotalVentasCredito { get; set; }

        [Column("total_pagos_proveedores_efectivo", TypeName = "numeric(12,2)")]
        public decimal TotalPagosProveedoresEfectivo { get; set; } = 0; // Egresos por compras en efectivo

        [Column("total_esperado", TypeName = "numeric(12,2)")]
        public decimal TotalEsperado { get; set; } // Efectivo esperado (MontoInicial + TotalVentasEfectivo - TotalPagosProveedoresEfectivo)

        [Column("total_real_contado", TypeName = "numeric(12,2)")]
        public decimal TotalRealContado { get; set; }

        [Column("diferencia", TypeName = "numeric(12,2)")]
        public decimal Diferencia { get; set; } // TotalRealContado - TotalEsperado

        [MaxLength(500)]
        [Column("observaciones")]
        public string? Observaciones { get; set; }

        [Required]
        [MaxLength(30)]
        [Column("estado")]
        public string Estado { get; set; } = "Cerrada"; // "Abierta", "Cerrada"

        // Navegación
        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
        public ICollection<CompraProveedor> Compras { get; set; } = new List<CompraProveedor>();
    }
}
