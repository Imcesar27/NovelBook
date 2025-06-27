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
    /// ARREGLO 6: Convierte un UserLibraryItem a LibraryDisplayItem con cálculos correctos
    /// </summary>
    public static async Task<LibraryDisplayItem> FromUserLibraryItem(UserLibraryItem item, ImageService imageService)
    {
        var coverImage = await imageService.GetCoverImageAsync(item.Novel.CoverImage);

        // ARREGLO 6: Cálculos de progreso corregidos
        int totalChapters = item.Novel.ChapterCount;
        int lastRead = item.LastReadChapter;

        // Calcular capítulos sin leer correctamente
        int unreadCount = Math.Max(0, totalChapters - lastRead);

        // Calcular progreso como porcentaje (0.0 - 1.0)
        double progress = totalChapters > 0 ? (double)lastRead / totalChapters : 0.0;

        return new LibraryDisplayItem
        {
            LibraryItemId = item.Id,
            NovelId = item.Novel.Id,
            Title = item.Novel.Title,
            Author = item.Novel.Author,
            CoverImageSource = coverImage,
            ChapterCount = totalChapters,
            LastReadChapter = lastRead,
            UnreadCount = unreadCount,
            Progress = progress, // 0.0 = 0%, 1.0 = 100%
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
    private string _currentFilter = "📔 Todos";

    // ARREGLO 3: Variables para long press mejorado
    private bool _isProcessingTap = false;

    public LibraryPage()
    {
        InitializeComponent();

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _libraryService = new LibraryService(_databaseService, new AuthService(_databaseService));
        _imageService = new ImageService(_databaseService);

        // Inicializar listas
        _libraryItems = new List<UserLibraryItem>();
        _filteredItems = new List<UserLibraryItem>();
    }

    /// <summary>
    /// Se ejecuta cada vez que aparece la página - carga los datos actualizados
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadLibraryAsync();
    }

    /// <summary>
    /// Carga las novelas de la librería personal del usuario
    /// </summary>
    private async Task LoadLibraryAsync()
    {
        try
        {
            // Mostrar indicador de carga
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            // Verificar si hay usuario logueado
            if (AuthService.CurrentUser == null)
            {
                ShowEmptyState("Inicia sesión para ver tu biblioteca");
                return;
            }

            // Obtener novelas de la librería del usuario
            _libraryItems = await _libraryService.GetUserLibraryAsync();

            if (_libraryItems == null || _libraryItems.Count == 0)
            {
                ShowEmptyState("Tu biblioteca está vacía");
                return;
            }

            // Procesar las novelas para mostrar en la UI
            var displayItems = new List<LibraryDisplayItem>();

            foreach (var item in _libraryItems)
            {
                var displayItem = await LibraryDisplayItem.FromUserLibraryItem(item, _imageService);
                displayItems.Add(displayItem);
            }

            // Actualizar la interfaz en el hilo principal
            Device.BeginInvokeOnMainThread(() =>
            {
                novelsCollection.ItemsSource = displayItems;
                EmptyStateLayout.IsVisible = false;
                novelsCollection.IsVisible = true;
            });

            // Aplicar filtro actual
            ApplyFilter(_currentFilter);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando librería: {ex.Message}");
            Device.BeginInvokeOnMainThread(() =>
            {
                ShowEmptyState("Error al cargar la biblioteca");
            });
        }
        finally
        {
            // Ocultar indicador de carga
            Device.BeginInvokeOnMainThread(() =>
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            });
        }
    }

    /// <summary>
    /// Muestra el estado vacío con un mensaje personalizado
    /// </summary>
    private void ShowEmptyState(string message)
    {
        Device.BeginInvokeOnMainThread(() =>
        {
            EmptyStateMessage.Text = message;
            EmptyStateLayout.IsVisible = true;
            novelsCollection.IsVisible = false;
            LoadingIndicator.IsVisible = false;
        });
    }

    /// <summary>
    /// ARREGLO 3: Tap simple mejorado
    /// </summary>
    private async void OnNovelTapped(object sender, EventArgs e)
    {
        if (_isProcessingTap) return;

        try
        {
            _isProcessingTap = true;

            if (sender is Frame frame && frame.BindingContext is LibraryDisplayItem novel)
            {
                await Navigation.PushAsync(new NovelDetailPage(novel.NovelId));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo abrir la novela", "OK");
            System.Diagnostics.Debug.WriteLine($"Error al abrir novela: {ex.Message}");
        }
        finally
        {
            _isProcessingTap = false;
        }
    }

    /// <summary>
    /// ARREGLO 3: Long press separado y funcional
    /// </summary>
    private async void OnNovelLongPressed(object sender, EventArgs e)
    {
        if (_isProcessingTap) return;

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
    }

    /// <summary>
    /// Maneja las acciones del menú contextual
    /// </summary>
    private async Task HandleNovelAction(string action, int novelId)
    {
        try
        {
            switch (action)
            {
                case "Marcar como favorito":
                    await _libraryService.ToggleFavoriteAsync(novelId);
                    await LoadLibraryAsync(); // Recargar para actualizar UI
                    break;

                case "Cambiar estado de lectura":
                    var newStatus = await DisplayActionSheet(
                        "Nuevo estado",
                        "Cancelar",
                        null,
                        "Leyendo",
                        "Completado",
                        "En pausa",
                        "Planeo leer");

                    if (newStatus != "Cancelar" && !string.IsNullOrEmpty(newStatus))
                    {
                        var statusMap = new Dictionary<string, string>
                        {
                            {"Leyendo", "reading"},
                            {"Completado", "completed"},
                            {"En pausa", "paused"},
                            {"Planeo leer", "plan_to_read"}
                        };

                        if (statusMap.ContainsKey(newStatus))
                        {
                            await _libraryService.UpdateReadingStatusAsync(novelId, statusMap[newStatus]);
                            await LoadLibraryAsync();
                        }
                    }
                    break;

                case "Eliminar de biblioteca":
                    var confirm = await DisplayAlert(
                        "Confirmar",
                        "¿Eliminar esta novela de tu biblioteca?",
                        "Sí",
                        "No");

                    if (confirm)
                    {
                        await _libraryService.RemoveFromLibraryAsync(novelId);
                        await LoadLibraryAsync();
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo realizar la acción", "OK");
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
        // Resetear todos los botones a estado no seleccionado
        foreach (var child in FilterLayout.Children)
        {
            if (child is Button btn)
            {
                btn.BackgroundColor = Color.FromArgb("#2D2D2D");
                btn.TextColor = Color.FromArgb("#B0B0B0");
            }
        }

        // Marcar el botón seleccionado
        selectedButton.BackgroundColor = Color.FromArgb("#8B5CF6");
        selectedButton.TextColor = Colors.White;

        _currentFilter = selectedButton.Text;
    }

    /// <summary>
    /// ARREGLO 2: Filtros actualizados con todos los estados
    /// </summary>
    private void ApplyFilter(string filterText)
    {
        if (_libraryItems == null) return;

        List<UserLibraryItem> filteredItems = filterText switch
        {
            "🧾 Leyendo" => _libraryItems.Where(x => x.ReadingStatus == "reading").ToList(),
            "✔️ Completados" => _libraryItems.Where(x => x.ReadingStatus == "completed").ToList(),
            "⭐ Favoritos" => _libraryItems.Where(x => x.IsFavorite).ToList(),
            "⏸️ En pausa" => _libraryItems.Where(x => x.ReadingStatus == "paused").ToList(),
            "📋 Planeo leer" => _libraryItems.Where(x => x.ReadingStatus == "plan_to_read").ToList(),
            _ => _libraryItems // "📔 Todos"
        };

        // Actualizar la colección mostrada
        UpdateDisplayItems(filteredItems);
    }

    /// <summary>
    /// Actualiza los elementos mostrados en la interfaz
    /// </summary>
    private async void UpdateDisplayItems(List<UserLibraryItem> items)
    {
        var displayItems = new List<LibraryDisplayItem>();

        foreach (var item in items)
        {
            var displayItem = await LibraryDisplayItem.FromUserLibraryItem(item, _imageService);
            displayItems.Add(displayItem);
        }

        Device.BeginInvokeOnMainThread(() =>
        {
            novelsCollection.ItemsSource = displayItems;

            if (displayItems.Count == 0)
            {
                var filterName = _currentFilter.Replace("📔 ", "").Replace("🧾 ", "").Replace("✔️ ", "").Replace("⭐ ", "").Replace("⏸️ ", "").Replace("📋 ", "");
                ShowEmptyState($"No hay novelas en: {filterName}");
            }
            else
            {
                EmptyStateLayout.IsVisible = false;
                novelsCollection.IsVisible = true;
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
            "Ordenar por",
            "Cancelar",
            null,
            "Añadido recientemente",
            "Título (A-Z)",
            "Título (Z-A)",
            "Último leído",
            "Más capítulos",
            "Menos capítulos");

        if (sortOption != "Cancelar" && !string.IsNullOrEmpty(sortOption))
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

        List<UserLibraryItem> sortedItems = sortOption switch
        {
            "Añadido recientemente" => _libraryItems.OrderByDescending(x => x.AddedAt).ToList(),
            "Título (A-Z)" => _libraryItems.OrderBy(x => x.Novel.Title).ToList(),
            "Título (Z-A)" => _libraryItems.OrderByDescending(x => x.Novel.Title).ToList(),
            "Último leído" => _libraryItems.OrderByDescending(x => x.LastReadChapter).ToList(),
            "Más capítulos" => _libraryItems.OrderByDescending(x => x.Novel.ChapterCount).ToList(),
            "Menos capítulos" => _libraryItems.OrderBy(x => x.Novel.ChapterCount).ToList(),
            _ => _libraryItems
        };

        // Aplicar el filtro actual a los elementos ordenados
        _libraryItems = sortedItems;
        ApplyFilter(_currentFilter);
    }

    /// <summary>
    /// ARREGLO 1: Navegación a explorar arreglada
    /// </summary>
    private async void OnExploreNovelsClicked(object sender, EventArgs e)
    {
        try
        {
            // Usar GoToAsync con ruta específica
            await Shell.Current.GoToAsync("//ExplorePage");
        }
        catch (Exception ex)
        {
            try
            {
                // Método alternativo si falla el primero
                await Shell.Current.GoToAsync("///ExplorePage");
            }
            catch
            {
                // Último recurso - cambiar tab manualmente
                if (Shell.Current is AppShell appShell)
                {
                    appShell.CurrentItem = appShell.Items.FirstOrDefault(item =>
                        item.Route.Contains("Explorar") || item.Title.Contains("Explorar"));
                }
            }
            System.Diagnostics.Debug.WriteLine($"Error navegando a explorar: {ex.Message}");
        }
    }
}