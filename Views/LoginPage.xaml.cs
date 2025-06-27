using NovelBook.Services;

namespace NovelBook;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly DatabaseService _databaseService;

    public LoginPage()
    {
        InitializeComponent();
        
        // Crear instancias de los servicios
        _databaseService = new DatabaseService();
        _authService = new AuthService(_databaseService);
    }

  /*  private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EmailEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            await DisplayAlert("Error", "Por favor completa todos los campos", "OK");
            return;
        }

        // Mostrar indicador de carga
        var loginButton = sender as Button;
        loginButton.IsEnabled = false;
        loginButton.Text = "Iniciando sesión...";

        try
        {
            var (success, message, user) = await _authService.LoginAsync(
                EmailEntry.Text.Trim(), 
                PasswordEntry.Text
            );

            if (success)
            {
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
            loginButton.IsEnabled = true;
            loginButton.Text = "Entrar";
        }
    }*/

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