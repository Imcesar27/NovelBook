using NovelBook.Services;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace NovelBook;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly DatabaseService _databaseService;
    private readonly IFingerprint _fingerprint;

    public LoginPage()
    {
        InitializeComponent();

        // Crear instancias de los servicios
        _databaseService = new DatabaseService();
        _authService = new AuthService(_databaseService);
        _fingerprint = CrossFingerprint.Current;

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
                        Text = "🔐 Iniciar con Face ID/Touch ID",
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
            await DisplayAlert("Error", "No se pudieron recuperar las credenciales guardadas", "OK");
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
                "Iniciar Sesión",
                "Usa tu huella digital o Face ID para acceder a NovelBook");

            var result = await _fingerprint.AuthenticateAsync(request);

            if (result.Authenticated)
            {
                // Autenticación biométrica exitosa, proceder con login
                var (success, message, user) = await _authService.LoginAsync(email, password);

                if (success)
                {
                    await DisplayAlert("Éxito", $"Bienvenido {user.Name}", "OK");
                    App.SetMainPageToShell();
                }
                else
                {
                    await DisplayAlert("Error", "Las credenciales guardadas ya no son válidas", "OK");
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
                await DisplayAlert("Error", "Demasiados intentos fallidos. Usa tu contraseña.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error en autenticación biométrica: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Pregunta al usuario si desea guardar las credenciales para biometría
    /// </summary>
    private async Task AskToSaveBiometricCredentials(string email, string password)
    {
        try
        {
            var isAvailable = await _fingerprint.IsAvailableAsync();
            if (!isAvailable) return;

            // Verificar si ya tiene credenciales guardadas
            var savedEmail = await SecureStorage.GetAsync("biometric_email");
            if (!string.IsNullOrEmpty(savedEmail)) return;

            // Preguntar al usuario
            var answer = await DisplayAlert(
                "Biometría",
                "¿Deseas habilitar el inicio de sesión con Face ID/Touch ID para futuros accesos?",
                "Sí",
                "No");

            if (answer)
            {
                // Primero autenticar con biometría
                var request = new AuthenticationRequestConfiguration(
                    "Autenticación Biométrica",
                    "Usa tu huella digital o Face ID para habilitar el acceso rápido");

                var result = await _fingerprint.AuthenticateAsync(request);

                if (result.Authenticated)
                {
                    // Guardar credenciales de forma segura
                    await SecureStorage.SetAsync("biometric_email", email);
                    await SecureStorage.SetAsync("biometric_password", password);

                    await DisplayAlert("Éxito", "Face ID/Touch ID habilitado correctamente", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error guardando credenciales biométricas: {ex.Message}");
        }
    }

    // TU CÓDIGO EXISTENTE COMIENZA AQUÍ (con modificación en PerformLogin)

    private async void OnFacebookLoginTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Facebook", "Login con Facebook - Próximamente", "OK");
    }

    private async void OnGoogleLoginTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Google", "Login con Google - Próximamente", "OK");
    }

    private async void OnRegisterTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Views.RegisterPage());
    }

    private async void OnGuestTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Invitado", "¡Bienvenido Invitado!", "OK");
        // Por ahora, ir directo a la app sin login
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
            await DisplayAlert("Error", "Por favor completa todos los campos", "OK");
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
            loginButton.Text = "Iniciando sesión...";
        }

        try
        {
            var (success, message, user) = await _authService.LoginAsync(
                EmailEntry.Text.Trim(),
                PasswordEntry.Text
            );

            if (success)
            {
                // NUEVA LÍNEA: Preguntar si quiere guardar para biometría
                await AskToSaveBiometricCredentials(EmailEntry.Text.Trim(), PasswordEntry.Text);

                await DisplayAlert("Éxito", $"Bienvenido {user.Name}", "OK");
                App.SetMainPageToShell();
            }
            else
            {
                await DisplayAlert("Error", message, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error de conexión. Verifica tu conexión a internet.", "OK");
        }
        finally
        {
            EmailEntry.IsEnabled = true;
            PasswordEntry.IsEnabled = true;

            if (loginButton != null)
            {
                loginButton.IsEnabled = true;
                loginButton.Text = "Entrar";
            }
        }
    }
}