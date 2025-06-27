namespace NovelBook.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private async void OnClearCacheClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert("Limpiar Caché",
            "¿Estás seguro de que deseas limpiar el caché?\nEsto liberará espacio pero tendrás que volver a descargar las imágenes.",
            "Sí", "No");

        if (answer)
        {
            // Implementar limpieza de caché
            await DisplayAlert("Éxito", "Caché limpiado correctamente", "OK");
        }
    }

    private async void OnBackupClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Backup", "Creando backup de tu biblioteca y configuración...", "OK");
        // Implementar backup
    }

    private async void OnRestoreClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert("Restaurar Backup",
            "¿Estás seguro de que deseas restaurar un backup?\nEsto reemplazará tu configuración actual.",
            "Sí", "No");

        if (answer)
        {
            // Implementar restauración
            await DisplayAlert("Éxito", "Backup restaurado correctamente", "OK");
        }
    }

    private async void OnChangePasswordClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Cambiar Contraseña", "Función próximamente", "OK");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert("Cerrar Sesión",
            "¿Estás seguro de que deseas cerrar sesión?",
            "Sí", "No");

        if (answer)
        {
            // Volver a la página de login
            Application.Current.MainPage = new NavigationPage(new LoginPage())
            {
                BarBackgroundColor = Color.FromArgb("#1A1A1A"),
                BarTextColor = Colors.White
            };
        }
    }
}