using BCrypt.Net;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using NovelBook.Models;

namespace NovelBook.Services;

public class AuthService
{
    private readonly DatabaseService _database;
    private static User _currentUser;

    public AuthService(DatabaseService database)
    {
        _database = database;
    }

    //Empieza Social Login

    /// Establece el usuario actual (usado por login social)
    /// </summary>
    /// <param name="user">Usuario autenticado</param>
    public void SetCurrentUser(User user)
    {
        _currentUser = user;
    }

    /// <summary>
    /// Verifica si el usuario actual se autenticó con login social
    /// </summary>
    public bool IsCurrentUserSocialLogin()
    {
        return _currentUser?.IsSocialLogin ?? false;
    }

    /// <summary>
    /// Obtiene la URL de la foto de perfil del usuario actual
    /// </summary>
    public string GetCurrentUserProfilePicture()
    {
        return _currentUser?.ProfilePictureUrl;
    }

    /// <summary>
    /// Obtiene el proveedor de autenticación del usuario actual
    /// </summary>
    public string GetCurrentUserAuthProvider()
    {
        return _currentUser?.AuthProvider ?? "email";
    }

    /// <summary>
    /// Actualiza la contraseña para un usuario de login social
    /// Permite que usuarios que se registraron con Google/Facebook/GitHub puedan establecer una contraseña
    /// </summary>
    public async Task<(bool success, string message)> SetPasswordForSocialUserAsync(int userId, string newPassword)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Verificar que el usuario existe y es de login social
            var checkQuery = @"
                    SELECT COUNT(*) 
                    FROM users 
                    WHERE id = @userId 
                    AND password_hash IS NULL 
                    AND auth_provider IS NOT NULL";

            using var checkCommand = new SqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@userId", userId);

            var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            if (count == 0)
            {
                return (false, "Usuario no encontrado o ya tiene contraseña establecida");
            }

