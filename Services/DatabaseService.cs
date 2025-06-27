using Microsoft.Data.SqlClient;
using System.Data;

namespace NovelBook.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        // Opción 1: Si usas autenticación de Windows (la más común)
        _connectionString = "Server=CESARR-PC;Database=novelbook_db;Trusted_Connection=true;TrustServerCertificate=true;";

        // Opción 2: Si el servidor tiene una instancia específica (ej: CESARR-PC\SQLEXPRESS)
        // _connectionString = "Server=CESARR-PC\\SQLEXPRESS;Database=novelbook_db;Trusted_Connection=true;TrustServerCertificate=true;";

        // Opción 3: Si se usa autenticación SQL Server (con usuario y contraseña)
        // _connectionString = "Server=CESARR-PC;Database=novelbook_db;User Id=sa;Password=tupassword;TrustServerCertificate=true;";
    }

    public SqlConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            // Verificar que la base de datos existe
            var command = new SqlCommand("SELECT DB_NAME()", connection);
            var dbName = await command.ExecuteScalarAsync();
            System.Diagnostics.Debug.WriteLine($"Conectado exitosamente a: {dbName}");

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error de conexión: {ex.Message}");
            return false;
        }
    }
}