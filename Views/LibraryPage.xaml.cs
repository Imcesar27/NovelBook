using Microsoft.Maui.Controls;
using NovelBook.Services;
using NovelBook.Models;

namespace NovelBook.Views;

/// <summary>
/// Clase para representar una novela en la UI de la librería personal
/// </summary>
public class LibraryDisplayItem
{
    public int LibraryItemId { get; set; }  // ID del registro en user_library
    public int NovelId { get; set; }        // ID de la novela
    public string Title { get; set; }
    public string Author { get; set; }
    public ImageSource CoverImageSource { get; set; }
    public int ChapterCount { get; set; }
    public int LastReadChapter { get; set; }
    public int UnreadCount { get; set; }
    public bool ShowUnreadBadge => UnreadCount > 0;
    public double Progress { get; set; }
    public bool IsFavorite { get; set; }
    public string ReadingStatus { get; set; }
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// Convierte un UserLibraryItem a LibraryDisplayItem con cálculos de capítulos
    /// </summary>
    public static async Task<LibraryDisplayItem> FromUserLibraryItem(UserLibraryItem item, ImageService imageService)
    {
        var coverImage = await imageService.GetCoverImageAsync(item.Novel.CoverImage);

        // Cálculos de capítulos
        int totalChapters = item.Novel.ChapterCount;
        int lastReadChapter = item.LastReadChapter;

        // Los capítulos sin leer son: total - último leído
        // Si tienes 5 capítulos y leíste hasta el 3, te quedan 2 por leer
        int unreadCount = Math.Max(0, totalChapters - lastReadChapter);

        // El progreso es el porcentaje de capítulos leídos
        double progress = totalChapters > 0 ? (double)lastReadChapter / totalChapters : 0.0;

        return new LibraryDisplayItem
        {
            LibraryItemId = item.Id,
            NovelId = item.Novel.Id,
            Title = item.Novel.Title,
            Author = item.Novel.Author,
            CoverImageSource = coverImage,
            ChapterCount = totalChapters,
            LastReadChapter = lastReadChapter, // Este es el número del último capítulo leído
            UnreadCount = unreadCount,
            Progress = progress,
            IsFavorite = item.IsFavorite,
            ReadingStatus = item.ReadingStatus,
            AddedAt = item.AddedAt
        };
    }
}

public partial class LibraryPage : ContentPage
{
    // Servicios para manejar datos
    private readonly LibraryService _libraryService;
    private readonly DatabaseService _databaseService;
    private readonly ImageService _imageService;

    // Lista para almacenar las novelas de la librería
    private List<UserLibraryItem> _libraryItems;
    private List<UserLibraryItem> _filteredItems;

    // Estado actual del filtro
    private string _currentFilter;

    // Variables para long press mejorado
    private bool _isProcessingTap = false;

    public LibraryPage()
    {
        InitializeComponent();
        DebugShellStructure();

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _libraryService = new LibraryService(_databaseService, new AuthService(_databaseService));
        _imageService = new ImageService(_databaseService);

        // Inicializar listas
        _libraryItems = new List<UserLibraryItem>();
        _filteredItems = new List<UserLibraryItem>();

        // Inicializar textos de botones con traducciones
        InitializeFilterButtonTexts();

        _currentFilter = AllButton.Text; // Usar el texto traducido del botón
    }

    /// <summary>
    /// Inicializa los textos de los botones de filtro con las traducciones
    /// </summary>
    private void InitializeFilterButtonTexts()
    {
        AllButton.Text = $"📔 {LocalizationService.GetString("All")}";
        ReadingButton.Text = $"🧾 {LocalizationService.GetString("Reading")}";
        CompletedButton.Text = $"✔️ {LocalizationService.GetString("Completed")}";
        FavoritesButton.Text = $"⭐ {LocalizationService.GetString("Favorites")}";
        DroppedButton.Text = $"⏸️ {LocalizationService.GetString("Dropped")}";
        PlanToReadButton.Text = $"📋 {LocalizationService.GetString("PlanToRead")}";
    }

