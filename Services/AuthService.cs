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

    public void Logout()
    {
        _currentUser = null;
    }
}