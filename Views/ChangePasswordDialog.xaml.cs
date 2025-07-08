using NovelBook.Services;

namespace NovelBook.Views.Dialogs;

public partial class ChangePasswordDialog : ContentPage
{
    private readonly TaskCompletionSource<bool> _taskCompletionSource;
    private readonly AuthService _authService;
    private readonly DatabaseService _databaseService;

    public ChangePasswordDialog()
    {
        InitializeComponent();

        _taskCompletionSource = new TaskCompletionSource<bool>();
        _databaseService = new DatabaseService();
        _authService = new AuthService(_databaseService);

        // Animar la entrada
        this.Opacity = 0;
        this.FadeTo(1, 250);
    }

    /// <summary>
    /// Muestra el diálogo y espera el resultado
    /// </summary>
    public async Task<bool> ShowAsync()
    {
        return await _taskCompletionSource.Task;
    }

    /// <summary>
    /// Maneja el tap en el fondo para cerrar
    /// </summary>
    private void OnBackgroundTapped(object sender, EventArgs e)
    {
        OnCancelClicked(sender, e);
    }

    /// <summary>
    /// Cancela el cambio de contraseña
    /// </summary>
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await this.FadeTo(0, 250);
        _taskCompletionSource.SetResult(false);
        await Navigation.PopModalAsync();
    }

    /// <summary>
    /// Confirma el cambio de contraseña
    /// </summary>
    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        // Validar campos vacíos
        if (string.IsNullOrWhiteSpace(CurrentPasswordEntry.Text) ||
            string.IsNullOrWhiteSpace(NewPasswordEntry.Text) ||
            string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
        {
            ShowError("Por favor completa todos los campos");
            return;
        }

        // Validar que las contraseñas nuevas coincidan
        if (NewPasswordEntry.Text != ConfirmPasswordEntry.Text)
        {
            ShowError("Las contraseñas nuevas no coinciden");
            return;
        }

        // Validar longitud mínima
        if (NewPasswordEntry.Text.Length < 6)
        {
            ShowError("La contraseña debe tener al menos 6 caracteres");
            return;
        }

        // Validar que la nueva contraseña sea diferente
        if (CurrentPasswordEntry.Text == NewPasswordEntry.Text)
        {
            ShowError("La nueva contraseña debe ser diferente a la actual");
            return;
        }

        // Deshabilitar botones mientras procesa
        ConfirmButton.IsEnabled = false;
        ConfirmButton.Text = "Cambiando...";

        try
        {
            // Verificar la contraseña actual
            var currentUser = AuthService.CurrentUser;
            if (currentUser == null)
            {
                ShowError("No hay usuario autenticado");
                return;
            }

            // Verificar que la contraseña actual sea correcta
            var (isValid, _, _) = await _authService.LoginAsync(currentUser.Email, CurrentPasswordEntry.Text);

            if (!isValid)
            {
                ShowError("La contraseña actual es incorrecta");
                return;
            }

            // Actualizar la contraseña
            var success = await UpdatePassword(currentUser.Id, NewPasswordEntry.Text);

            if (success)
            {
                await DisplayAlert("Éxito", "Tu contraseña ha sido actualizada correctamente", "OK");

                // Animar el cierre
                await this.FadeTo(0, 250);
                _taskCompletionSource.SetResult(true);
                await Navigation.PopModalAsync();
            }
            else
            {
                ShowError("No se pudo actualizar la contraseña");
            }
        }
        catch (Exception ex)
        {
            ShowError("Error al cambiar la contraseña");
            System.Diagnostics.Debug.WriteLine($"Error cambiando contraseña: {ex.Message}");
        }
        finally
        {
            ConfirmButton.IsEnabled = true;
            ConfirmButton.Text = "Cambiar";
        }
    }

    /// <summary>
    /// Actualiza la contraseña en la base de datos
    /// </summary>
    private async Task<bool> UpdatePassword(int userId, string newPassword)
    {
        try
        {
            using var connection = _databaseService.GetConnection();
            await connection.OpenAsync();

            // Hash de la nueva contraseña
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            var query = "UPDATE users SET password_hash = @passwordHash WHERE id = @userId";
            using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
            command.Parameters.AddWithValue("@passwordHash", passwordHash);
            command.Parameters.AddWithValue("@userId", userId);

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
    /// Muestra un mensaje de error
    /// </summary>
    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;

        // Animación de shake
        Device.BeginInvokeOnMainThread(async () =>
        {
            await ErrorLabel.TranslateTo(-10, 0, 50);
            await ErrorLabel.TranslateTo(10, 0, 50);
            await ErrorLabel.TranslateTo(-10, 0, 50);
            await ErrorLabel.TranslateTo(10, 0, 50);
            await ErrorLabel.TranslateTo(0, 0, 50);
        });
    }

    /// <summary>
    /// Alterna la visibilidad de la contraseña actual
    /// </summary>
    private void OnCurrentPasswordToggleClicked(object sender, EventArgs e)
    {
        CurrentPasswordEntry.IsPassword = !CurrentPasswordEntry.IsPassword;

        PasswordToggle.Text = CurrentPasswordEntry.IsPassword ? "🙈" : "👁️";
    }

    /// <summary>
    /// Alterna la visibilidad de la nueva contraseña
    /// </summary>
    private void OnNewPasswordToggleClicked(object sender, EventArgs e)
    {
        NewPasswordEntry.IsPassword = !NewPasswordEntry.IsPassword;

        NewPasswordToggle.Text = NewPasswordEntry.IsPassword ? "🙈" : "👁️";

    }

    /// <summary>
    /// Alterna la visibilidad de la confirmación de contraseña
    /// </summary>
    private void OnConfirmPasswordToggleClicked(object sender, EventArgs e)
    {
        ConfirmPasswordEntry.IsPassword = !ConfirmPasswordEntry.IsPassword;

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

        // Ocultar error cuando el usuario empieza a escribir
        ErrorLabel.IsVisible = false;
    }

    protected override bool OnBackButtonPressed()
    {
        OnCancelClicked(null, null);
        return true;
    }
}