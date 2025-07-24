using NovelBook.Services;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace NovelBook;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly DatabaseService _databaseService;
    private readonly IFingerprint _fingerprint;
    private readonly GitHubAuthService _githubAuthService; 

    public LoginPage()
    {
        InitializeComponent();

        // Crear instancias de los servicios
        _databaseService = new DatabaseService();
        _authService = new AuthService(_databaseService);
        _fingerprint = CrossFingerprint.Current;

        // Crear instancia del servicio de GitHub
        _githubAuthService = new GitHubAuthService(_databaseService, _authService);

        // Verificar si hay credenciales guardadas para biometría
        CheckBiometricLogin();
    }

    /// <summary>
    /// Verifica si el dispositivo tiene biometría disponible y si hay credenciales guardadas
    /// </summary>
    private async void CheckBiometricLogin()
    {
        try
        {
            // Verificar si el dispositivo soporta biometría
            var isAvailable = await _fingerprint.IsAvailableAsync();

            // Verificar si hay credenciales guardadas
            var savedEmail = await SecureStorage.GetAsync("biometric_email");
            var savedPassword = await SecureStorage.GetAsync("biometric_password");

            if (isAvailable && !string.IsNullOrEmpty(savedEmail) && !string.IsNullOrEmpty(savedPassword))
            {
                // Mostrar botón de biometría
                await Device.InvokeOnMainThreadAsync(() =>
                {
                    // Agregar un botón de biometría dinámicamente
                    var biometricButton = new Button
                    {
                        Text = LocalizationService.GetString("BiometricLoginButton"),
                        BackgroundColor = Color.FromArgb("#2E7D32"),
                        TextColor = Colors.White,
                        CornerRadius = 10,
                        HeightRequest = 50,
                        FontAttributes = FontAttributes.Bold,
                        Margin = new Thickness(0, 10, 0, 0)
                    };

                    biometricButton.Clicked += OnBiometricLoginClicked;

                    // Encontrar el VerticalStackLayout donde está el botón de login
                    var loginButton = this.FindByName<Button>("LoginButton");
                    if (loginButton?.Parent is VerticalStackLayout layout)
                    {
                        var index = layout.Children.IndexOf(loginButton);
                        layout.Children.Insert(index + 1, biometricButton);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error verificando biometría: {ex.Message}");
        }
    }

    /// <summary>
    /// Maneja el evento de click en el botón de biometría
    /// </summary>
    private async void OnBiometricLoginClicked(object sender, EventArgs e)
    {
        try
        {
            var email = await SecureStorage.GetAsync("biometric_email");
            var password = await SecureStorage.GetAsync("biometric_password");

            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
            {
                await TryBiometricLogin(email, password);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                LocalizationService.GetString("Error"),
                LocalizationService.GetString("BiometricCredentialsError") ?? "No se pudieron recuperar las credenciales guardadas",
                LocalizationService.GetString("OK"));
        }
    }

    /// <summary>
    /// Intenta hacer login usando biometría
    /// </summary>
    private async Task TryBiometricLogin(string email, string password)
    {
        try
        {
            var request = new AuthenticationRequestConfiguration(
                           LocalizationService.GetString("Login"),
                           LocalizationService.GetString("BiometricLoginMessage"));

            var result = await _fingerprint.AuthenticateAsync(request);

            if (result.Authenticated)
            {
                // Autenticación biométrica exitosa, proceder con login
                var (success, message, user) = await _authService.LoginAsync(email, password);

                if (success)
                {
                    await DisplayAlert(
                        LocalizationService.GetString("Success"),
                        LocalizationService.GetString("WelcomeBack", user.Name),
                        LocalizationService.GetString("OK"));
                    App.SetMainPageToShell();
                }
                else
                {
                    await DisplayAlert(
                        LocalizationService.GetString("Error"),
                        LocalizationService.GetString("InvalidCredentials"),
                        LocalizationService.GetString("OK"));
                    // Limpiar credenciales guardadas
                    SecureStorage.Remove("biometric_email");
                    SecureStorage.Remove("biometric_password");
                }
            }
            else if (result.Status == FingerprintAuthenticationResultStatus.Canceled)
            {
                // Usuario canceló - no hacer nada
            }
            else if (result.Status == FingerprintAuthenticationResultStatus.TooManyAttempts)
            {
                await DisplayAlert(
                    LocalizationService.GetString("Error"),
                    LocalizationService.GetString("TooManyAttempts") ?? "Demasiados intentos fallidos. Usa tu contraseña.",
                    LocalizationService.GetString("OK"));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                LocalizationService.GetString("Error"),
                $"{LocalizationService.GetString("BiometricError")}: {ex.Message}",
                LocalizationService.GetString("OK"));
        }
    }

    /// <summary>
    /// Pregunta al usuario si desea guardar las credenciales para biometría
    /// </summary>
    private async Task AskToSaveBiometricCredentials(string email, string password)
    {
        try
        {
            // NO preguntar para usuarios de login social
            if (_authService.IsCurrentUserSocialLogin())
                return;

            var isAvailable = await _fingerprint.IsAvailableAsync();
            if (!isAvailable) return;

            // Verificar si ya tiene credenciales guardadas
            var savedEmail = await SecureStorage.GetAsync("biometric_email");
            if (!string.IsNullOrEmpty(savedEmail)) return;

            // Preguntar al usuario
            var answer = await DisplayAlert(
                LocalizationService.GetString("BiometricAuth"),
                LocalizationService.GetString("EnableBiometricMessage"),
                LocalizationService.GetString("Yes"),
                LocalizationService.GetString("No"));

            if (answer)
            {
                // Primero autenticar con biometría
                var request = new AuthenticationRequestConfiguration(
                    LocalizationService.GetString("BiometricAuth"),
                    LocalizationService.GetString("BiometricSetupMessage"));

                var result = await _fingerprint.AuthenticateAsync(request);

                if (result.Authenticated)
                {
                    // Guardar credenciales de forma segura
                    await SecureStorage.SetAsync("biometric_email", email);
                    await SecureStorage.SetAsync("biometric_password", password);

                    await DisplayAlert(
                        LocalizationService.GetString("Success"),
                        LocalizationService.GetString("BiometricEnabled"),
                        LocalizationService.GetString("OK"));
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error guardando credenciales biométricas: {ex.Message}");
        }
    }

    private async void OnFacebookLoginTapped(object sender, EventArgs e)
    {
        await DisplayAlert(
            "Facebook",
            LocalizationService.GetString("ComingSoon") ?? "Login con Facebook - Próximamente",
            LocalizationService.GetString("OK"));
    }

    private async void OnGoogleLoginTapped(object sender, EventArgs e)
    {
        await DisplayAlert(
            "Google",
            LocalizationService.GetString("ComingSoon") ?? "Login con Google - Próximamente",
            LocalizationService.GetString("OK"));
    }

    /// <summary>
    /// Maneja el login con GitHub
    /// AGREGAR este método nuevo (no reemplazar el de Facebook)
    /// </summary>
    private async void OnGitHubLoginTapped(object sender, EventArgs e)
    {
        try
        {
            // Deshabilitar el botón mientras se procesa
            var githubButton = sender as View;
            if (githubButton != null)
                githubButton.IsEnabled = false;

            // Mostrar indicador de carga (opcional)
            await DisplayAlert(
                LocalizationService.GetString("PleaseWait") ?? "Por favor espera",
                LocalizationService.GetString("ConnectingGitHub") ?? "Conectando con GitHub...",
                LocalizationService.GetString("OK"));

            // Intentar login con GitHub
            var (success, message, user) = await _githubAuthService.LoginWithGitHubAsync();

            if (success)
            {
                // Login exitoso
                await DisplayAlert(
                    LocalizationService.GetString("Success"),
                    LocalizationService.GetString("WelcomeBack", user.Name) ?? $"¡Bienvenido {user.Name}!",
                    LocalizationService.GetString("OK"));

                // Navegar a la pantalla principal
                App.SetMainPageToShell();
            }
            else
            {
                // Mostrar error
                await DisplayAlert(
                    LocalizationService.GetString("Error"),
                    message,
                    LocalizationService.GetString("OK"));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en GitHub Login: {ex.Message}");

            await DisplayAlert(
                LocalizationService.GetString("Error"),
                LocalizationService.GetString("GitHubLoginError") ?? "Error al iniciar sesión con GitHub",
                LocalizationService.GetString("OK"));
        }
        finally
        {
            // Re-habilitar el botón
            var githubButton = sender as View;
            if (githubButton != null)
                githubButton.IsEnabled = true;
        }
    }

    private async void OnRegisterTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Views.RegisterPage());
    }

    private async void OnGuestTapped(object sender, EventArgs e)
    {
        await DisplayAlert(
            LocalizationService.GetString("Guest"),
            LocalizationService.GetString("WelcomeGuest") ?? "¡Bienvenido Invitado!",
            LocalizationService.GetString("OK"));

        App.SetMainPageToShell();
    }

    //Enter para iniciar sesión
    private async void OnPasswordEntryCompleted(object sender, EventArgs e)
    {
        // En lugar de buscar el botón, ejecutar directamente la lógica de login
        await PerformLogin();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        await PerformLogin();
    }

    private async Task PerformLogin()
    {
        if (string.IsNullOrWhiteSpace(EmailEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            await DisplayAlert(
            LocalizationService.GetString("Error"),
            LocalizationService.GetString("FillAllFields"),
            LocalizationService.GetString("OK"));
            return;
        }

        // Deshabilitar controles mientras se hace login
        EmailEntry.IsEnabled = false;
        PasswordEntry.IsEnabled = false;

        // Buscar el botón de login si existe
        var loginButton = this.FindByName<Button>("LoginButton");
        if (loginButton != null)
        {
            loginButton.IsEnabled = false;
            loginButton.Text = LocalizationService.GetString("LoggingIn");
        }

        try
        {
            var (success, message, user) = await _authService.LoginAsync(
                EmailEntry.Text.Trim(),
                PasswordEntry.Text
            );

            if (success)
            {
                // Preguntar si quiere guardar para biometría
                await AskToSaveBiometricCredentials(EmailEntry.Text.Trim(), PasswordEntry.Text);

                await DisplayAlert(
                    LocalizationService.GetString("Success"),
                    LocalizationService.GetString("WelcomeBack", user.Name),
                    LocalizationService.GetString("OK"));
                App.SetMainPageToShell();
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
            EmailEntry.IsEnabled = true;
            PasswordEntry.IsEnabled = true;

            if (loginButton != null)
            {
                loginButton.IsEnabled = true;
                loginButton.Text = LocalizationService.GetString("Login");
            }
        }
    }

    /// <summary>
    /// Alterna la visibilidad de la contraseña
    /// </summary>
    private void OnPasswordToggleClicked(object sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;

        // Cambiar el emoji del label
        PasswordToggle.Text = PasswordEntry.IsPassword ? "🙈" : "👁️";
    }

    /// <summary>
    /// Maneja el tap en "¿Olvidaste tu contraseña?"
    /// </summary>
    private async void OnForgotPasswordTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Views.ForgotPasswordPage());
    }
}