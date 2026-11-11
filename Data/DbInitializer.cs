using AisaacSTR.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AisaacSTR.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            // Asegurar que las tablas existan
            context.Database.EnsureCreated();

            if (context.Database.IsRelational())
            {
                try
                {
                    context.Database.ExecuteSqlRaw(@"
                        ALTER TABLE cierres_caja ADD COLUMN IF NOT EXISTS total_pagos_proveedores_efectivo NUMERIC(12,2) NOT NULL DEFAULT 0.00;
                        ALTER TABLE compras_proveedor ADD COLUMN IF NOT EXISTS medio_pago VARCHAR(50) NOT NULL DEFAULT 'Efectivo';
                        ALTER TABLE compras_proveedor ADD COLUMN IF NOT EXISTS descontar_caja BOOLEAN NOT NULL DEFAULT TRUE;
                        ALTER TABLE compras_proveedor ADD COLUMN IF NOT EXISTS cierre_caja_id INTEGER REFERENCES cierres_caja(id) ON DELETE SET NULL;
                        ALTER TABLE productos ADD COLUMN IF NOT EXISTS imagen_url TEXT;

                        CREATE TABLE IF NOT EXISTS entradas_inventario (
                            id SERIAL PRIMARY KEY,
                            variante_producto_id INTEGER NOT NULL REFERENCES variante_productos(id) ON DELETE CASCADE,
                            usuario_id INTEGER NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
                            fecha TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                            cantidad INTEGER NOT NULL,
                            stock_anterior INTEGER NOT NULL,
                            stock_nuevo INTEGER NOT NULL,
                            motivo VARCHAR(250)
                        );
                        CREATE INDEX IF NOT EXISTS ix_entradas_inventario_fecha ON entradas_inventario(fecha);
                        CREATE INDEX IF NOT EXISTS ix_entradas_inventario_variante_id ON entradas_inventario(variante_producto_id);
                    ");
                }
                catch { /* Continuar si ya existen o es un proveedor no compatible */ }
            }

            // 1. Usuarios Semilla
            if (!context.Usuarios.Any())
            {
                var admin = new Usuario
                {
                    NombreCompleto = "Thomas Santiago Gonzalez (Gerente)",
                    Email = "admin@aisaacstr.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123*"),
                    Rol = "Administrador",
                    Telefono = "+57 312 456 7890",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                var cajero = new Usuario
                {
                    NombreCompleto = "Sharith Rincon (Cajera Principal)",
                    Email = "cajero@aisaacstr.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cajero123*"),
                    Rol = "Cajero",
                    Telefono = "+57 320 987 6543",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                context.Usuarios.AddRange(admin, cajero);
                context.SaveChanges();
            }

            // Asegurar que las contraseñas de las cuentas demo siempre tengan sus credenciales válidas
            var demoCredentials = new[]
            {
                (Email: "admin@aisaacstr.com", Password: "Admin123*"),
                (Email: "cajero@aisaacstr.com", Password: "Cajero123*")
            };

            foreach (var cred in demoCredentials)
            {
                var user = context.Usuarios.FirstOrDefault(u => u.Email.ToLower() == cred.Email.ToLower());
                if (user != null)
                {
                    bool isValid = false;
                    try
                    {
                        isValid = BCrypt.Net.BCrypt.Verify(cred.Password, user.PasswordHash);
                    }
                    catch { isValid = false; }

                    if (!isValid)
                    {
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(cred.Password);
                        user.Activo = true;
                    }
                }
            }
            context.SaveChanges();

            // 2. Medios de Pago Semilla
            if (!context.MediosPago.Any())
            {
                var medios = new List<MedioPago>
                {
                    new() { Codigo = "EFECTIVO", Nombre = "Efectivo", CategoriaFlujo = "Efectivo", Descripcion = "Pago directo en billetes y monedas", RequiereReferencia = false, Activo = true },
                    new() { Codigo = "TRANSFERENCIA", Nombre = "Transferencia Bancaria", CategoriaFlujo = "Transferencia", Descripcion = "Nequi · Daviplata · Bancolombia · PSE", RequiereReferencia = true, Activo = true },
                    new() { Codigo = "SISTECREDITO", Nombre = "Sistecrédito (Crédito)", CategoriaFlujo = "Credito", Descripcion = "Financiación de cartera vía Sistecrédito", RequiereReferencia = true, Activo = true },
                    new() { Codigo = "ADDI", Nombre = "Addi (Crédito)", CategoriaFlujo = "Credito", Descripcion = "Financiación en cuotas con Addi", RequiereReferencia = true, Activo = true },
                    new() { Codigo = "TARJETA", Nombre = "Tarjeta Débito / Crédito", CategoriaFlujo = "Transferencia", Descripcion = "Datáfono Redeban / Credibanco", RequiereReferencia = true, Activo = true }
                };
                context.MediosPago.AddRange(medios);
                context.SaveChanges();
            }

            // 3. Proveedores Semilla
            if (!context.Proveedores.Any())
            {
                var proveedores = new List<Proveedor>
                {
                    new()
                    {
                        DocumentoNit = "900.123.456-1",
                        RazonSocial = "Arturo Calle Proveedores S.A.S.",
                        Contacto = "Carlos Morales",
                        Telefono = "3104561234",
                        Email = "ventas@arturocalle.co",
                        Direccion = "Av. Calle 100 # 45-20",
                        Ciudad = "Bogotá",
                        Activo = true
                    },
                    new()
                    {
                        DocumentoNit = "891.901.234-5",
                        RazonSocial = "Confecciones del Huila S.A.S.",
                        Contacto = "Lucía Perdomo",
                        Telefono = "3158904321",
                        Email = "pedidos@confeccioneshuila.com",
                        Direccion = "Cra 5 # 12-40 Centro",
                        Ciudad = "Neiva",
                        Activo = true
                    },
                    new()
                    {
                        DocumentoNit = "800.567.890-2",
                        RazonSocial = "Calzado Rivera & Moda Ltda.",
                        Contacto = "Hernando Silva",
                        Telefono = "3187654321",
                        Email = "distribucion@calzadorivera.com",
                        Direccion = "Calle 8 # 3-15",
                        Ciudad = "Rivera",
                        Activo = true
                    },
                    new()
                    {
                        DocumentoNit = "901.345.678-9",
                        RazonSocial = "Distribuciones Textiles del Sur S.A.S.",
                        Contacto = "Mariana Vargas",
                        Telefono = "3143322110",
                        Email = "contacto@textilesdelsur.com",
                        Direccion = "Calle 20 Sur # 14-80",
                        Ciudad = "Neiva",
                        Activo = true
                    }
                };
                context.Proveedores.AddRange(proveedores);
                context.SaveChanges();
            }

            // 4. Productos Base y Variantes
            if (!context.Productos.Any())
            {
                var p1 = new Producto { CodigoBase = "CAM-OXF", Nombre = "Camisa Oxford", Descripcion = "Camisa clásica 100% algodón corte regular", Categoria = "Camisas", ImagenUrl = "https://images.unsplash.com/photo-1602810318383-e386cc2a3ccf?w=500&auto=format&fit=crop&q=80" };
                var p2 = new Producto { CodigoBase = "PAN-CHI", Nombre = "Pantalón Chino", Descripcion = "Pantalón casual gabardina stretch", Categoria = "Pantalones", ImagenUrl = "https://images.unsplash.com/photo-1473966968600-fa801b869a1a?w=500&auto=format&fit=crop&q=80" };
                var p3 = new Producto { CodigoBase = "ZAP-CUE", Nombre = "Zapato Cuero", Descripcion = "Zapato formal mocasín en puro cuero vacuno", Categoria = "Calzado", ImagenUrl = "https://images.unsplash.com/photo-1549298916-b41d501d3772?w=500&auto=format&fit=crop&q=80" };
                var p4 = new Producto { CodigoBase = "CIN-TEJ", Nombre = "Cinturón Tejido", Descripcion = "Cinturón artesanal en cuero y fibra textil", Categoria = "Accesorios", ImagenUrl = "https://images.unsplash.com/photo-1624222247344-550fb60583dc?w=500&auto=format&fit=crop&q=80" };
                var p5 = new Producto { CodigoBase = "CAM-LIN", Nombre = "Camisa Lino", Descripcion = "Camisa de lino manga larga diseño fresco", Categoria = "Camisas", ImagenUrl = "https://images.unsplash.com/photo-1596755094514-f87e34085b2c?w=500&auto=format&fit=crop&q=80" };
                var p6 = new Producto { CodigoBase = "PAN-DRE", Nombre = "Pantalón Dress", Descripcion = "Pantalón formal corte slim paño fino", Categoria = "Pantalones", ImagenUrl = "https://images.unsplash.com/photo-1594633312681-425c7b97ccd1?w=500&auto=format&fit=crop&q=80" };
                var p7 = new Producto { CodigoBase = "POL-PIM", Nombre = "Polo Pima", Descripcion = "Camiseta cuello polo en algodón Pima peinado", Categoria = "Camisas", ImagenUrl = "https://images.unsplash.com/photo-1581655353564-df123a1eb820?w=500&auto=format&fit=crop&q=80" };
                var p8 = new Producto { CodigoBase = "BER-CAS", Nombre = "Bermuda Casual Drill", Descripcion = "Bermuda estilo playero con bolsillos laterales", Categoria = "Bermudas", ImagenUrl = "https://images.unsplash.com/photo-1591195853828-11db59a44f6b?w=500&auto=format&fit=crop&q=80" };

                context.Productos.AddRange(p1, p2, p3, p4, p5, p6, p7, p8);
                context.SaveChanges();

                // Matriz de Variantes con Talla, Marca, Color, Stock, Precios
                var variantes = new List<VarianteProducto>
                {
                    // Camisa Oxford
                    new() { ProductoId = p1.Id, Talla = "M", Marca = "Arturo Calle", Color = "Blanco", Sku = "CAM-001", CodigoBarras = "770123400101", PrecioCompra = 50000, PrecioVenta = 89300, StockActual = 14, StockMinimo = 5, StockMaximo = 30 },
                    new() { ProductoId = p1.Id, Talla = "L", Marca = "Arturo Calle", Color = "Blanco", Sku = "CAM-002", CodigoBarras = "770123400102", PrecioCompra = 50000, PrecioVenta = 89300, StockActual = 8, StockMinimo = 4, StockMaximo = 25 },
                    new() { ProductoId = p1.Id, Talla = "M", Marca = "Arturo Calle", Color = "Azul", Sku = "CAM-003", CodigoBarras = "770123400103", PrecioCompra = 50000, PrecioVenta = 89300, StockActual = 11, StockMinimo = 5, StockMaximo = 30 },

                    // Pantalón Chino
                    new() { ProductoId = p2.Id, Talla = "32", Marca = "Arturo Calle", Color = "Khaki", Sku = "PAN-007", CodigoBarras = "770123400701", PrecioCompra = 85000, PrecioVenta = 145300, StockActual = 9, StockMinimo = 4, StockMaximo = 20 },
                    new() { ProductoId = p2.Id, Talla = "34", Marca = "Arturo Calle", Color = "Khaki", Sku = "PAN-008", CodigoBarras = "770123400702", PrecioCompra = 85000, PrecioVenta = 145300, StockActual = 7, StockMinimo = 4, StockMaximo = 20 },
                    new() { ProductoId = p2.Id, Talla = "32", Marca = "Arturo Calle", Color = "Negro", Sku = "PAN-009", CodigoBarras = "770123400703", PrecioCompra = 85000, PrecioVenta = 145300, StockActual = 6, StockMinimo = 4, StockMaximo = 20 },

                    // Zapato Cuero
                    new() { ProductoId = p3.Id, Talla = "42", Marca = "Arturo Calle", Color = "Negro", Sku = "ZAP-016", CodigoBarras = "770123401601", PrecioCompra = 190000, PrecioVenta = 320000, StockActual = 5, StockMinimo = 3, StockMaximo = 15 },
                    new() { ProductoId = p3.Id, Talla = "40", Marca = "Calzado Rivera", Color = "Café", Sku = "ZAP-017", CodigoBarras = "770123401602", PrecioCompra = 185000, PrecioVenta = 310000, StockActual = 4, StockMinimo = 3, StockMaximo = 15 },

                    // Cinturón Tejido
                    new() { ProductoId = p4.Id, Talla = "Único", Marca = "Arturo Calle", Color = "Café", Sku = "CIN-004", CodigoBarras = "770123400401", PrecioCompra = 30000, PrecioVenta = 55300, StockActual = 22, StockMinimo = 6, StockMaximo = 40 },
                    new() { ProductoId = p4.Id, Talla = "Único", Marca = "Arturo Calle", Color = "Negro", Sku = "CIN-005", CodigoBarras = "770123400402", PrecioCompra = 30000, PrecioVenta = 55300, StockActual = 18, StockMinimo = 6, StockMaximo = 40 },

                    // Camisa Lino
                    new() { ProductoId = p5.Id, Talla = "L", Marca = "Arturo Calle", Color = "Azul", Sku = "CAM-012", CodigoBarras = "770123401201", PrecioCompra = 58000, PrecioVenta = 95000, StockActual = 7, StockMinimo = 4, StockMaximo = 20 },
                    new() { ProductoId = p5.Id, Talla = "M", Marca = "Confecciones Huila", Color = "Blanco", Sku = "CAM-013", CodigoBarras = "770123401202", PrecioCompra = 58000, PrecioVenta = 95000, StockActual = 8, StockMinimo = 4, StockMaximo = 20 },

                    // Pantalón Dress
                    new() { ProductoId = p6.Id, Talla = "34", Marca = "Arturo Calle", Color = "Negro", Sku = "PAN-015", CodigoBarras = "770123401501", PrecioCompra = 98000, PrecioVenta = 165000, StockActual = 6, StockMinimo = 3, StockMaximo = 18 },

                    // Polo Pima (En alerta de bajo stock para demostrar el sistema de alertas)
                    new() { ProductoId = p7.Id, Talla = "M", Marca = "Confecciones Huila", Color = "Verde Militar", Sku = "POL-021", CodigoBarras = "770123402101", PrecioCompra = 38000, PrecioVenta = 72000, StockActual = 2, StockMinimo = 8, StockMaximo = 35 },
                    new() { ProductoId = p7.Id, Talla = "L", Marca = "Confecciones Huila", Color = "Blanco", Sku = "POL-022", CodigoBarras = "770123402102", PrecioCompra = 38000, PrecioVenta = 72000, StockActual = 3, StockMinimo = 8, StockMaximo = 35 },

                    // Bermuda Casual (En alerta crítica agotada para demostrar el cálculo automático)
                    new() { ProductoId = p8.Id, Talla = "32", Marca = "Arturo Calle", Color = "Beige", Sku = "BER-030", CodigoBarras = "770123403001", PrecioCompra = 45000, PrecioVenta = 85000, StockActual = 0, StockMinimo = 6, StockMaximo = 25 }
                };

                context.Variantes.AddRange(variantes);
                context.SaveChanges();
            }
            else
            {
                // Si ya existen productos, actualizar sus imágenes si están vacías
                var imgMap = new Dictionary<string, string>
                {
                    { "CAM-OXF", "https://images.unsplash.com/photo-1602810318383-e386cc2a3ccf?w=500&auto=format&fit=crop&q=80" },
                    { "PAN-CHI", "https://images.unsplash.com/photo-1473966968600-fa801b869a1a?w=500&auto=format&fit=crop&q=80" },
                    { "ZAP-CUE", "https://images.unsplash.com/photo-1549298916-b41d501d3772?w=500&auto=format&fit=crop&q=80" },
                    { "CIN-TEJ", "https://images.unsplash.com/photo-1624222247344-550fb60583dc?w=500&auto=format&fit=crop&q=80" },
                    { "CAM-LIN", "https://images.unsplash.com/photo-1596755094514-f87e34085b2c?w=500&auto=format&fit=crop&q=80" },
                    { "PAN-DRE", "https://images.unsplash.com/photo-1594633312681-425c7b97ccd1?w=500&auto=format&fit=crop&q=80" },
                    { "POL-PIM", "https://images.unsplash.com/photo-1581655353564-df123a1eb820?w=500&auto=format&fit=crop&q=80" },
                    { "BER-CAS", "https://images.unsplash.com/photo-1591195853828-11db59a44f6b?w=500&auto=format&fit=crop&q=80" }
                };

                bool actualizo = false;
                foreach (var prod in context.Productos.ToList())
                {
                    if (string.IsNullOrEmpty(prod.ImagenUrl) && imgMap.TryGetValue(prod.CodigoBase, out var url))
                    {
                        prod.ImagenUrl = url;
                        actualizo = true;
                    }
                }
                if (actualizo)
                {
                    context.SaveChanges();
                }
            }

            // 5. Compras iniciales para existencias
            if (!context.Compras.Any())
            {
                var prov = context.Proveedores.First();
                var user = context.Usuarios.First();
                var v1 = context.Variantes.First(v => v.Sku == "CAM-001");
                var v2 = context.Variantes.First(v => v.Sku == "PAN-007");

                var compra = new CompraProveedor
                {
                    NumeroFactura = "FAC-PROV-9821",
                    ProveedorId = prov.Id,
                    UsuarioId = user.Id,
                    Fecha = DateTime.UtcNow.AddDays(-5),
                    Total = (15 * v1.PrecioCompra) + (10 * v2.PrecioCompra),
                    Estado = "Registrada",
                    Observaciones = "Entrada inicial de lote de temporada Campoalegre"
                };

                compra.Detalles.Add(new DetalleCompra
                {
                    VarianteProductoId = v1.Id,
                    Cantidad = 15,
                    CostoUnitario = v1.PrecioCompra,
                    Subtotal = 15 * v1.PrecioCompra
                });

                compra.Detalles.Add(new DetalleCompra
                {
                    VarianteProductoId = v2.Id,
                    Cantidad = 10,
                    CostoUnitario = v2.PrecioCompra,
                    Subtotal = 10 * v2.PrecioCompra
                });

                context.Compras.Add(compra);
                context.SaveChanges();
            }

            // 6. Ventas Semilla (con discriminación precisa: Efectivo, Transferencia, Sistecrédito)
            if (!context.Ventas.Any())
            {
                var userCajero = context.Usuarios.FirstOrDefault(u => u.Rol == "Cajero") ?? context.Usuarios.First();
                var mEfectivo = context.MediosPago.First(m => m.Codigo == "EFECTIVO");
                var mTransf = context.MediosPago.First(m => m.Codigo == "TRANSFERENCIA");
                var mSistecredito = context.MediosPago.First(m => m.Codigo == "SISTECREDITO");

                var varCamisa = context.Variantes.First(v => v.Sku == "CAM-001");
                var varPant = context.Variantes.First(v => v.Sku == "PAN-007");
                var varZap = context.Variantes.First(v => v.Sku == "ZAP-016");
                var varCin = context.Variantes.First(v => v.Sku == "CIN-004");
                var varPol = context.Variantes.First(v => v.Sku == "POL-021");

                // Venta 1: En Efectivo
                var v1 = new Venta
                {
                    NumeroFactura = "FAC-0001",
                    Fecha = DateTime.UtcNow.Date.AddHours(9),
                    UsuarioId = userCajero.Id,
                    MedioPagoId = mEfectivo.Id,
                    Subtotal = varCamisa.PrecioVenta + (varCin.PrecioVenta * 2),
                    Descuento = 0,
                    Iva = 0,
                    Total = varCamisa.PrecioVenta + (varCin.PrecioVenta * 2),
                    Estado = "Completada"
                };
                v1.Detalles.Add(new DetalleVenta { VarianteProductoId = varCamisa.Id, Cantidad = 1, PrecioUnitario = varCamisa.PrecioVenta, Subtotal = varCamisa.PrecioVenta });
                v1.Detalles.Add(new DetalleVenta { VarianteProductoId = varCin.Id, Cantidad = 2, PrecioUnitario = varCin.PrecioVenta, Subtotal = varCin.PrecioVenta * 2 });

                // Venta 2: En Transferencia (Nequi)
                var v2 = new Venta
                {
                    NumeroFactura = "FAC-0002",
                    Fecha = DateTime.UtcNow.Date.AddHours(11),
                    UsuarioId = userCajero.Id,
                    MedioPagoId = mTransf.Id,
                    ReferenciaPago = "NEQUI-TRX-874219",
                    Subtotal = varZap.PrecioVenta + varPant.PrecioVenta,
                    Descuento = 0,
                    Iva = 0,
                    Total = varZap.PrecioVenta + varPant.PrecioVenta,
                    Estado = "Completada"
                };
                v2.Detalles.Add(new DetalleVenta { VarianteProductoId = varZap.Id, Cantidad = 1, PrecioUnitario = varZap.PrecioVenta, Subtotal = varZap.PrecioVenta });
                v2.Detalles.Add(new DetalleVenta { VarianteProductoId = varPant.Id, Cantidad = 1, PrecioUnitario = varPant.PrecioVenta, Subtotal = varPant.PrecioVenta });

                // Venta 3: A Crédito con Sistecrédito
                var v3 = new Venta
                {
                    NumeroFactura = "FAC-0003",
                    Fecha = DateTime.UtcNow.Date.AddHours(14),
                    UsuarioId = userCajero.Id,
                    MedioPagoId = mSistecredito.Id,
                    ReferenciaPago = "SISTECREDITO-AUTH-55410",
                    Subtotal = (varCamisa.PrecioVenta * 2) + varPant.PrecioVenta + (varPol.PrecioVenta * 3),
                    Descuento = 0,
                    Iva = 0,
                    Total = (varCamisa.PrecioVenta * 2) + varPant.PrecioVenta + (varPol.PrecioVenta * 3),
                    Estado = "Completada"
                };
                v3.Detalles.Add(new DetalleVenta { VarianteProductoId = varCamisa.Id, Cantidad = 2, PrecioUnitario = varCamisa.PrecioVenta, Subtotal = varCamisa.PrecioVenta * 2 });
                v3.Detalles.Add(new DetalleVenta { VarianteProductoId = varPant.Id, Cantidad = 1, PrecioUnitario = varPant.PrecioVenta, Subtotal = varPant.PrecioVenta });
                v3.Detalles.Add(new DetalleVenta { VarianteProductoId = varPol.Id, Cantidad = 3, PrecioUnitario = varPol.PrecioVenta, Subtotal = varPol.PrecioVenta * 3 });

                // Venta 4: Efectivo tarde
                var v4 = new Venta
                {
                    NumeroFactura = "FAC-0004",
                    Fecha = DateTime.UtcNow.Date.AddHours(16),
                    UsuarioId = userCajero.Id,
                    MedioPagoId = mEfectivo.Id,
                    Subtotal = varPant.PrecioVenta * 2,
                    Descuento = 0,
                    Iva = 0,
                    Total = varPant.PrecioVenta * 2,
                    Estado = "Completada"
                };
                v4.Detalles.Add(new DetalleVenta { VarianteProductoId = varPant.Id, Cantidad = 2, PrecioUnitario = varPant.PrecioVenta, Subtotal = varPant.PrecioVenta * 2 });

                context.Ventas.AddRange(v1, v2, v3, v4);
                context.SaveChanges();
            }
        }
    }
}