            // Validar la nueva contraseña
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                return (false, "La contraseña no puede estar vacía");
            }

            if (newPassword.Length < 6)
            {
                return (false, "La contraseña debe tener al menos 6 caracteres");
            }

            // Hash de la nueva contraseña
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            // Actualizar la contraseña
            var updateQuery = @"
                    UPDATE users 
                    SET password_hash = @passwordHash,
                        auth_provider = 'email' 
                    WHERE id = @userId";

            using var updateCommand = new SqlCommand(updateQuery, connection);
            updateCommand.Parameters.AddWithValue("@userId", userId);
            updateCommand.Parameters.AddWithValue("@passwordHash", passwordHash);

            var rowsAffected = await updateCommand.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                // Actualizar el usuario actual si es el mismo
                if (_currentUser != null && _currentUser.Id == userId)
                {
                    _currentUser.PasswordHash = passwordHash;
                    _currentUser.AuthProvider = "email";
                }

                return (true, "Contraseña establecida exitosamente");
            }
            else
            {
                return (false, "No se pudo actualizar la contraseña");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error al establecer contraseña: {ex.Message}");
        }
    }

    /// <summary>
    /// Vincula una cuenta social a un usuario existente
    /// </summary>
    public async Task<(bool success, string message)> LinkSocialAccountAsync(
        int userId,
        string provider,
        string providerId,
        string profilePictureUrl = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerId))
            {
                return (false, "Proveedor o ID inválido");
            }

            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Determinar qué columna actualizar según el proveedor
            string columnName = provider.ToLower() switch
            {
                "google" => "google_id",
                "facebook" => "facebook_id",
                "github" => "github_id",
                _ => throw new ArgumentException($"Proveedor no soportado: {provider}")
            };

            // Verificar que el ID del proveedor no esté ya en uso
            var checkQuery = $"SELECT COUNT(*) FROM users WHERE {columnName} = @providerId";
            using var checkCommand = new SqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@providerId", providerId);

            var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            if (count > 0)
            {
                return (false, "Esta cuenta social ya está vinculada a otro usuario");
            }

            // Verificar que el usuario existe
            var userExistsQuery = "SELECT COUNT(*) FROM users WHERE id = @userId";
            using var userExistsCommand = new SqlCommand(userExistsQuery, connection);
            userExistsCommand.Parameters.AddWithValue("@userId", userId);

            var userExists = Convert.ToInt32(await userExistsCommand.ExecuteScalarAsync());
            if (userExists == 0)
            {
                return (false, "Usuario no encontrado");
            }

            // Actualizar el usuario
            var updateQuery = $@"
                    UPDATE users 
                    SET {columnName} = @providerId
                    {(profilePictureUrl != null ? ", profile_picture_url = @profilePictureUrl" : "")}
                    WHERE id = @userId";

            using var updateCommand = new SqlCommand(updateQuery, connection);
            updateCommand.Parameters.AddWithValue("@userId", userId);
            updateCommand.Parameters.AddWithValue("@providerId", providerId);

            if (profilePictureUrl != null)
            {
                updateCommand.Parameters.AddWithValue("@profilePictureUrl", profilePictureUrl);
            }

            var rowsAffected = await updateCommand.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                // Actualizar el usuario actual si es el mismo
                if (_currentUser != null && _currentUser.Id == userId)
                {
                    switch (provider.ToLower())
                    {
                        case "google":
                            _currentUser.GoogleId = providerId;
                            break;
                        case "facebook":
                            _currentUser.FacebookId = providerId;
                            break;
                        case "github":
                            _currentUser.GithubId = providerId;
                            break;
                    }

                    if (profilePictureUrl != null)
                    {
                        _currentUser.ProfilePictureUrl = profilePictureUrl;
                    }
                }

                return (true, $"Cuenta de {provider} vinculada exitosamente");
            }
            else
            {
                return (false, "No se pudo vincular la cuenta");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error al vincular cuenta: {ex.Message}");
        }
    }

    /// <summary>
    /// Desvincula una cuenta social de un usuario
    /// </summary>
    public async Task<(bool success, string message)> UnlinkSocialAccountAsync(int userId, string provider)
    {
        try
        {
            // Verificar que el usuario tenga al menos otro método de login
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var checkQuery = @"
                    SELECT password_hash, google_id, facebook_id, github_id 
                    FROM users 
                    WHERE id = @userId";

            using var checkCommand = new SqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@userId", userId);

            using var reader = await checkCommand.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return (false, "Usuario no encontrado");
            }

            var hasPassword = !reader.IsDBNull(0);
            var hasGoogle = !reader.IsDBNull(1);
            var hasFacebook = !reader.IsDBNull(2);
            var hasGitHub = !reader.IsDBNull(3);
            reader.Close();

            // Contar métodos de autenticación
            var authMethodCount = 0;
            if (hasPassword) authMethodCount++;
            if (hasGoogle) authMethodCount++;
            if (hasFacebook) authMethodCount++;
            if (hasGitHub) authMethodCount++;

            if (authMethodCount <= 1)
            {
                return (false, "No puedes desvincular tu único método de autenticación");
            }

            // Determinar qué columna actualizar
            string columnName = provider.ToLower() switch
            {
                "google" => "google_id",
                "facebook" => "facebook_id",
                "github" => "github_id",
                _ => throw new ArgumentException($"Proveedor no soportado: {provider}")
            };

            // Desvincular la cuenta
            var updateQuery = $"UPDATE users SET {columnName} = NULL WHERE id = @userId";
            using var updateCommand = new SqlCommand(updateQuery, connection);
            updateCommand.Parameters.AddWithValue("@userId", userId);

            await updateCommand.ExecuteNonQueryAsync();

            return (true, $"Cuenta de {provider} desvinculada exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al desvincular cuenta: {ex.Message}");
        }
    }

    //Termina Social Login

    public static User CurrentUser => _currentUser;

    public async Task<(bool success, string message, User user)> LoginAsync(string email, string password)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = "SELECT * FROM users WHERE email = @email AND is_active = 1";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@email", email);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var user = new User
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    Role = reader.IsDBNull(reader.GetOrdinal("role")) ? "user" : reader.GetString(reader.GetOrdinal("role"))
                };

                // Verificar contraseña
                if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    _currentUser = user;

                    // Actualizar último login
                    await UpdateLastLoginAsync(user.Id);

                    return (true, "Login exitoso", user);
                }
            }

            return (false, "Email o contraseña incorrectos", null);
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool success, string message)> RegisterAsync(string name, string email, string password)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Verificar si el email ya existe
            var checkQuery = "SELECT COUNT(*) FROM users WHERE email = @email";
            using var checkCommand = new SqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@email", email);

            var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            if (count > 0)
            {
                return (false, "El email ya está registrado");
            }

            // Hash de la contraseña
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // Insertar nuevo usuario
            var insertQuery = @"INSERT INTO users (email, password_hash, name) 
                               OUTPUT INSERTED.id
                               VALUES (@email, @passwordHash, @name)";
            using var insertCommand = new SqlCommand(insertQuery, connection);
            insertCommand.Parameters.AddWithValue("@email", email);
            insertCommand.Parameters.AddWithValue("@passwordHash", passwordHash);
            insertCommand.Parameters.AddWithValue("@name", name);

            var userId = (int)await insertCommand.ExecuteScalarAsync();

            // Crear configuración por defecto
            var settingsQuery = "INSERT INTO user_settings (user_id) VALUES (@userId)";
            using var settingsCommand = new SqlCommand(settingsQuery, connection);
            settingsCommand.Parameters.AddWithValue("@userId", userId);
            await settingsCommand.ExecuteNonQueryAsync();

            return (true, "Registro exitoso");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    private async Task UpdateLastLoginAsync(int userId)
    {
        using var connection = _database.GetConnection();
        await connection.OpenAsync();

        var query = "UPDATE users SET last_login = GETDATE() WHERE id = @userId";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Cierra la sesión del usuario actual
    /// </summary>
    public void Logout()
    {
        // Guardar las configuraciones importantes antes de limpiar
        var savedLanguage = Preferences.Get("AppLanguage", "system");
        var savedTheme = Preferences.Get("AppTheme", "System");
        var savedFontSize = Preferences.Get("FontSize", 16.0);

        // Limpiar datos del usuario
        _currentUser = null;

        // Limpiar solo las preferencias relacionadas con el usuario
        Preferences.Remove("user_email");
        Preferences.Remove("user_password");
        Preferences.Remove("remember_user");

        // NO limpiar Preferences.Clear() para mantener las configuraciones

        // Restaurar las configuraciones importantes
        Preferences.Set("AppLanguage", savedLanguage);
        Preferences.Set("AppTheme", savedTheme);
        Preferences.Set("FontSize", savedFontSize);

        // Limpiar credenciales biométricas
        SecureStorage.Remove("biometric_email");
    }

    // Alternativamente método separado para limpiar solo datos del usuario:
    public void ClearUserData()
    {
        _currentUser = null;

        // Lista de claves relacionadas con el usuario
        var userRelatedKeys = new[]
        {
        "user_email",
        "user_password",
        "remember_user",
        "last_login",
        "user_token"
    };

        // Limpiar solo las claves del usuario
        foreach (var key in userRelatedKeys)
        {
            Preferences.Remove(key);
        }

        // Limpiar datos seguros
        SecureStorage.RemoveAll();
    }
}