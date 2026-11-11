using System.ComponentModel.DataAnnotations;

namespace AisaacSTR.DTOs
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }

    public class CrearUsuarioRequest
    {
        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ\s]+$", ErrorMessage = "El nombre completo solo puede contener letras, espacios y acentos.")]
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Rol { get; set; } = "Cajero";
        [RegularExpression(@"^\+?[0-9]{7,15}$", ErrorMessage = "El teléfono solo puede contener números y, opcionalmente, un signo + al inicio.")]
        public string? Telefono { get; set; }
    }

    public class EstadoUsuarioRequest
    {
        public bool Activo { get; set; }
    }

    public class ActualizarUsuarioRequest
    {
        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ\s]+$", ErrorMessage = "El nombre completo solo puede contener letras, espacios y acentos.")]
        public string NombreCompleto { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Rol { get; set; } = "Cajero";

        [RegularExpression(@"^\+?[0-9]{7,15}$", ErrorMessage = "El teléfono solo puede contener números y, opcionalmente, un signo + al inicio.")]
        public string? Telefono { get; set; }
    }

    public class RestablecerPasswordRequest
    {
        public string Password { get; set; } = string.Empty;
        public string ConfirmarPassword { get; set; } = string.Empty;
    }
}
