namespace AisaacSTR.Models
{
    public class CierreCaja
    {
        public DateTime Fecha { get; set; } = DateTime.Today;
        public decimal TotalEfectivo { get; set; }
        public decimal TotalTransferencias { get; set; }
        public decimal TotalCredito { get; set; }
        public decimal VentaTotalDia => TotalEfectivo + TotalTransferencias + TotalCredito;
        public decimal EfectivoContadoFisico { get; set; }
        public decimal Diferencia => EfectivoContadoFisico - TotalEfectivo;
        public bool Cerrado { get; set; } = false;
    }
}
