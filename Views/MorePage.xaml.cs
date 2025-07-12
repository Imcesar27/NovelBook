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
                biometricStatusLabel.Text = !string.IsNullOrEmpty(savedEmail) ?
                LocalizationService.GetString("Activated") : // CAMBIO
                LocalizationService.GetString("NotConfigured"); // CAMBIO
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
            memberSinceLabel.Text = string.Format(
            LocalizationService.GetString("MemberSince"), // CAMBIO
            user.CreatedAt.ToString("MMMM yyyy")
            );
        }
        else
        {
            // Modo invitado
            avatarLabel.Text = "👤";
            userNameLabel.Text = LocalizationService.GetString("GuestUser"); // CAMBIO
            userEmailLabel.Text = LocalizationService.GetString("NoEmailRegistered"); // CAMBIO
            memberSinceLabel.Text = LocalizationService.GetString("GuestMode"); // CAMBIO
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
            await DisplayAlert(
            LocalizationService.GetString("Info"),
            LocalizationService.GetString("LoginRequiredProfilePhoto"), // CAMBIO
            LocalizationService.GetString("OK"));
            return;
        }

        // Mostrar opciones
        var action = await DisplayActionSheet(
        LocalizationService.GetString("ChangeProfilePhoto"), // CAMBIO
        LocalizationService.GetString("Cancel"),
        LocalizationService.GetString("RemovePhoto"), // CAMBIO
        LocalizationService.GetString("TakePhoto"), // CAMBIO
        LocalizationService.GetString("ChooseFromGallery")); // CAMBIO

        if (action == LocalizationService.GetString("TakePhoto"))
        {
            await TakePhotoAsync();
        }
        else if (action == LocalizationService.GetString("ChooseFromGallery"))
        {
            await PickPhotoAsync();
        }
        else if (action == LocalizationService.GetString("RemovePhoto"))
        {
            await RemoveProfilePhoto();
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
                    Title = LocalizationService.GetString("TakeProfilePhotoTitle") // CAMBIO
                });

                if (photo != null)
                {
                    await ProcessPhotoAsync(photo);
                }
            }
            else
            {
                await DisplayAlert(
                LocalizationService.GetString("Permissions"),
                LocalizationService.GetString("CameraPermissionRequired"), // CAMBIO
                LocalizationService.GetString("OK"));
            }
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert(
            LocalizationService.GetString("Error"),
            LocalizationService.GetString("CameraNotAvailable"), 
            LocalizationService.GetString("OK"));
        }
        catch (PermissionException)
        {
            await DisplayAlert(
           LocalizationService.GetString("Error"),
           LocalizationService.GetString("CameraPermissionDenied"), // CAMBIO
           LocalizationService.GetString("OK"));
        }
        catch (Exception ex)
        {
            await DisplayAlert(
            LocalizationService.GetString("Error"),
            string.Format(LocalizationService.GetString("ErrorTakingPhoto"), ex.Message), // CAMBIO
            LocalizationService.GetString("OK"));
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
                    Title = LocalizationService.GetString("SelectProfilePhotoTitle") // CAMBIO
                });

                if (photo != null)
                {
                    await ProcessPhotoAsync(photo);
                }
            }
            else
            {
                await DisplayAlert(
                LocalizationService.GetString("Permissions"),
                LocalizationService.GetString("PhotosPermissionRequired"), // CAMBIO
                LocalizationService.GetString("OK"));
            }
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert(
            LocalizationService.GetString("Error"),
            LocalizationService.GetString("GalleryNotAvailable"), 
            LocalizationService.GetString("OK"));
        }
        catch (PermissionException)
        {
            await DisplayAlert(
            LocalizationService.GetString("Error"),
            LocalizationService.GetString("PhotosPermissionDenied"), // CAMBIO
            LocalizationService.GetString("OK"));
        }
        catch (Exception ex)
        {
            await DisplayAlert(
            LocalizationService.GetString("Error"),
            string.Format(LocalizationService.GetString("ErrorSelectingPhoto"), ex.Message), // CAMBIO
            LocalizationService.GetString("OK"));
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

            await DisplayAlert(
                LocalizationService.GetString("Success"),
                LocalizationService.GetString("PhotoUpdateSuccess"), 
                LocalizationService.GetString("OK"));
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                LocalizationService.GetString("Error"),
                string.Format(LocalizationService.GetString("ErrorProcessingPhoto"), ex.Message), // Ya lo tienes
                LocalizationService.GetString("OK"));
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
                LocalizationService.GetString("RemovePhotoTitle"), // CAMBIO
                LocalizationService.GetString("ConfirmRemovePhoto"), // CAMBIO
                LocalizationService.GetString("Yes"),
                LocalizationService.GetString("No"));

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

            await DisplayAlert(
                LocalizationService.GetString("Success"),
                LocalizationService.GetString("PhotoRemoveSuccess"), // CAMBIO
                LocalizationService.GetString("OK"));
        }
        catch (Exception ex)
        {
            await DisplayAlert(
            LocalizationService.GetString("Error"),
            string.Format(LocalizationService.GetString("ErrorRemovingPhoto"), ex.Message), // CAMBIO
            LocalizationService.GetString("OK"));
        }
    }

    private async void OnIncognitoToggled(object sender, ToggledEventArgs e)
    {
        // Guardar estado en preferencias
        Preferences.Set("IncognitoMode", e.Value);

        // Notificar al usuario
        var message = e.Value ?
            LocalizationService.GetString("IncognitoActivated") : // CAMBIO
            LocalizationService.GetString("IncognitoDeactivated"); // CAMBIO


        await DisplayAlert(
        LocalizationService.GetString("Info"), 
        message, 
        LocalizationService.GetString("OK"));
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
                    await DisplayAlert(
                            LocalizationService.GetString("About"),
                            LocalizationService.GetString("AboutMessage"), // CAMBIO
                            LocalizationService.GetString("OK"));
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
        bool confirm = await DisplayAlert(
            LocalizationService.GetString("Logout"), // Título: "Cerrar Sesión"
            LocalizationService.GetString("LogoutConfirmMessage"), // Mensaje: "¿Estás seguro de que deseas cerrar sesión?"
            LocalizationService.GetString("Yes"), // Botón de confirmación: "Sí"
            LocalizationService.GetString("No")   // Botón de cancelación: "No"
);


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