    /// <summary>
    /// Se ejecuta cada vez que aparece la página - carga los datos actualizados
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            InitializeFilterButtonTexts(); // Actualizar textos por si cambió el idioma
            System.Diagnostics.Debug.WriteLine($"LibraryPage.OnAppearing - Filtro actual: {_currentFilter}");

            // Si estamos en un filtro vacío, volver a "Todos"
            if (_libraryItems != null && _libraryItems.Count > 0)
            {
                var currentFilteredItems = GetFilteredItems(_currentFilter);
                if (currentFilteredItems.Count == 0 && _currentFilter != "📔 Todos")
                {
                    System.Diagnostics.Debug.WriteLine("Filtro vacío detectado, cambiando a Todos");

                    // Resetear visualmente el filtro
                    ResetFilterToAll();

                    // Aplicar filtro "Todos"
                    _currentFilter = AllButton.Text;
                }
            }

            // Siempre recargar la biblioteca
            await LoadLibraryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en OnAppearing: {ex.Message}");
        }
    }

    /// <summary>
    /// Resetea visualmente el filtro a "Todos"
    /// </summary>
    private void ResetFilterToAll()
    {
        Device.BeginInvokeOnMainThread(() =>
        {
            if (FilterLayout != null && FilterLayout.Children.Count > 0)
            {
                // Buscar el botón "Todos"
                var todosButton = FilterLayout.Children
                    .OfType<Button>()
                    .FirstOrDefault(b => b.Text.Contains("Todos"));

                if (todosButton != null)
                {
                    UpdateFilterButtons(todosButton);
                }
            }
        });
    }

    /// <summary>
    /// Obtiene los items filtrados sin actualizar la UI
    /// </summary>
    private List<UserLibraryItem> GetFilteredItems(string filterKey)
    {
        if (_libraryItems == null) return new List<UserLibraryItem>();

        // Eliminar emojis y espacios para comparar
        var cleanFilterKey = filterKey?.Replace("📔", "").Replace("🧾", "").Replace("✔️", "")
                                      .Replace("⭐", "").Replace("⏸️", "").Replace("📋", "")
                                      .Trim() ?? "";

        // Usar las traducciones para comparar
        var reading = LocalizationService.GetString("Reading");
        var completed = LocalizationService.GetString("Completed");
        var favorites = LocalizationService.GetString("Favorites");
        var dropped = LocalizationService.GetString("Dropped");
        var planToRead = LocalizationService.GetString("PlanToRead");

        return cleanFilterKey switch
        {
            var f when f == reading => _libraryItems.Where(x => x.ReadingStatus == "reading").ToList(),
            var f when f == completed => _libraryItems.Where(x => x.ReadingStatus == "completed").ToList(),
            var f when f == favorites => _libraryItems.Where(x => x.IsFavorite).ToList(),
            var f when f == dropped => _libraryItems.Where(x => x.ReadingStatus == "dropped").ToList(),
            var f when f == planToRead => _libraryItems.Where(x => x.ReadingStatus == "plan_to_read").ToList(),
            _ => _libraryItems
        };
    }

    /// <summary>
    /// Carga las novelas de la librería personal del usuario
    /// </summary>
    private async Task LoadLibraryAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("LoadLibraryAsync - Iniciando carga");

            // Mostrar indicador de carga
            Device.BeginInvokeOnMainThread(() =>
            {
                if (LoadingIndicator != null)
                {
                    LoadingIndicator.IsVisible = true;
                    LoadingIndicator.IsRunning = true;
                }

                // Ocultar temporalmente la colección
                if (novelsCollection != null)
                    novelsCollection.IsVisible = false;
            });

            // Verificar si hay usuario logueado
            if (AuthService.CurrentUser == null)
            {
                ShowEmptyState(LocalizationService.GetString("LoginToSeeLibrary"));
                return;
            }

            // Sincronizar progreso
            var chapterService = new ChapterService(_databaseService);
            _libraryItems = await _libraryService.GetUserLibraryAsync();

            System.Diagnostics.Debug.WriteLine($"Novelas cargadas: {_libraryItems?.Count ?? 0}");

            // Sincronizar progreso de cada novela
            if (_libraryItems != null && _libraryItems.Count > 0)
            {
                foreach (var item in _libraryItems)
                {
                    await chapterService.SyncUserLibraryProgressAsync(
                        AuthService.CurrentUser.Id,
                        item.NovelId
                    );
                }

                // Recargar con datos actualizados
                _libraryItems = await _libraryService.GetUserLibraryAsync();
            }

            if (_libraryItems == null || _libraryItems.Count == 0)
            {
                ShowEmptyState(LocalizationService.GetString("LibraryEmpty"));
                return;
            }

            // Aplicar el filtro actual y mostrar
            await RefreshDisplay();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando librería: {ex.Message}");
            ShowEmptyState(LocalizationService.GetString("ErrorLoadingLibrary"));
        }
        finally
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (LoadingIndicator != null)
                {
                    LoadingIndicator.IsVisible = false;
                    LoadingIndicator.IsRunning = false;
                }
            });
        }
    }

    /// <summary>
    /// Refresca la visualización con el filtro actual
    /// </summary>
    private async Task RefreshDisplay()
    {
        if (_libraryItems == null) return;

        // Obtener items filtrados
        var filteredItems = GetFilteredItems(_currentFilter);

        System.Diagnostics.Debug.WriteLine($"RefreshDisplay - Filtro: {_currentFilter}, Items: {filteredItems.Count}");

        // Si el filtro actual no tiene items pero hay items en total, cambiar a "Todos"
        if (filteredItems.Count == 0 && _libraryItems.Count > 0 && _currentFilter != "📔 Todos")
        {
            System.Diagnostics.Debug.WriteLine("Filtro vacío, cambiando a Todos");
            _currentFilter = "📔 Todos";
            filteredItems = _libraryItems;
            ResetFilterToAll();
        }

        // Procesar para mostrar
        await UpdateDisplayItems(filteredItems);
    }

    /// <summary>
    /// Muestra el estado vacío con un mensaje personalizado
    /// </summary>
    private void ShowEmptyState(string message)
    {
        Device.BeginInvokeOnMainThread(() =>
        {
            // Ocultar la colección y su ScrollView
            if (novelsCollection != null)
                novelsCollection.IsVisible = false;

            // Si tienes un ScrollView padre, ocúltalo también
            var scrollView = this.FindByName<ScrollView>("CollectionScrollView");
            if (scrollView != null)
                scrollView.IsVisible = false;

            // Mostrar el estado vacío
            EmptyStateMessage.Text = message;
            EmptyStateLayout.IsVisible = true;
            LoadingIndicator.IsVisible = false;

            // Asegurar que el EmptyStateLayout esté al frente
            if (EmptyStateLayout.Parent is Grid parentGrid)
            {
                parentGrid.Children.Remove(EmptyStateLayout);
                parentGrid.Children.Add(EmptyStateLayout); // Re-agregar al final
            }
        });
    }

    /// <summary>
    /// Tap simple
    /// </summary>
    private async void OnNovelTapped(object sender, EventArgs e)
    {
        try
        {
            LibraryDisplayItem novel = null;

            // Buscar el item de diferentes maneras
            if (sender is Frame frame)
            {
                novel = frame.BindingContext as LibraryDisplayItem;
            }
            else if (sender is Grid grid)
            {
                novel = grid.BindingContext as LibraryDisplayItem;
            }

            if (novel != null)
            {
                System.Diagnostics.Debug.WriteLine($"Abriendo novela: {novel.Title} (ID: {novel.NovelId})");
                await Navigation.PushAsync(new NovelDetailPage(novel.NovelId));
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No se pudo obtener el contexto de la novela");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo abrir la novela: {ex.Message}", "OK");
            System.Diagnostics.Debug.WriteLine($"Error completo: {ex}");
        }
    }

    /// <summary>
    /// Long press separado y funcional
    /// </summary>

    // <summary>
    /// Maneja el click en el botón de menú (tres puntos)
    /// </summary>
    private async void OnNovelMenuClicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is Button button && button.BindingContext is LibraryDisplayItem novel)
            {
                var action = await DisplayActionSheet(
                    $"{LocalizationService.GetString("OptionsFor")} {novel.Title}",
                    LocalizationService.GetString("Cancel"),
                    null,
                    novel.IsFavorite ?
                        LocalizationService.GetString("RemoveFromFavorites") :
                        LocalizationService.GetString("AddToFavorites"),
                    LocalizationService.GetString("ChangeStatus"),
                    LocalizationService.GetString("RemoveFromLibrary"));

                await HandleNovelAction(action, novel);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en menú: {ex.Message}");
        }
    }

    /* private async void OnNovelLongPressed(object sender, EventArgs e)
     {
         try
         {
             if (sender is Button button && button.BindingContext is LibraryDisplayItem novel)
             {
                 // Vibración para feedback
                 try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }

                 var action = await DisplayActionSheet(
                     $"Opciones para: {novel.Title}",
                     "Cancelar",
                     null,
                     "Marcar como favorito",
                     "Cambiar estado de lectura",
                     "Eliminar de biblioteca");

                 await HandleNovelAction(action, novel.NovelId);
             }
         }
         catch (Exception ex)
         {
             System.Diagnostics.Debug.WriteLine($"Error en long press: {ex.Message}");
         }
     }*/

    /// <summary>
    /// Maneja las acciones del menú contextual
    /// </summary>
    private async Task HandleNovelAction(string action, LibraryDisplayItem novel)
    {
        try
        {
            // Para las comparaciones, usar las traducciones
            var removeFromFavorites = LocalizationService.GetString("RemoveFromFavorites");
            var addToFavorites = LocalizationService.GetString("AddToFavorites");
            var changeStatus = LocalizationService.GetString("ChangeStatus");
            var removeFromLibrary = LocalizationService.GetString("RemoveFromLibrary");

            if (action == removeFromFavorites || action == addToFavorites)
            {
                // CAMBIO: Usar el nuevo método que acepta LibraryItemId
                var success = await _libraryService.ToggleFavoriteByLibraryItemIdAsync(novel.LibraryItemId);

                if (success)
                {
                    await LoadLibraryAsync();

                    // Mostrar confirmación
                    await DisplayAlert(
                        LocalizationService.GetString("Success"),
                        action == addToFavorites ?
                            LocalizationService.GetString("MarkedAsFavorite") :
                            LocalizationService.GetString("RemovedFromFavorites"),
                        LocalizationService.GetString("OK"));
                }
                else
                {
                    await DisplayAlert(
                        LocalizationService.GetString("Error"),
                        LocalizationService.GetString("ErrorPerformingAction"),
                        LocalizationService.GetString("OK"));
                }
            }
            else if (action == changeStatus)
            {
                var newStatus = await DisplayActionSheet(
                    LocalizationService.GetString("ChangeStatusTo"),
                    LocalizationService.GetString("Cancel"),
                    null,
                    LocalizationService.GetString("Reading"),
                    LocalizationService.GetString("Completed"),
                    LocalizationService.GetString("Dropped"),
                    LocalizationService.GetString("PlanToRead"));

                if (!string.IsNullOrEmpty(newStatus) && newStatus != LocalizationService.GetString("Cancel"))
                {
                    // Mapear el texto traducido al valor de la base de datos
                    string statusValue = null;

                    if (newStatus == LocalizationService.GetString("Reading"))
                        statusValue = "reading";
                    else if (newStatus == LocalizationService.GetString("Completed"))
                        statusValue = "completed";
                    else if (newStatus == LocalizationService.GetString("Dropped"))
                        statusValue = "dropped";
                    else if (newStatus == LocalizationService.GetString("PlanToRead"))
                        statusValue = "plan_to_read";

                    if (statusValue != null)
                    {
                        // CAMBIO: Usar el nuevo método que acepta LibraryItemId
                        var success = await _libraryService.UpdateReadingStatusByLibraryItemIdAsync(novel.LibraryItemId, statusValue);

                        if (success)
                        {
                            await DisplayAlert(
                                LocalizationService.GetString("Success"),
                                LocalizationService.GetString("StatusUpdated"),
                                LocalizationService.GetString("OK"));
                            await LoadLibraryAsync();
                        }
                        else
                        {
                            await DisplayAlert(
                                LocalizationService.GetString("Error"),
                                LocalizationService.GetString("ErrorPerformingAction"),
                                LocalizationService.GetString("OK"));
                        }
                    }
                }
            }
            else if (action == removeFromLibrary)
            {
                var confirm = await DisplayAlert(
                    LocalizationService.GetString("ConfirmAction"),
                    $"{LocalizationService.GetString("RemoveFromLibrary")}?",
                    LocalizationService.GetString("Yes"),
                    LocalizationService.GetString("No"));

                if (confirm)
                {
                    // Este método sí usa NovelId, no LibraryItemId
                    var success = await _libraryService.RemoveFromLibraryAsync(novel.NovelId);

                    if (success)
                    {
                        await LoadLibraryAsync();
                    }
                    else
                    {
                        await DisplayAlert(
                            LocalizationService.GetString("Error"),
                            LocalizationService.GetString("ErrorPerformingAction"),
                            LocalizationService.GetString("OK"));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                LocalizationService.GetString("Error"),
                LocalizationService.GetString("ErrorPerformingAction"),
                LocalizationService.GetString("OK"));
            System.Diagnostics.Debug.WriteLine($"Error en acción: {ex.Message}");
        }
    }

    /// <summary>
    /// Maneja los filtros de la librería
    /// </summary>
    private void OnFilterClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            // Actualizar estado visual de los botones
            UpdateFilterButtons(button);

            // Aplicar filtro
            ApplyFilter(button.Text);
        }
    }

    /// <summary>
    /// Actualiza el estado visual de los botones de filtro
    /// </summary>
    private void UpdateFilterButtons(Button selectedButton)
    {
        // Restablecer todos los botones al estilo inactivo usando AppThemeBinding
        var inactiveBackgroundColor = Application.Current.RequestedTheme == AppTheme.Light
            ? (Color)Application.Current.Resources["BackgroundMediumLight"]
            : (Color)Application.Current.Resources["BackgroundMedium"];

        var inactiveTextColor = Application.Current.RequestedTheme == AppTheme.Light
            ? (Color)Application.Current.Resources["TextSecondaryLight"]
            : (Color)Application.Current.Resources["TextSecondary"];

        AllButton.BackgroundColor = inactiveBackgroundColor;
        AllButton.TextColor = inactiveTextColor;
        ReadingButton.BackgroundColor = inactiveBackgroundColor;
        ReadingButton.TextColor = inactiveTextColor;
        CompletedButton.BackgroundColor = inactiveBackgroundColor;
        CompletedButton.TextColor = inactiveTextColor;
        DroppedButton.BackgroundColor = inactiveBackgroundColor;
        DroppedButton.TextColor = inactiveTextColor;
        FavoritesButton.BackgroundColor = inactiveBackgroundColor;
        FavoritesButton.TextColor = inactiveTextColor;

        // Aplicar estilo activo al botón seleccionado
        var activeButton = _currentFilter switch
        {
            "all" => AllButton,
            "reading" => ReadingButton,
            "completed" => CompletedButton,
            "dropped" => DroppedButton,
            "favorites" => FavoritesButton,
            _ => AllButton
        };

        activeButton.BackgroundColor = (Color)Application.Current.Resources["Primary"];
        activeButton.TextColor = Colors.White;
    }

    /// <summary>
    /// Filtros actualizados con todos los estados
    /// </summary>
    private void ApplyFilter(string filterText)
    {
        if (_libraryItems == null) return;

        _currentFilter = filterText;
        var filteredItems = GetFilteredItems(filterText);

        // Actualizar la visualización
        Task.Run(async () => await UpdateDisplayItems(filteredItems));
    }
    /// <summary>
    /// Actualiza los elementos mostrados en la interfaz
    /// </summary>
    private async Task UpdateDisplayItems(List<UserLibraryItem> items)
    {
        var displayItems = new List<LibraryDisplayItem>();

        foreach (var item in items)
        {
            var displayItem = await LibraryDisplayItem.FromUserLibraryItem(item, _imageService);
            displayItems.Add(displayItem);
        }

        Device.BeginInvokeOnMainThread(() =>
        {
            System.Diagnostics.Debug.WriteLine($"Actualizando UI con {displayItems.Count} items");

            if (novelsCollection != null)
            {
                novelsCollection.ItemsSource = null; // Forzar refresh
                novelsCollection.ItemsSource = displayItems;
                novelsCollection.IsVisible = true;
            }

            var scrollView = this.FindByName<ScrollView>("CollectionScrollView");
            if (scrollView != null)
                scrollView.IsVisible = true;

            if (displayItems.Count == 0)
            {
                var filterName = _currentFilter.Replace("📔 ", "").Replace("🧾 ", "")
                    .Replace("✔️ ", "").Replace("⭐ ", "").Replace("⏸️ ", "").Replace("📋 ", "");
                ShowEmptyState(LocalizationService.GetString("NoNovelsInFilter", filterName));
            }
            else
            {
                if (EmptyStateLayout != null)
                    EmptyStateLayout.IsVisible = false;
            }
        });
    }

    /// <summary>
    /// Maneja la búsqueda en tiempo real
    /// </summary>
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(searchText))
        {
            // Si no hay búsqueda, aplicar filtro actual
            ApplyFilter(_currentFilter);
        }
        else
        {
            // Filtrar por texto de búsqueda
            if (_libraryItems != null)
            {
                var searchResults = _libraryItems.Where(x =>
                    x.Novel.Title.ToLower().Contains(searchText) ||
                    x.Novel.Author.ToLower().Contains(searchText)).ToList();

                UpdateDisplayItems(searchResults);
            }
        }
    }

    /// <summary>
    /// Maneja la ordenación de novelas
    /// </summary>
    private async void OnSortClicked(object sender, EventArgs e)
    {
        var sortOption = await DisplayActionSheet(
            LocalizationService.GetString("SortBy"),
            LocalizationService.GetString("Cancel"),
            null,
            LocalizationService.GetString("RecentlyAdded"),
            LocalizationService.GetString("TitleAZ"),
            LocalizationService.GetString("TitleZA"),
            LocalizationService.GetString("LastRead"),
            LocalizationService.GetString("MoreChapters"),
            LocalizationService.GetString("LessChapters"));

        if (sortOption != LocalizationService.GetString("Cancel") && !string.IsNullOrEmpty(sortOption))
        {
            ApplySort(sortOption);
        }
    }

    /// <summary>
    /// Aplica la ordenación seleccionada
    /// </summary>
    private void ApplySort(string sortOption)
    {
        if (_libraryItems == null) return;

        var recentlyAdded = LocalizationService.GetString("RecentlyAdded");
        var titleAZ = LocalizationService.GetString("TitleAZ");
        var titleZA = LocalizationService.GetString("TitleZA");
        var lastRead = LocalizationService.GetString("LastRead");
        var moreChapters = LocalizationService.GetString("MoreChapters");
        var lessChapters = LocalizationService.GetString("LessChapters");

        List<UserLibraryItem> sortedItems = sortOption switch
        {
            var s when s == recentlyAdded => _libraryItems.OrderByDescending(x => x.AddedAt).ToList(),
            var s when s == titleAZ => _libraryItems.OrderBy(x => x.Novel.Title).ToList(),
            var s when s == titleZA => _libraryItems.OrderByDescending(x => x.Novel.Title).ToList(),
            var s when s == lastRead => _libraryItems.OrderByDescending(x => x.LastReadChapter).ToList(),
            var s when s == moreChapters => _libraryItems.OrderByDescending(x => x.Novel.ChapterCount).ToList(),
            var s when s == lessChapters => _libraryItems.OrderBy(x => x.Novel.ChapterCount).ToList(),
            _ => _libraryItems
        };

        // Aplicar el filtro actual a los elementos ordenados
        _libraryItems = sortedItems;
        ApplyFilter(_currentFilter);
    }

    /// <summary>
    /// Navega a la página de explorar cuando no hay novelas en la biblioteca
    /// </summary>
    /// 
    private async void OnExploreNovelsClicked(object sender, EventArgs e)
    {
        try
        {
            // NO usar Navigation.PushAsync - usar Shell navigation
            if (Shell.Current?.CurrentItem is TabBar tabBar)
            {
                // Buscar y activar el tab de Explorar
                var exploreTab = tabBar.Items.FirstOrDefault(x => x.Title.Contains("Explorar"));
                if (exploreTab != null)
                {
                    await Device.InvokeOnMainThreadAsync(() =>
                    {
                        Shell.Current.CurrentItem = exploreTab;
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error navegando: {ex.Message}");

            // Si falla, intentar navegación por ruta
            try
            {
                await Shell.Current.GoToAsync("//ExplorePage");
            }
            catch
            {
                await DisplayAlert(
                  LocalizationService.GetString("Error"),
                  LocalizationService.GetString("NavigationError") ?? "No se pudo navegar a la página",
                  LocalizationService.GetString("OK"));
            }
        }
    }

    /// <summary>
    /// Maneja el tap en el nombre del autor para navegar a sus novelas
    /// </summary>
    private async void OnAuthorTapped(object sender, EventArgs e)
    {
        if (sender is Label label && label.GestureRecognizers[0] is TapGestureRecognizer tap)
        {
            string authorName = tap.CommandParameter as string;
            if (!string.IsNullOrEmpty(authorName))
            {
                await Navigation.PushAsync(new AuthorNovelsPage(authorName));
            }
        }
    }

    private void DebugShellStructure()
    {
        System.Diagnostics.Debug.WriteLine("=== ESTRUCTURA SHELL ===");

        if (Shell.Current != null)
        {
            System.Diagnostics.Debug.WriteLine($"Shell.Current: {Shell.Current.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"CurrentItem: {Shell.Current.CurrentItem?.GetType().Name}");

            if (Shell.Current.CurrentItem is TabBar tabBar)
            {
                System.Diagnostics.Debug.WriteLine($"CurrentItem en TabBar: {tabBar.CurrentItem?.Title}");

                foreach (var item in tabBar.Items)
                {
                    System.Diagnostics.Debug.WriteLine($"Tab: {item.Title}");
                    if (item.Items.Count > 0)
                    {
                        foreach (var content in item.Items)
                        {
                            System.Diagnostics.Debug.WriteLine($"  - Content: {content.Title}, Route: {content.Route}");
                        }
                    }
                }
            }
        }

        System.Diagnostics.Debug.WriteLine("========================");
    }

}