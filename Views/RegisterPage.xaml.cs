using NovelBook.Services;

namespace NovelBook.Views;

public partial class RegisterPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly DatabaseService _databaseService;

    public RegisterPage()
    {
        InitializeComponent();

        _databaseService = new DatabaseService();
        _authService = new AuthService(_databaseService);
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // Validar campos
        if (string.IsNullOrWhiteSpace(NameEntry.Text) ||
            string.IsNullOrWhiteSpace(EmailEntry.Text) ||
            string.IsNullOrWhiteSpace(PasswordEntry.Text) ||
            string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
        {
            await DisplayAlert(
            LocalizationService.GetString("Error"),
            LocalizationService.GetString("FillAllFieldsError"),
            LocalizationService.GetString("OK"));
            return;
        }

        // Validar email
        if (!EmailEntry.Text.Contains("@"))
        {
            await DisplayAlert(
            LocalizationService.GetString("Error"),
            LocalizationService.GetString("InvalidEmailError"),
            LocalizationService.GetString("OK"));
            return;
        }

        // Validar contraseña
        if (PasswordEntry.Text.Length < 6)
        {
            await DisplayAlert(
            LocalizationService.GetString("Error"),
            LocalizationService.GetString("PasswordTooShortError"),
            LocalizationService.GetString("OK"));
            return;
        }

        // Validar que las contraseñas coincidan
        if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
        {
            await DisplayAlert(
            LocalizationService.GetString("Error"),
            LocalizationService.GetString("PasswordsDontMatchError"),
            LocalizationService.GetString("OK"));
            return;
        }

        var registerButton = sender as Button;
        registerButton.IsEnabled = false;
        registerButton.Text = LocalizationService.GetString("CreatingAccount");

        try
        {
            var (success, message) = await _authService.RegisterAsync(
                NameEntry.Text.Trim(),
                EmailEntry.Text.Trim(),
                PasswordEntry.Text
            );

            if (success)
            {
                await DisplayAlert(
                LocalizationService.GetString("Success"),
                LocalizationService.GetString("AccountCreatedSuccess"),
                LocalizationService.GetString("OK"));


                // Auto login después del registro
                var (loginSuccess, _, user) = await _authService.LoginAsync(
                    EmailEntry.Text.Trim(),
                    PasswordEntry.Text
                );

                if (loginSuccess)
                {
                    App.SetMainPageToShell();
                }
                else
                {
                    await Navigation.PopAsync();
                }
            }
            else
            {
                await DisplayAlert(
                LocalizationService.GetString("Error"),
                message,
                LocalizationService.GetString("OK"));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(
            LocalizationService.GetString("Error"),
            LocalizationService.GetString("ConnectionError"),
            LocalizationService.GetString("OK"));
        }
        finally
        {
            registerButton.IsEnabled = true;
            registerButton.Text = LocalizationService.GetString("CreateAccount");
        }
    }

    private async void OnLoginTapped(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    /// <summary>
    /// Alterna la visibilidad de la contraseña principal
    /// </summary>
    private void OnPasswordToggleClicked(object sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;

        // Cambiar el emoji del label
        PasswordToggle.Text = PasswordEntry.IsPassword ? "🙈" : "👁️";
    }

    /// <summary>
    /// Alterna la visibilidad de la contraseña de confirmación
    /// </summary>
    private void OnConfirmPasswordToggleClicked(object sender, EventArgs e)
    {
        ConfirmPasswordEntry.IsPassword = !ConfirmPasswordEntry.IsPassword;

        // Cambiar el emoji del label
        ConfirmPasswordToggle.Text = ConfirmPasswordEntry.IsPassword ? "🙈" : "👁️";
    }

    /// <summary>
    /// Valida en tiempo real que las contraseñas coincidan
    /// </summary>
    private void OnPasswordChanged(object sender, TextChangedEventArgs e)
    {
        ValidatePasswords();
    }

    /// <summary>
    /// Valida que las contraseñas coincidan y muestra retroalimentación visual
    /// </summary>
    private void ValidatePasswords()
    {
        if (!string.IsNullOrEmpty(PasswordEntry.Text) && !string.IsNullOrEmpty(ConfirmPasswordEntry.Text))
        {
            bool passwordsMatch = PasswordEntry.Text == ConfirmPasswordEntry.Text;

            // Cambiar el color del marco según si coinciden o no
            PasswordFrame.BorderColor = passwordsMatch ? Colors.Transparent : Color.FromArgb("#EF4444");
            ConfirmPasswordFrame.BorderColor = passwordsMatch ? Colors.Transparent : Color.FromArgb("#EF4444");

            // Mostrar/ocultar mensaje de error
            PasswordMatchLabel.IsVisible = !passwordsMatch;
        }
        else
        {
            // Resetear a estado normal si algún campo está vacío
            PasswordFrame.BorderColor = Colors.Transparent;
            ConfirmPasswordFrame.BorderColor = Colors.Transparent;
            PasswordMatchLabel.IsVisible = false;
        }
    }
}