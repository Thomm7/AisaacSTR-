using Microsoft.EntityFrameworkCore;
using AisaacSTR.Data.Entities;

namespace AisaacSTR.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<VarianteProducto> Variantes => Set<VarianteProducto>();
        public DbSet<MedioPago> MediosPago => Set<MedioPago>();
        public DbSet<Venta> Ventas => Set<Venta>();
        public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
        public DbSet<CierreCaja> CierresCaja => Set<CierreCaja>();
        public DbSet<Proveedor> Proveedores => Set<Proveedor>();
        public DbSet<CompraProveedor> Compras => Set<CompraProveedor>();
        public DbSet<DetalleCompra> DetallesCompra => Set<DetalleCompra>();
        public DbSet<EntradaInventario> EntradasInventario => Set<EntradaInventario>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Usuario ──────────────────────────
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Rol).HasDefaultValue("Cajero");
                entity.Property(u => u.Activo).HasDefaultValue(true);
            });

            // ── Producto ─────────────────────────
            modelBuilder.Entity<Producto>(entity =>
            {
                entity.HasIndex(p => p.CodigoBase).IsUnique();
                entity.Property(p => p.Activo).HasDefaultValue(true);
            });

            // ── VarianteProducto ─────────────────
            modelBuilder.Entity<VarianteProducto>(entity =>
            {
                entity.HasIndex(v => v.Sku).IsUnique();
                entity.HasIndex(v => v.CodigoBarras);
                entity.HasIndex(v => new { v.ProductoId, v.Talla, v.Color, v.Marca });

                entity.Property(v => v.PrecioCompra).HasPrecision(12, 2);
                entity.Property(v => v.PrecioVenta).HasPrecision(12, 2);
                entity.Property(v => v.Activo).HasDefaultValue(true);

                entity.HasOne(v => v.Producto)
                      .WithMany(p => p.Variantes)
                      .HasForeignKey(v => v.ProductoId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── MedioPago ────────────────────────
            modelBuilder.Entity<MedioPago>(entity =>
            {
                entity.HasIndex(m => m.Codigo).IsUnique();
                entity.Property(m => m.Activo).HasDefaultValue(true);
            });

            // ── Venta ────────────────────────────
            modelBuilder.Entity<Venta>(entity =>
            {
                entity.HasIndex(v => v.NumeroFactura).IsUnique();
                entity.HasIndex(v => v.Fecha);

                entity.Property(v => v.Subtotal).HasPrecision(12, 2);
                entity.Property(v => v.Descuento).HasPrecision(12, 2);
                entity.Property(v => v.Iva).HasPrecision(12, 2);
                entity.Property(v => v.Total).HasPrecision(12, 2);

                entity.HasOne(v => v.Usuario)
                      .WithMany(u => u.Ventas)
                      .HasForeignKey(v => v.UsuarioId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.MedioPago)
                      .WithMany(m => m.Ventas)
                      .HasForeignKey(v => v.MedioPagoId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.CierreCaja)
                      .WithMany(c => c.Ventas)
                      .HasForeignKey(v => v.CierreCajaId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ── DetalleVenta ─────────────────────
            modelBuilder.Entity<DetalleVenta>(entity =>
            {
                entity.Property(d => d.PrecioUnitario).HasPrecision(12, 2);
                entity.Property(d => d.Subtotal).HasPrecision(12, 2);

                entity.HasOne(d => d.Venta)
                      .WithMany(v => v.Detalles)
                      .HasForeignKey(d => d.VentaId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.VarianteProducto)
                      .WithMany(vp => vp.DetallesVenta)
                      .HasForeignKey(d => d.VarianteProductoId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── CierreCaja ───────────────────────
            modelBuilder.Entity<CierreCaja>(entity =>
            {
                entity.HasIndex(c => c.FechaApertura);

                entity.Property(c => c.MontoInicial).HasPrecision(12, 2);
                entity.Property(c => c.TotalVentasEfectivo).HasPrecision(12, 2);
                entity.Property(c => c.TotalVentasTransferencia).HasPrecision(12, 2);
                entity.Property(c => c.TotalVentasCredito).HasPrecision(12, 2);
                entity.Property(c => c.TotalPagosProveedoresEfectivo).HasPrecision(12, 2);
                entity.Property(c => c.TotalEsperado).HasPrecision(12, 2);
                entity.Property(c => c.TotalRealContado).HasPrecision(12, 2);
                entity.Property(c => c.Diferencia).HasPrecision(12, 2);

                entity.HasOne(c => c.Usuario)
                      .WithMany(u => u.CierresCaja)
                      .HasForeignKey(c => c.UsuarioId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Proveedor ────────────────────────
            modelBuilder.Entity<Proveedor>(entity =>
            {
                entity.HasIndex(p => p.DocumentoNit).IsUnique();
                entity.Property(p => p.Activo).HasDefaultValue(true);
            });

            // ── CompraProveedor ──────────────────
            modelBuilder.Entity<CompraProveedor>(entity =>
            {
                entity.Property(c => c.Total).HasPrecision(12, 2);

                entity.HasOne(c => c.Proveedor)
                      .WithMany(p => p.Compras)
                      .HasForeignKey(c => c.ProveedorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Usuario)
                      .WithMany(u => u.Compras)
                      .HasForeignKey(c => c.UsuarioId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.CierreCaja)
                      .WithMany(cc => cc.Compras)
                      .HasForeignKey(c => c.CierreCajaId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ── DetalleCompra ────────────────────
            modelBuilder.Entity<DetalleCompra>(entity =>
            {
                entity.Property(dc => dc.CostoUnitario).HasPrecision(12, 2);
                entity.Property(dc => dc.Subtotal).HasPrecision(12, 2);

                entity.HasOne(dc => dc.Compra)
                      .WithMany(c => c.Detalles)
                      .HasForeignKey(dc => dc.CompraId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(dc => dc.VarianteProducto)
                      .WithMany(vp => vp.DetallesCompra)
                      .HasForeignKey(dc => dc.VarianteProductoId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
