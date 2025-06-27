using NovelBook.Services;
using BCrypt.Net;

namespace NovelBook.Views;

public partial class TestConnectionPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly AuthService _authService;

    public TestConnectionPage()
    {
        InitializeComponent();

        _databaseService = new DatabaseService();
        _authService = new AuthService(_databaseService);

        // Para debug: mostrar el hash de "123456"
        var testHash = BCrypt.Net.BCrypt.HashPassword("123456");
        System.Diagnostics.Debug.WriteLine($"Hash para '123456': {testHash}");
    }

    private async void OnTestConnectionClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        button.IsEnabled = false;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        ResultLabel.Text = "Conectando...";

        try
        {
            var isConnected = await _databaseService.TestConnectionAsync();

            if (isConnected)
            {
                ResultLabel.TextColor = Color.FromArgb("#10B981");
                ResultLabel.Text = "¡Conexión exitosa! ✓\nLa base de datos está lista.";

                // Verificar directamente en la base de datos
                await VerifyUserInDatabase();
            }
            else
            {
                ResultLabel.TextColor = Color.FromArgb("#EF4444");
                ResultLabel.Text = "Error de conexión ✗";
            }
        }
        catch (Exception ex)
        {
            ResultLabel.TextColor = Color.FromArgb("#EF4444");
            ResultLabel.Text = $"Error: {ex.Message}";
        }
        finally
        {
            button.IsEnabled = true;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async Task VerifyUserInDatabase()
    {
        try
        {
            using var connection = _databaseService.GetConnection();
            await connection.OpenAsync();

            // Verificar si el usuario existe
            var query = "SELECT password_hash FROM users WHERE email = @email";
            using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
            command.Parameters.AddWithValue("@email", "test@test.com");

            var storedHash = await command.ExecuteScalarAsync() as string;

            if (storedHash != null)
            {
                ResultLabel.Text += $"\n\nUsuario encontrado en DB.";

                // Verificar si el hash coincide
                var passwordToTest = "123456";
                var isValid = BCrypt.Net.BCrypt.Verify(passwordToTest, storedHash);

                if (isValid)
                {
                    ResultLabel.Text += "\nHash válido - el login debería funcionar.";
                }
                else
                {
                    ResultLabel.Text += "\nHash inválido - actualizando...";

                    // Actualizar con un hash correcto
                    var newHash = BCrypt.Net.BCrypt.HashPassword("123456");
                    var updateQuery = "UPDATE users SET password_hash = @hash WHERE email = @email";
                    using var updateCommand = new Microsoft.Data.SqlClient.SqlCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@hash", newHash);
                    updateCommand.Parameters.AddWithValue("@email", "test@test.com");
                    await updateCommand.ExecuteNonQueryAsync();

                    ResultLabel.Text += "\nHash actualizado. Intenta login ahora.";

                    // Probar login nuevamente
                    var (success, message, user) = await _authService.LoginAsync("test@test.com", "123456");
                    if (success)
                    {
                        ResultLabel.Text += $"\n\n✓ Login exitoso: {user.Name}";
                    }
                    else
                    {
                        ResultLabel.Text += $"\n\n✗ Login falló: {message}";
                    }
                }
            }
            else
            {
                ResultLabel.Text += "\n\nUsuario NO encontrado en DB.";
            }
        }
        catch (Exception ex)
        {
            ResultLabel.Text += $"\n\nError verificando: {ex.Message}";
        }
    }

    // Resto de métodos...
}