using System.Text;
using AisaacSTR.Data;
using AisaacSTR.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de Base de Datos (PostgreSQL con fallback seguro a InMemory si la instancia no está activa)
var connectionString = builder.Configuration.GetConnectionString("PostgreSql") 
    ?? "Host=localhost;Port=5432;Database=aisaac_str_db;Username=postgres;Password=1234";

bool usePostgres = false;
try
{
    // Test real de conexión y credenciales a Postgres con timeout de 1 segundo
    var testBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
    {
        Timeout = 1,
        CommandTimeout = 1
    };
    using var npgsqlConn = new Npgsql.NpgsqlConnection(testBuilder.ConnectionString);
    npgsqlConn.Open();
    usePostgres = true;
}
catch
{
    usePostgres = false;
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (usePostgres)
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        // En caso de que el servicio local de PostgreSQL esté apagado, usamos InMemory
        // para que la aplicación web funcione al 100% sin romperse.
        options.UseInMemoryDatabase("AisaacSTR_FallbackDb");
    }
});

// 2. Servicios de Autenticación y JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "AISAAC_STR_SUPER_SECRET_KEY_2026_JWT_RETAIL_SECURITY_TOKEN!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AisaacSTR";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AisaacSTR_Clients";

builder.Services.AddSingleton<IJwtService, JwtService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Permitir leer el token también desde la Cookie si no viene en el Header
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (string.IsNullOrEmpty(context.Token))
            {
                var cookieToken = context.Request.Cookies["Aisaac_Token"];
                if (!string.IsNullOrEmpty(cookieToken))
                {
                    context.Token = cookieToken;
                }
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequiereAdmin", policy => policy.RequireRole("Administrador"));
    options.AddPolicy("RequiereCajero", policy => policy.RequireRole("Cajero", "Administrador"));
});

// 3. MVC y Controladores API con soporte JSON
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();

// 4. Inicialización y Semilla de Base de Datos
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        DbInitializer.Initialize(db);
        Console.WriteLine($"[AISAAC STR] Base de datos inicializada correctamente. Modo: {(usePostgres ? "PostgreSQL Relacional" : "InMemory Demostración")}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[AISAAC STR] Error inicializando BD: {ex.Message}");
    }
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
