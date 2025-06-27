using Microsoft.Maui.Controls;
using NovelBook.Services;
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
                    await DisplayAlert("Cola de Descarga", "Ver cola de descargas", "OK");
                    break;

                case "Categories":
                    await DisplayAlert("Categorías", "Ver todas las categorías", "OK");
                    break;

                case "Statistics":
                    await Navigation.PushAsync(new StatsPage());
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
                    await DisplayAlert("Gestionar", "Gestionar novelas - Próximamente", "OK");
                    break;

                case "Logout": 
                    await HandleLogout();
                    break;

            }
        }
    }
    //Actualizar información del usuario
    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateUserInfo();
        CheckAdminAccess();

        // Cargar estado del modo incógnito
        IncognitoSwitch.IsToggled = Preferences.Get("IncognitoMode", false);
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
            // Modo invitado - deshabilitar switch
            IncognitoSwitch.IsEnabled = false;
            IncognitoSwitch.IsToggled = false;

            // Opcional: agregar tooltip
            var incognitoGrid = IncognitoSwitch.Parent as Grid;
            if (incognitoGrid != null)
            {
                incognitoGrid.Opacity = 0.5;
                ToolTipProperties.SetText(incognitoGrid, "Inicia sesión para activar el modo incógnito.");
            }
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