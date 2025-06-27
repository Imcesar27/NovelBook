namespace NovelBook;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert("Cerrar Sesión",
            "¿Estás seguro de que deseas cerrar sesión?",
            "Sí", "No");

        if (answer)
        {
            // Volver a la página de login
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}