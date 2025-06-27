namespace NovelBook;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Navegación segura
        this.Navigated += OnShellNavigated;
    }

    /// <summary>
    /// Maneja la navegación de forma segura
    /// </summary>
    private async void OnShellNavigated(object sender, ShellNavigatedEventArgs e)
    {
        try
        {
            // Solo limpiar si navegamos entre pestañas principales
            if (e.Source == ShellNavigationSource.ShellSectionChanged ||
                e.Source == ShellNavigationSource.ShellContentChanged)
            {
                await CleanNavigationStackSafely();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en navegación: {ex.Message}");
        }
    }

    /// <summary>
    /// Limpia la pila de navegación de forma segura
    /// </summary>
    private async Task CleanNavigationStackSafely()
    {
        try
        {
            await Device.InvokeOnMainThreadAsync(() =>
            {
                // Verificar que hay páginas para remover
                if (Navigation?.NavigationStack?.Count > 1)
                {
                    // Crear lista de páginas a remover
                    var pagesToRemove = new List<Page>();

                    // Agregar todas las páginas excepto la primera (página principal)
                    for (int i = Navigation.NavigationStack.Count - 1; i > 0; i--)
                    {
                        var page = Navigation.NavigationStack[i - 1];
                        if (page != null)
                        {
                            pagesToRemove.Add(page);
                        }
                    }

                    // Remover páginas de forma segura
                    foreach (var page in pagesToRemove)
                    {
                        try
                        {
                            Navigation.RemovePage(page);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error removiendo página: {ex.Message}");
                        }
                    }
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error limpiando navegación: {ex.Message}");
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert("Cerrar Sesión",
            "¿Estás seguro de que deseas cerrar sesión?",
            "Sí", "No");

        if (answer)
        {
            try
            {
                // Limpiar pila de navegación antes de logout
                await CleanNavigationStackSafely();

                // Limpiar servicios
                var authService = new Services.AuthService(new Services.DatabaseService());
                authService.Logout();

                // Limpiar preferencias
                Preferences.Clear();

                // Volver a la página de login
                Application.Current.MainPage = new NavigationPage(new LoginPage())
                {
                    BarBackgroundColor = Color.FromArgb("#1A1A1A"),
                    BarTextColor = Colors.White
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en logout: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Método público para cambiar al tab de Explorar
    /// </summary>
    public async Task NavigateToExplorePage()
    {
        try
        {
            // Buscar el tab de Explorar
            if (this.CurrentItem is TabBar tabBar)
            {
                var exploreTab = tabBar.Items.FirstOrDefault(tab =>
                    tab.Title.Contains("Explorar") ||
                    tab.Items.Any(item => item.Route == "ExplorePage"));

                if (exploreTab != null)
                {
                    // Cambiar al tab
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        tabBar.CurrentItem = exploreTab;
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error navegando a Explorar: {ex.Message}");
        }
    }
}