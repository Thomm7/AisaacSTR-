namespace AisaacSTR.Models
{
    public class Producto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Variante { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public string Talla { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
    }
}
