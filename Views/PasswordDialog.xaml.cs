using Microsoft.Maui.Controls;
using NovelBook.Services;

namespace NovelBook.Views.Dialogs;

public partial class PasswordDialog : ContentPage
{
    private TaskCompletionSource<string> _taskCompletionSource;
    private bool _isPasswordVisible = false;

    public PasswordDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Muestra el diálogo y espera la respuesta del usuario
    /// </summary>
    public Task<string> ShowAsync()
    {
        _taskCompletionSource = new TaskCompletionSource<string>();
        return _taskCompletionSource.Task;
    }

    /// <summary>
    /// Alterna la visibilidad de la contraseña
    /// </summary>
    private void OnTogglePasswordVisibility(object sender, EventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        PasswordEntry.IsPassword = !_isPasswordVisible;

        // Cambiar el ícono
        VisibilityIcon.Text = _isPasswordVisible ? "🙈" : "👁️";
    }

    /// <summary>
    /// Maneja el evento cuando se presiona Enter en el campo de contraseña
    /// </summary>
    private void OnPasswordCompleted(object sender, EventArgs e)
    {
        OnConfirmClicked(sender, e);
    }

    /// <summary>
    /// Maneja el click del botón Cancelar
    /// </summary>
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        // Animar el cierre
        await this.FadeTo(0, 250);

        _taskCompletionSource?.SetResult(null);
        await Navigation.PopModalAsync();
    }

    /// <summary>
    /// Maneja el click del botón Confirmar
    /// </summary>
    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(password))
        {
            // Mostrar error
            ErrorLabel.Text = LocalizationService.GetString("PleaseEnterPassword");
            ErrorLabel.IsVisible = true;

            // Shake animation para el campo
            await PasswordEntry.TranslateTo(-10, 0, 50);
            await PasswordEntry.TranslateTo(10, 0, 50);
            await PasswordEntry.TranslateTo(-10, 0, 50);
            await PasswordEntry.TranslateTo(10, 0, 50);
            await PasswordEntry.TranslateTo(0, 0, 50);

            return;
        }

        // Deshabilitar botones mientras procesa
        ConfirmButton.IsEnabled = false;
        ConfirmButton.Text = LocalizationService.GetString("Verifying");

        // Animar el cierre
        await this.FadeTo(0, 250);

        _taskCompletionSource?.SetResult(password);
        await Navigation.PopModalAsync();
    }

    /// <summary>
    /// Muestra un mensaje de error
    /// </summary>
    public void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;

        // Re-habilitar botones
        ConfirmButton.IsEnabled = true;
        ConfirmButton.Text = LocalizationService.GetString("Confirm");
    }

    protected override bool OnBackButtonPressed()
    {
        // Cancelar al presionar el botón atrás
        OnCancelClicked(null, null);
        return true;
    }
}