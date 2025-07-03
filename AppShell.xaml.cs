namespace NovelBook;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Rutas de navegacion
        Routing.RegisterRoute(nameof(Views.CreateNovelPage), typeof(Views.CreateNovelPage));
        Routing.RegisterRoute(nameof(Views.EditNovelPage), typeof(Views.EditNovelPage));
        Routing.RegisterRoute(nameof(Views.ManageNovelsPage), typeof(Views.ManageNovelsPage));
        Routing.RegisterRoute(nameof(Views.ManageGenresPage), typeof(Views.ManageGenresPage));

        // Rutas del sistema de reseñas
        Routing.RegisterRoute(nameof(Views.ReviewsPage), typeof(Views.ReviewsPage));
        Routing.RegisterRoute(nameof(Views.WriteReviewPage), typeof(Views.WriteReviewPage));

        //Ruta del editor de capítulos
        Routing.RegisterRoute(nameof(Views.EditChapterPage), typeof(Views.EditChapterPage));

        // Rutas de navegación de capítulos
        Routing.RegisterRoute(nameof(Views.AddChapterPage), typeof(Views.AddChapterPage));
        Routing.RegisterRoute(nameof(Views.EditChapterPage), typeof(Views.EditChapterPage));

        // Rutas para categorías populares
        Routing.RegisterRoute(nameof(Views.PopularGenresPage), typeof(Views.PopularGenresPage));
        Routing.RegisterRoute(nameof(Views.GenreDetailPage), typeof(Views.GenreDetailPage));
        Routing.RegisterRoute(nameof(Views.AllGenresPage), typeof(Views.AllGenresPage));

        // Ruta para la página de novelas por autor
        Routing.RegisterRoute(nameof(Views.AuthorNovelsPage), typeof(Views.AuthorNovelsPage));

        // Rutas para el sistema de categorías personalizadas
        Routing.RegisterRoute(nameof(Views.CategoriesPage), typeof(Views.CategoriesPage));
        Routing.RegisterRoute(nameof(Views.CreateCategoryPage), typeof(Views.CreateCategoryPage));
        Routing.RegisterRoute(nameof(Views.CategoryNovelsPage), typeof(Views.CategoryNovelsPage));
        Routing.RegisterRoute(nameof(Views.AddNovelsToCategoryPage), typeof(Views.AddNovelsToCategoryPage));

        // Ruta para la página de logros
        Routing.RegisterRoute(nameof(Views.AchievementsPage), typeof(Views.AchievementsPage));

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
    /// Navega al tab de Explorar
    /// </summary>
    public async Task NavigateToExplorePage()
    {
        try
        {
            // Método 1: Cambiar de tab directamente
            if (this.CurrentItem is TabBar tabBar)
            {
                // Buscar el tab de Explorar por título o route
                var exploreTab = tabBar.Items.FirstOrDefault(tab =>
                    tab.Title.Contains("Explorar") ||
                    tab.Route == "ExploreTab");

                if (exploreTab != null)
                {
                    // Cambiar al tab en el UI thread
                    await Device.InvokeOnMainThreadAsync(() =>
                    {
                        Shell.Current.CurrentItem = exploreTab;
                    });
                    return;
                }
            }

            // Método 2: Usar navegación por ruta
            await Shell.Current.GoToAsync("//ExploreTab");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error navegando a Explorar: {ex.Message}");

            // Método 3: Último recurso
            try
            {
                await Shell.Current.GoToAsync("//ExplorePage");
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Fallo navegación alternativa");
            }
        }
    }
}