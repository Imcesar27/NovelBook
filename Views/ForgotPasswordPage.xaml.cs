using NovelBook.Services;
using System.Text.RegularExpressions;

namespace NovelBook.Views;

public partial class ForgotPasswordPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly DatabaseService _databaseService;
    private string _verificationCode;
    private string _userEmail;
    private DateTime _codeExpirationTime;

    public ForgotPasswordPage()
    {
        InitializeComponent();

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _authService = new AuthService(_databaseService);
    }

    /// <summary>
    /// Envía el código de verificación al email
    /// </summary>
    private async void OnSendCodeClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EmailEntry.Text))
        {
            await DisplayAlert("Error", "Por favor ingresa tu correo electrónico", "OK");
            return;
        }

        // Validar formato de email
        if (!IsValidEmail(EmailEntry.Text))
        {
            await DisplayAlert("Error", "Por favor ingresa un correo electrónico válido", "OK");
            return;
        }

        // Deshabilitar botón mientras procesa
        SendCodeButton.IsEnabled = false;
        SendCodeButton.Text = "Enviando...";

        try
        {
            // Verificar si el email existe en la base de datos
            var emailExists = await CheckEmailExists(EmailEntry.Text.Trim());

            if (!emailExists)
            {
                await DisplayAlert("Error", "No existe una cuenta con ese correo electrónico", "OK");
                return;
            }

            // Generar código de 6 dígitos
            _verificationCode = GenerateVerificationCode();
            _userEmail = EmailEntry.Text.Trim();
            _codeExpirationTime = DateTime.Now.AddMinutes(15); // Código válido por 15 minutos

            // Simular envío de email (en producción, aquí se enviaría realmente el email)
            await SimulateSendEmail(_userEmail, _verificationCode);

            // Mostrar mensaje de éxito
            await DisplayAlert("Código Enviado",
                $"Se ha enviado un código de verificación a {_userEmail}. El código es válido por 15 minutos.",
                "OK");

            // Mostrar la sección de código
            EmailSection.IsVisible = false;
            CodeSection.IsVisible = true;

            // Para testing: mostrar el código en la consola
            System.Diagnostics.Debug.WriteLine($"Código de verificación: {_verificationCode}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Ocurrió un error al enviar el código. Por favor intenta de nuevo.", "OK");
            System.Diagnostics.Debug.WriteLine($"Error enviando código: {ex.Message}");
        }
        finally
        {
            SendCodeButton.IsEnabled = true;
            SendCodeButton.Text = "Enviar código";
        }
    }

    /// <summary>
    /// Verifica el código ingresado
    /// </summary>
    private async void OnVerifyCodeClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CodeEntry.Text))
        {
            await DisplayAlert("Error", "Por favor ingresa el código de verificación", "OK");
            return;
        }

        if (CodeEntry.Text.Length != 6)
        {
            await DisplayAlert("Error", "El código debe tener 6 dígitos", "OK");
            return;
        }

        // Verificar si el código ha expirado
        if (DateTime.Now > _codeExpirationTime)
        {
            await DisplayAlert("Error", "El código ha expirado. Por favor solicita uno nuevo.", "OK");
            return;
        }

        // Verificar el código
        if (CodeEntry.Text == _verificationCode)
        {
            await DisplayAlert("Éxito", "Código verificado correctamente", "OK");

            // Mostrar la sección de nueva contraseña
            CodeSection.IsVisible = false;
            NewPasswordSection.IsVisible = true;
        }
        else
        {
            await DisplayAlert("Error", "Código incorrecto. Por favor verifica e intenta de nuevo.", "OK");
        }
    }

    /// <summary>
    /// Reenvía el código de verificación
    /// </summary>
    private async void OnResendCodeTapped(object sender, EventArgs e)
    {
        ResendLabel.IsEnabled = false;
        ResendLabel.TextColor = Colors.Gray;

        try
        {
            // Generar nuevo código
            _verificationCode = GenerateVerificationCode();
            _codeExpirationTime = DateTime.Now.AddMinutes(15);

            // Simular reenvío de email
            await SimulateSendEmail(_userEmail, _verificationCode);

            await DisplayAlert("Código Reenviado",
                "Se ha enviado un nuevo código de verificación a tu correo.",
                "OK");

            // Para testing: mostrar el código en la consola
            System.Diagnostics.Debug.WriteLine($"Nuevo código de verificación: {_verificationCode}");

            // Deshabilitar el reenvío por 60 segundos
            await Task.Delay(60000);
        }
        finally
        {
            ResendLabel.IsEnabled = true;
            ResendLabel.TextColor = Color.FromArgb("#F97316");
        }
    }

    /// <summary>
    /// Restablece la contraseña
    /// </summary>
    private async void OnResetPasswordClicked(object sender, EventArgs e)
    {
        // Validar campos
        if (string.IsNullOrWhiteSpace(NewPasswordEntry.Text) ||
            string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
        {
            await DisplayAlert("Error", "Por favor completa todos los campos", "OK");
            return;
        }

        // Validar que las contraseñas coincidan
        if (NewPasswordEntry.Text != ConfirmPasswordEntry.Text)
        {
            await DisplayAlert("Error", "Las contraseñas no coinciden", "OK");
            return;
        }

        // Validar longitud mínima
        if (NewPasswordEntry.Text.Length < 6)
        {
            await DisplayAlert("Error", "La contraseña debe tener al menos 6 caracteres", "OK");
            return;
        }

        // Deshabilitar botón mientras procesa
        ResetPasswordButton.IsEnabled = false;
        ResetPasswordButton.Text = "Restableciendo...";

        try
        {
            // Actualizar la contraseña en la base de datos
            var success = await UpdatePassword(_userEmail, NewPasswordEntry.Text);

            if (success)
            {
                await DisplayAlert("Éxito",
                    "Tu contraseña ha sido restablecida exitosamente. Ya puedes iniciar sesión con tu nueva contraseña.",
                    "OK");

                // Volver a la página de login
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Error", "No se pudo actualizar la contraseña. Por favor intenta de nuevo.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Ocurrió un error al restablecer la contraseña.", "OK");
            System.Diagnostics.Debug.WriteLine($"Error actualizando contraseña: {ex.Message}");
        }
        finally
        {
            ResetPasswordButton.IsEnabled = true;
            ResetPasswordButton.Text = "Restablecer contraseña";
        }
    }

    /// <summary>
    /// Vuelve a la página de login
    /// </summary>
    private async void OnBackToLoginTapped(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    /// <summary>
    /// Alterna la visibilidad de la nueva contraseña
    /// </summary>
    private void OnNewPasswordToggleClicked(object sender, EventArgs e)
    {
        NewPasswordEntry.IsPassword = !NewPasswordEntry.IsPassword;

        // Cambiar el emoji del label
        PasswordToggle.Text = NewPasswordEntry.IsPassword ? "🙈" : "👁️";
    }

    /// <summary>
    /// Alterna la visibilidad de la confirmación de contraseña
    /// </summary>
    private void OnConfirmPasswordToggleClicked(object sender, EventArgs e)
    {
        ConfirmPasswordEntry.IsPassword = !ConfirmPasswordEntry.IsPassword;

        // Cambiar el emoji del label
        ConfirmPasswordToggle.Text = ConfirmPasswordEntry.IsPassword ? "🙈" : "👁️";
    }

    /// <summary>
    /// Valida que las contraseñas coincidan mientras se escriben
    /// </summary>
    private void OnPasswordChanged(object sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(NewPasswordEntry.Text) && !string.IsNullOrEmpty(ConfirmPasswordEntry.Text))
        {
            bool passwordsMatch = NewPasswordEntry.Text == ConfirmPasswordEntry.Text;

            NewPasswordFrame.BorderColor = passwordsMatch ? Colors.Transparent : Color.FromArgb("#EF4444");
            ConfirmPasswordFrame.BorderColor = passwordsMatch ? Colors.Transparent : Color.FromArgb("#EF4444");
            PasswordMatchLabel.IsVisible = !passwordsMatch;
        }
        else
        {
            NewPasswordFrame.BorderColor = Colors.Transparent;
            ConfirmPasswordFrame.BorderColor = Colors.Transparent;
            PasswordMatchLabel.IsVisible = false;
        }
    }

    /// <summary>
    /// Valida el formato del email
    /// </summary>
    private bool IsValidEmail(string email)
    {
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, pattern);
    }

    /// <summary>
    /// Genera un código de verificación de 6 dígitos
    /// </summary>
    private string GenerateVerificationCode()
    {
        Random random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    /// <summary>
    /// Verifica si existe un usuario con el email dado
    /// </summary>
    private async Task<bool> CheckEmailExists(string email)
    {
        try
        {
            using var connection = _databaseService.GetConnection();
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM users WHERE email = @email";
            using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
            command.Parameters.AddWithValue("@email", email);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error verificando email: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Actualiza la contraseña del usuario en la base de datos
    /// </summary>
    private async Task<bool> UpdatePassword(string email, string newPassword)
    {
        try
        {
            using var connection = _databaseService.GetConnection();
            await connection.OpenAsync();

            // Hash de la nueva contraseña
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            var query = "UPDATE users SET password_hash = @passwordHash WHERE email = @email";
            using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
            command.Parameters.AddWithValue("@passwordHash", passwordHash);
            command.Parameters.AddWithValue("@email", email);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error actualizando contraseña: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Simula el envío de email (en producción se usaría un servicio real)
    /// </summary>
    private async Task SimulateSendEmail(string email, string code)
    {
        // En producción aquí se integraría con un servicio de email como SendGrid, SMTP, etc.
        await Task.Delay(1000); // Simular delay de envío

        System.Diagnostics.Debug.WriteLine($"===== EMAIL SIMULADO =====");
        System.Diagnostics.Debug.WriteLine($"Para: {email}");
        System.Diagnostics.Debug.WriteLine($"Asunto: Código de verificación - NovelBook");
        System.Diagnostics.Debug.WriteLine($"Contenido:");
        System.Diagnostics.Debug.WriteLine($"Tu código de verificación es: {code}");
        System.Diagnostics.Debug.WriteLine($"Este código es válido por 15 minutos.");
        System.Diagnostics.Debug.WriteLine($"==========================");
    }
}