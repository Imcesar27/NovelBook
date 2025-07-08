using Microsoft.Maui.Controls;
using NovelBook.Services;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace NovelBook.Views;

public partial class SettingsPage : ContentPage
{
    private readonly IFingerprint _fingerprint;
    private bool _isLoadingSettings = true; // 🛑 Bandera para evitar ejecución automática del evento del switch

    public SettingsPage()
    {
        InitializeComponent();
        _fingerprint = CrossFingerprint.Current;
        LoadSettings();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isLoadingSettings = true;
        LoadSettings();
        CheckBiometricStatus();
        _isLoadingSettings = false;
    }

    private void LoadSettings()
    {
        // Cargar configuraciones de lectura
        FontSizeSlider.Value = Preferences.Get("FontSize", 16.0);
        FontSizeLabel.Text = FontSizeSlider.Value.ToString("0");
        NightModeSwitch.IsToggled = Preferences.Get("NightMode", false);
        KeepScreenOnSwitch.IsToggled = Preferences.Get("KeepScreenOn", true);

        // Cargar configuraciones de notificaciones
        UpdateNotificationsSwitch.IsToggled = Preferences.Get("UpdateNotifications", true);
        RecommendationNotificationsSwitch.IsToggled = Preferences.Get("RecommendationNotifications", false);

        // Cargar configuraciones de descargas
        WifiOnlySwitch.IsToggled = Preferences.Get("WifiOnly", true);
        DownloadLimitStepper.Value = Preferences.Get("DownloadLimit", 10.0);
        UpdateDownloadLimitLabel();

        // Cargar información de cuenta
        if (AuthService.CurrentUser != null)
        {
            AccountEmailLabel.Text = AuthService.CurrentUser.Email;
        }
        else
        {
            AccountEmailLabel.Text = "Modo invitado";
        }

        // Calcular tamaño de caché
        UpdateCacheSize();
    }

