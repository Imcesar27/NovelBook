namespace NovelBook.Models
{
    /// <summary>
    /// Modelo que representa un usuario en el sistema
    /// Actualizado para soportar login social
    /// </summary>
    public class User
    {
        /// <summary>
        /// ID único del usuario
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Email del usuario (único)
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Hash de la contraseña (puede ser null para usuarios de login social)
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        /// Nombre completo del usuario
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Fecha de creación de la cuenta
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Fecha del último login
        /// </summary>
        public DateTime? LastLogin { get; set; }

        /// <summary>
        /// Indica si la cuenta está activa
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Rol del usuario (user/admin)
        /// </summary>
        public string Role { get; set; } = "user";

        // ===== NUEVOS CAMPOS PARA LOGIN SOCIAL =====

        /// <summary>
        /// ID del usuario en Google (null si no usa Google)
        /// </summary>
        public string GoogleId { get; set; }

        /// <summary>
        /// ID del usuario en Facebook (null si no usa Facebook)
        /// </summary>
        public string FacebookId { get; set; }

        /// <summary>
        /// ID del usuario en GitHub (null si no usa GitHub)
        /// </summary>
        public string GithubId { get; set; }

        /// <summary>
        /// URL de la foto de perfil del usuario
        /// </summary>
        public string ProfilePictureUrl { get; set; }

        /// <summary>
        /// Proveedor de autenticación usado (google/facebook/github/email)
        /// </summary>
        public string AuthProvider { get; set; }

        // ===== PROPIEDADES CALCULADAS =====

        /// <summary>
        /// Indica si es administrador
        /// </summary>
        public bool IsAdmin => Role == "admin";

        /// <summary>
        /// Indica si el usuario se registró con login social
        /// </summary>
        public bool IsSocialLogin => !string.IsNullOrEmpty(AuthProvider) && AuthProvider != "email";

        /// <summary>
        /// Indica si el usuario tiene contraseña establecida
        /// </summary>
        public bool HasPassword => !string.IsNullOrEmpty(PasswordHash);

        /// <summary>
        /// Obtiene las iniciales del usuario para mostrar como avatar
        /// </summary>
        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name))
                    return "U";

                var parts = Name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    return $"{parts[0][0]}{parts[^1][0]}".ToUpper();

                return Name[0].ToString().ToUpper();
            }
        }
    }
}