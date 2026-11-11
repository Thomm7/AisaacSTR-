namespace AisaacSTR.Models
{
    public class ItemCarrito
    {
        public string Codigo { get; set; } = string.Empty;
        public string Producto { get; set; } = string.Empty;
        public string Variante { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Subtotal => Precio * Cantidad;
    }

    public enum MetodoPago
    {
        Efectivo,
        Transferencia,
        Credito
    }

    public class Venta
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public List<ItemCarrito> Items { get; set; } = new();
        public MetodoPago MetodoPago { get; set; }
        public decimal Subtotal => Items.Sum(i => i.Subtotal);
        public decimal Descuentos { get; set; } = 0;
        public decimal Iva => (Subtotal - Descuentos) * 0.19m;
        public decimal Total => Subtotal - Descuentos + Iva;
    }
}
