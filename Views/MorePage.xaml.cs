using Microsoft.Maui.Controls;
using NovelBook.Services;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace NovelBook.Views;

public partial class MorePage : ContentPage
{
    public MorePage()
    {
        InitializeComponent();
        CheckAdminAccess();
    }

    private void CheckAdminAccess()
    {
        if (AuthService.CurrentUser != null && AuthService.CurrentUser.IsAdmin)
        {
            AdminSection.IsVisible = true;
            AdminSeparator.IsVisible = true;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateUserInfo();
        CheckAdminAccess();

        // Cargar estado del modo incógnito
        IncognitoSwitch.IsToggled = Preferences.Get("IncognitoMode", false);

        // Cargar foto de perfil si existe
        LoadProfileImage();

        // Actualizar estado de biometría
        UpdateBiometricStatus();
    }

    private async void UpdateBiometricStatus()
    {
        try
        {
            var biometricStatusLabel = this.FindByName<Label>("BiometricStatusMoreLabel");
            if (biometricStatusLabel != null)
            {
                var savedEmail = await SecureStorage.GetAsync("biometric_email");
                biometricStatusLabel.Text = !string.IsNullOrEmpty(savedEmail) ? "Activado" : "No configurado";
            }
        }
        catch { }
    }

    private void UpdateUserInfo()
    {
        var userInfoGrid = this.FindByName<Grid>("UserInfoGrid");
        var avatarLabel = this.FindByName<Label>("AvatarLabel");
        var userNameLabel = this.FindByName<Label>("UserNameLabel");
        var userEmailLabel = this.FindByName<Label>("UserEmailLabel");
        var memberSinceLabel = this.FindByName<Label>("MemberSinceLabel");

        if (AuthService.CurrentUser != null)
        {
            // Usuario logueado
            var user = AuthService.CurrentUser;
            avatarLabel.Text = user.Name.Substring(0, 1).ToUpper();
            userNameLabel.Text = user.Name;
            userEmailLabel.Text = user.Email;
            memberSinceLabel.Text = $"Miembro desde: {user.CreatedAt:MMMM yyyy}";
        }
        else
        {
            // Modo invitado
            avatarLabel.Text = "👤";
            userNameLabel.Text = "Invitado";
            userEmailLabel.Text = "No has iniciado sesión";
            memberSinceLabel.Text = "Modo invitado";
        }

        if (AuthService.CurrentUser != null)
        {
            // Usuario logueado - habilitar switch
            IncognitoSwitch.IsEnabled = true;
        }
        else
        {
            // Modo invitado - deshabilitar switch y gestos de foto
            IncognitoSwitch.IsEnabled = false;
            IncognitoSwitch.IsToggled = false;

            // Deshabilitar tap en avatar para invitados
            var avatarFrame = avatarLabel.Parent?.Parent as Frame;
            if (avatarFrame != null)
            {
                avatarFrame.GestureRecognizers.Clear();
            }
        }
    }

    /// <summary>
    /// Carga la imagen de perfil guardada
    /// </summary>
    private async void LoadProfileImage()
    {
        try
        {
            if (AuthService.CurrentUser == null) return;

            // Obtener la ruta de la imagen guardada
            var imagePath = Preferences.Get($"profile_image_{AuthService.CurrentUser.Id}", string.Empty);

            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                // Buscar los controles por nombre
                var profileImage = this.FindByName<Image>("ProfileImage");
                var avatarLabel = this.FindByName<Label>("AvatarLabel");

                if (profileImage != null && avatarLabel != null)
                {
                    // Cargar la imagen
                    profileImage.Source = ImageSource.FromFile(imagePath);
                    profileImage.IsVisible = true;
                    avatarLabel.IsVisible = false;
                }
            }
            else
            {
                // No hay imagen, mostrar inicial
                var profileImage = this.FindByName<Image>("ProfileImage");
                var avatarLabel = this.FindByName<Label>("AvatarLabel");

                if (profileImage != null && avatarLabel != null)
                {
                    profileImage.IsVisible = false;
                    avatarLabel.IsVisible = true;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando imagen de perfil: {ex.Message}");
        }
    }

    /// <summary>
    /// Maneja el tap en el avatar para cambiar la foto
    /// </summary>
    private async void OnAvatarTapped(object sender, EventArgs e)
    {
        if (AuthService.CurrentUser == null)
        {
            await DisplayAlert("Información", "Debes iniciar sesión para cambiar tu foto de perfil", "OK");
            return;
        }

        // Mostrar opciones
        var action = await DisplayActionSheet(
            "Cambiar foto de perfil",
            "Cancelar",
            "Eliminar foto",
            "Tomar foto",
            "Elegir de galería");

        switch (action)
        {
            case "Tomar foto":
                await TakePhotoAsync();
                break;
            case "Elegir de galería":
                await PickPhotoAsync();
                break;
            case "Eliminar foto":
                await RemoveProfilePhoto();
                break;
        }
    }

    /// <summary>
    /// Toma una foto con la cámara
    /// </summary>
    private async Task TakePhotoAsync()
    {
        try
        {
            // Verificar permisos de cámara
            var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (cameraStatus != PermissionStatus.Granted)
            {
                cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (cameraStatus == PermissionStatus.Granted)
            {
                // Tomar foto
                var photo = await MediaPicker.CapturePhotoAsync(new MediaPickerOptions
                {
                    Title = "Tomar foto de perfil"
                });

                if (photo != null)
                {
                    await ProcessPhotoAsync(photo);
                }
            }
            else
            {
                await DisplayAlert("Permisos", "Se necesitan permisos de cámara para tomar fotos", "OK");
            }
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert("Error", "La cámara no está disponible en este dispositivo", "OK");
        }
        catch (PermissionException)
        {
            await DisplayAlert("Error", "No se otorgaron los permisos de cámara", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al tomar la foto: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Selecciona una foto de la galería
    /// </summary>
    private async Task PickPhotoAsync()
    {
        try
        {
            // Verificar permisos de galería
            var storageStatus = await Permissions.CheckStatusAsync<Permissions.Photos>();
            if (storageStatus != PermissionStatus.Granted)
            {
                storageStatus = await Permissions.RequestAsync<Permissions.Photos>();
            }

            if (storageStatus == PermissionStatus.Granted)
            {
                // Seleccionar foto
                var photo = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Seleccionar foto de perfil"
                });

                if (photo != null)
                {
                    await ProcessPhotoAsync(photo);
                }
            }
            else
            {
                await DisplayAlert("Permisos", "Se necesitan permisos para acceder a las fotos", "OK");
            }
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert("Error", "La galería no está disponible en este dispositivo", "OK");
        }
        catch (PermissionException)
        {
            await DisplayAlert("Error", "No se otorgaron los permisos de galería", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al seleccionar la foto: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Procesa la foto seleccionada o tomada
    /// </summary>
    private async Task ProcessPhotoAsync(FileResult photo)
    {
        try
        {
            // Crear directorio para fotos de perfil si no existe
            var appDataDirectory = FileSystem.AppDataDirectory;
            var profileImagesDir = Path.Combine(appDataDirectory, "ProfileImages");

            if (!Directory.Exists(profileImagesDir))
            {
                Directory.CreateDirectory(profileImagesDir);
            }

            // Generar nombre único para la imagen
            var fileName = $"profile_{AuthService.CurrentUser.Id}_{DateTime.Now.Ticks}.jpg";
            var destinationPath = Path.Combine(profileImagesDir, fileName);

            // Copiar y redimensionar la imagen
            using (var sourceStream = await photo.OpenReadAsync())
            {
                // Guardar la imagen
                using (var fileStream = File.Create(destinationPath))
                {
                    await sourceStream.CopyToAsync(fileStream);
                }
            }

            // Eliminar imagen anterior si existe
            var oldImagePath = Preferences.Get($"profile_image_{AuthService.CurrentUser.Id}", string.Empty);
            if (!string.IsNullOrEmpty(oldImagePath) && File.Exists(oldImagePath) && oldImagePath != destinationPath)
            {
                try
                {
                    File.Delete(oldImagePath);
                }
                catch { }
            }

            // Guardar nueva ruta
            Preferences.Set($"profile_image_{AuthService.CurrentUser.Id}", destinationPath);

            // Actualizar UI
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var profileImage = this.FindByName<Image>("ProfileImage");
                var avatarLabel = this.FindByName<Label>("AvatarLabel");

                if (profileImage != null && avatarLabel != null)
                {
                    profileImage.Source = ImageSource.FromFile(destinationPath);
                    profileImage.IsVisible = true;
                    avatarLabel.IsVisible = false;
                }
            });

            await DisplayAlert("Éxito", "Foto de perfil actualizada correctamente", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al procesar la foto: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Elimina la foto de perfil actual
    /// </summary>
    private async Task RemoveProfilePhoto()
    {
        try
        {
            var confirm = await DisplayAlert(
                "Eliminar foto",
                "¿Estás seguro de que deseas eliminar tu foto de perfil?",
                "Sí",
                "No");

            if (!confirm) return;

            // Obtener ruta de la imagen actual
            var imagePath = Preferences.Get($"profile_image_{AuthService.CurrentUser.Id}", string.Empty);

            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                try
                {
                    File.Delete(imagePath);
                }
                catch { }
            }

            // Limpiar preferencias
            Preferences.Remove($"profile_image_{AuthService.CurrentUser.Id}");

            // Actualizar UI
            var profileImage = this.FindByName<Image>("ProfileImage");
            var avatarLabel = this.FindByName<Label>("AvatarLabel");

            if (profileImage != null && avatarLabel != null)
            {
                profileImage.IsVisible = false;
                avatarLabel.IsVisible = true;
            }

            await DisplayAlert("Éxito", "Foto de perfil eliminada", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al eliminar la foto: {ex.Message}", "OK");
        }
    }

    private async void OnIncognitoToggled(object sender, ToggledEventArgs e)
    {
        // Guardar estado en preferencias
        Preferences.Set("IncognitoMode", e.Value);

        // Notificar al usuario
        var message = e.Value ?
            "Modo incógnito activado. No se guardará el historial de lectura." :
            "Modo incógnito desactivado.";

        await DisplayAlert("Modo Incógnito", message, "OK");
    }

    private async void OnOptionTapped(object sender, EventArgs e)
    {
        if (sender is Grid grid && grid.GestureRecognizers[0] is TapGestureRecognizer tap)
        {
            string option = tap.CommandParameter as string;

            switch (option)
            {
                case "ReadingMode":
                    // El switch maneja esto
                    break;

                case "Downloads":
                    await Navigation.PushAsync(new DownloadsPage());
                    break;

                case "DownloadQueue":
                    await Navigation.PushAsync(new DownloadQueuePage());
                    break;

                case "Categories":
                    await Navigation.PushAsync(new CategoriesPage());
                    break;

                case "Statistics":
                    await Navigation.PushAsync(new StatsPage());
                    break;

                case "BiometricSettings":
                    await Navigation.PushAsync(new SettingsPage());
                    break;

                case "Settings":
                    await Navigation.PushAsync(new SettingsPage());
                    break;

                case "About":
                    await DisplayAlert("Acerca de",
                        "NovelBook v1.0.0\n\n" +
                        "Tu biblioteca personal de novelas ligeras\n\n" +
                        "Desarrollado con ❤️ por tu equipo",
                        "OK");
                    break;

                case "CreateNovel":
                    await Navigation.PushAsync(new CreateNovelPage());
                    break;

                case "ManageNovels":
                    await Shell.Current.GoToAsync(nameof(ManageNovelsPage));
                    break;

                case "Logout":
                    await HandleLogout();
                    break;
            }
        }
    }

    // Manejar el evento de cierre de sesión
    private async Task HandleLogout()
    {
        bool confirm = await DisplayAlert("Cerrar Sesión",
            "¿Estás seguro de que deseas cerrar sesión?",
            "Sí", "No");

        if (confirm)
        {
            // Limpiar datos del usuario
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