    /// <summary>
    /// Verifica el estado actual de la biometría
    /// </summary>
    private async void CheckBiometricStatus()
    {
        try
        {
            // Verificar si el dispositivo soporta biometría
            var isAvailable = await _fingerprint.IsAvailableAsync();

            if (!isAvailable)
            {
                // Dispositivo no soporta biometría
                BiometricSwitch.IsEnabled = false;
                BiometricSwitch.IsToggled = false;
                BiometricStatusLabel.Text = "No disponible en este dispositivo";
                return;
            }

            // Verificar si hay credenciales guardadas
            var savedEmail = await SecureStorage.GetAsync("biometric_email");
            var hasBiometric = !string.IsNullOrEmpty(savedEmail);

            // Desconectar evento
            BiometricSwitch.Toggled -= OnBiometricToggled;

            // Actualizar UI
            BiometricSwitch.IsToggled = hasBiometric;
            BiometricStatusLabel.Text = hasBiometric ? "Activado" : "No configurado";

            // Reconectar evento
            BiometricSwitch.Toggled += OnBiometricToggled;

            // Si es modo invitado, deshabilitar
            if (AuthService.CurrentUser == null)
            {
                BiometricSwitch.IsEnabled = false;
                BiometricStatusLabel.Text = "Inicia sesión para usar esta función";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error verificando biometría: {ex.Message}");
        }
    }

    /// <summary>
    /// Maneja el cambio del switch de biometría
    /// </summary>
    private async void OnBiometricToggled(object sender, ToggledEventArgs e)
    {
        if (_isLoadingSettings)
            return;

        try
        {
            if (AuthService.CurrentUser == null)
            {
                BiometricSwitch.IsToggled = false;
                await DisplayAlert("Información", "Debes iniciar sesión para usar esta función", "OK");
                return;
            }

            if (e.Value)
            {
                // Activar biometría
                await EnableBiometric();
            }
            else
            {
                // Desactivar biometría
                await DisableBiometric();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cambiar configuración: {ex.Message}", "OK");
            // Revertir el switch
            BiometricSwitch.IsToggled = !e.Value;
        }
    }

    /// <summary>
    /// Activa la autenticación biométrica
    /// </summary>
    private async Task EnableBiometric()
    {
        try
        {
            // Verificar disponibilidad
            var isAvailable = await _fingerprint.IsAvailableAsync();
            if (!isAvailable)
            {
                await DisplayAlert("Error", "La autenticación biométrica no está disponible", "OK");
                BiometricSwitch.IsToggled = false;
                return;
            }

            // Crear y mostrar el diálogo de contraseña personalizado
            var passwordDialog = new Views.Dialogs.PasswordDialog();
            await Navigation.PushModalAsync(passwordDialog, true);

            // Esperar la respuesta del usuario
            var password = await passwordDialog.ShowAsync();

            if (string.IsNullOrEmpty(password))
            {
                // Usuario canceló
                BiometricSwitch.IsToggled = false;
                return;
            }

            // Verificar la contraseña
            var (success, message, user) = await new AuthService(new DatabaseService())
                .LoginAsync(AuthService.CurrentUser.Email, password);

            if (!success)
            {
                await DisplayAlert("Error", "Contraseña incorrecta", "OK");
                BiometricSwitch.IsToggled = false;
                return;
            }

            // Autenticar con biometría
            var request = new AuthenticationRequestConfiguration(
                "Habilitar Biometría",
                "Usa tu huella digital o Face ID para confirmar");

            var result = await _fingerprint.AuthenticateAsync(request);

            if (result.Authenticated)
            {
                // Guardar credenciales
                await SecureStorage.SetAsync("biometric_email", AuthService.CurrentUser.Email);
                await SecureStorage.SetAsync("biometric_password", password);

                // Desconectar evento temporalmente
                BiometricSwitch.Toggled -= OnBiometricToggled;

                // Actualizar el switch manualmente
                BiometricSwitch.IsToggled = true;

                // Reconectar evento
                BiometricSwitch.Toggled += OnBiometricToggled;

                BiometricStatusLabel.Text = "Activado";
                await DisplayAlert("Éxito", "Face ID/Touch ID habilitado correctamente", "OK");
            }
            else
            {
                BiometricSwitch.IsToggled = false;
                await DisplayAlert("Cancelado", "No se pudo habilitar la autenticación biométrica", "OK");
            }
        }
        catch (Exception ex)
        {
            BiometricSwitch.IsToggled = false;
            await DisplayAlert("Error", $"Error al habilitar biometría: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Desactiva la autenticación biométrica
    /// </summary>
    private async Task DisableBiometric()
    {
        try
        {
            var confirm = await DisplayAlert(
                "Desactivar Biometría",
                "¿Estás seguro de que deseas desactivar Face ID/Touch ID?\n\nTendrás que ingresar tu contraseña manualmente la próxima vez.",
                "Sí, desactivar",
                "Cancelar");

            if (!confirm)
            {
                BiometricSwitch.IsToggled = true;
                return;
            }

            // Autenticar antes de desactivar
            var request = new AuthenticationRequestConfiguration(
                "Confirmar Desactivación",
                "Usa tu huella digital o Face ID para confirmar");

            var result = await _fingerprint.AuthenticateAsync(request);

            if (result.Authenticated)
            {
                // Eliminar credenciales guardadas
                SecureStorage.Remove("biometric_email");
                SecureStorage.Remove("biometric_password");

                BiometricStatusLabel.Text = "No configurado";
                await DisplayAlert("Éxito", "Face ID/Touch ID desactivado", "OK");
            }
            else
            {
                // Si no se autentica, revertir el switch
                BiometricSwitch.IsToggled = true;
            }
        }
        catch (Exception ex)
        {
            BiometricSwitch.IsToggled = true;
            await DisplayAlert("Error", $"Error al desactivar biometría: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Maneja el tap en cambiar contraseña
    /// </summary>
private async void OnChangePasswordTapped(object sender, EventArgs e)
{
    try
    {
        // Verificar que haya un usuario autenticado
        if (AuthService.CurrentUser == null)
        {
            await DisplayAlert("Error", "Debes iniciar sesión para cambiar tu contraseña", "OK");
            return;
        }

        // Crear y mostrar el diálogo de cambio de contraseña
        var changePasswordDialog = new Dialogs.ChangePasswordDialog();
        await Navigation.PushModalAsync(changePasswordDialog, true);

        // Esperar el resultado
        var result = await changePasswordDialog.ShowAsync();

        if (result)
        {
            // La contraseña se cambió exitosamente
            System.Diagnostics.Debug.WriteLine("Contraseña actualizada exitosamente");
        }
    }
    catch (Exception ex)
    {
        await DisplayAlert("Error", "No se pudo abrir el diálogo de cambio de contraseña", "OK");
        System.Diagnostics.Debug.WriteLine($"Error en OnChangePasswordTapped: {ex.Message}");
    }
}

    // Eventos de configuración de lectura
    private void OnFontSizeChanged(object sender, ValueChangedEventArgs e)
    {
        var size = Math.Round(e.NewValue);
        FontSizeLabel.Text = size.ToString("0");
        Preferences.Set("FontSize", size);
    }

    private void OnNightModeToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Set("NightMode", e.Value);
    }

    private void OnKeepScreenOnToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Set("KeepScreenOn", e.Value);
        // Aplicar configuración
        DeviceDisplay.KeepScreenOn = e.Value;
    }

    // Eventos de notificaciones
    private void OnUpdateNotificationsToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Set("UpdateNotifications", e.Value);
    }

    private void OnRecommendationNotificationsToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Set("RecommendationNotifications", e.Value);
    }

    // Eventos de descargas
    private void OnWifiOnlyToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Set("WifiOnly", e.Value);
    }

    private void OnDownloadLimitChanged(object sender, ValueChangedEventArgs e)
    {
        var limit = (int)e.NewValue;
        Preferences.Set("DownloadLimit", limit);
        UpdateDownloadLimitLabel();
    }

    private void UpdateDownloadLimitLabel()
    {
        var limit = (int)DownloadLimitStepper.Value;
        DownloadLimitLabel.Text = $"Máximo {limit} capítulos";
    }

    private async void OnClearCacheTapped(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert(
            "Limpiar Caché",
            "¿Estás seguro de que deseas eliminar todos los archivos en caché?\n\nEsto liberará espacio pero tendrás que descargar de nuevo los capítulos.",
            "Sí, limpiar",
            "Cancelar");

        if (confirm)
        {
            try
            {
                // Limpiar directorio de caché
                var cacheDir = FileSystem.CacheDirectory;
                if (Directory.Exists(cacheDir))
                {
                    var di = new DirectoryInfo(cacheDir);
                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }

                await DisplayAlert("Éxito", "Caché limpiado correctamente", "OK");
                UpdateCacheSize();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al limpiar caché: {ex.Message}", "OK");
            }
        }
    }

    private void UpdateCacheSize()
    {
        try
        {
            long size = 0;
            var cacheDir = FileSystem.CacheDirectory;

            if (Directory.Exists(cacheDir))
            {
                var di = new DirectoryInfo(cacheDir);
                size = di.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
            }

            // Convertir a MB
            double sizeMB = size / (1024.0 * 1024.0);
            CacheSizeLabel.Text = $"{sizeMB:F1} MB";
        }
        catch
        {
            CacheSizeLabel.Text = "0 MB";
        }
    }

    // Eventos de cuenta
    private async void OnAccountInfoTapped(object sender, EventArgs e)
    {
        if (AuthService.CurrentUser == null)
        {
            await DisplayAlert("Información", "Debes iniciar sesión para ver la información de tu cuenta", "OK");
            return;
        }

        // TODO: navegar a una página de perfil
        // await Navigation.PushAsync(new ProfilePage());

        await DisplayAlert("Cuenta",
            $"Nombre: {AuthService.CurrentUser.Name}\n" +
            $"Email: {AuthService.CurrentUser.Email}\n" +
            $"Miembro desde: {AuthService.CurrentUser.CreatedAt:MMMM yyyy}",
            "OK");
    }

    private async void OnLogoutTapped(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert(
            "Cerrar Sesión",
            "¿Estás seguro de que deseas cerrar sesión?",
            "Sí",
            "No");

        if (confirm)
        {
            // Limpiar datos
            var authService = new AuthService(new DatabaseService());
            authService.Logout();

            // Limpiar preferencias
            Preferences.Clear();

            // Volver al login
            Application.Current.MainPage = new NavigationPage(new LoginPage())
            {
                BarBackgroundColor = Color.FromArgb("#1A1A1A"),
                BarTextColor = Colors.White
            };
        }
    }
    }