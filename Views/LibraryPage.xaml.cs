using NovelBook.Services;
using NovelBook.Models;

namespace NovelBook.Views;

public partial class LibraryPage : ContentPage
{
    private readonly LibraryService _libraryService;
    private readonly DatabaseService _databaseService;
    private readonly ImageService _imageService;

    private List<UserLibraryItem> _allLibraryItems = new List<UserLibraryItem>();
    private List<UserLibraryItem> _filteredItems = new List<UserLibraryItem>();
    private string _currentFilter = "all";
    private string _currentSort = "recent";

    public LibraryPage()
    {
        InitializeComponent();

        _databaseService = new DatabaseService();
        _libraryService = new LibraryService(_databaseService, new AuthService(_databaseService));
        _imageService = new ImageService(_databaseService);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadLibrary();
    }

    /// <summary>
    /// Carga todas las novelas de la biblioteca del usuario
    /// </summary>
    private async Task LoadLibrary()
    {
        try
        {
            // Verificar si hay usuario logueado
            if (AuthService.CurrentUser == null)
            {
                ShowEmptyState("Inicia sesión para ver tu biblioteca");
                return;
            }

            // Mostrar indicador de carga
            novelsCollection.IsVisible = false;
            EmptyStateLayout.IsVisible = false;
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            // Cargar biblioteca desde la base de datos
            _allLibraryItems = await _libraryService.GetUserLibraryAsync();

            if (_allLibraryItems.Count == 0)
            {
                ShowEmptyState("Tu biblioteca está vacía");
            }
            else
            {
                // Aplicar filtro y ordenamiento actuales
                await ApplyFilterAndSort();
                novelsCollection.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error al cargar biblioteca: " + ex.Message, "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    /// <summary>
    /// Aplica el filtro y ordenamiento actual
    /// </summary>
    private async Task ApplyFilterAndSort()
    {
        // Aplicar filtro
        _filteredItems = _currentFilter switch
        {
            "reading" => _allLibraryItems.Where(i => i.ReadingStatus == "reading").ToList(),
            "completed" => _allLibraryItems.Where(i => i.ReadingStatus == "completed").ToList(),
            "favorites" => _allLibraryItems.Where(i => i.IsFavorite).ToList(),
            _ => _allLibraryItems
        };

        // Aplicar ordenamiento
        _filteredItems = _currentSort switch
        {
            "recent" => _filteredItems.OrderByDescending(i => i.AddedAt).ToList(),
            "alphabetical" => _filteredItems.OrderBy(i => i.Novel.Title).ToList(),
            "lastRead" => _filteredItems.OrderByDescending(i => i.LastReadChapter).ToList(),
            _ => _filteredItems
        };

        // Convertir a DisplayItems
        var displayItems = new List<LibraryDisplayItem>();
        foreach (var item in _filteredItems)
        {
            // Cargar imagen
            var coverImage = await _imageService.GetCoverImageAsync(item.Novel.CoverImage);

            // Calcular capítulos sin leer
            var unreadChapters = Math.Max(0, item.Novel.ChapterCount - item.LastReadChapter);

            displayItems.Add(new LibraryDisplayItem
            {
                LibraryItemId = item.Id,
                NovelId = item.NovelId,
                Title = item.Novel.Title,
                CoverImageSource = coverImage,
                UnreadCount = unreadChapters,
                ShowUnreadBadge = unreadChapters > 0,
                IsFavorite = item.IsFavorite,
                ReadingStatus = item.ReadingStatus,
                Progress = item.Novel.ChapterCount > 0 ?
                    (double)item.LastReadChapter / item.Novel.ChapterCount : 0
            });
        }

        novelsCollection.ItemsSource = displayItems;
    }

    /// <summary>
    /// Muestra el estado vacío con un mensaje
    /// </summary>
    private void ShowEmptyState(string message)
    {
        EmptyStateLayout.IsVisible = true;
        EmptyStateMessage.Text = message;
        novelsCollection.IsVisible = false;
    }

    /// <summary>
    /// Maneja el cambio de filtros
    /// </summary>
    private async void OnFilterClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            // Actualizar apariencia de botones
            UpdateFilterButtons(button);

            // Obtener el nuevo filtro
            _currentFilter = button.Text.ToLower() switch
            {
                "leyendo" => "reading",
                "completados" => "completed",
                "favoritos" => "favorites",
                _ => "all"
            };

            // Aplicar filtro
            await ApplyFilterAndSort();
        }
    }

    /// <summary>
    /// Actualiza la apariencia de los botones de filtro
    /// </summary>
    private void UpdateFilterButtons(Button activeButton)
    {
        var filterButtons = FilterLayout.Children.OfType<Button>();
        foreach (var btn in filterButtons)
        {
            if (btn == activeButton)
            {
                btn.BackgroundColor = Color.FromArgb("#8B5CF6");
                btn.TextColor = Colors.White;
            }
            else
            {
                btn.BackgroundColor = Color.FromArgb("#2D2D2D");
                btn.TextColor = Color.FromArgb("#B0B0B0");
            }
        }
    }

    /// <summary>
    /// Maneja la búsqueda en la biblioteca
    /// </summary>
    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(searchText))
        {
            // Si no hay texto, mostrar todos
            await ApplyFilterAndSort();
        }
        else
        {
            // Filtrar por búsqueda
            var searchResults = _filteredItems.Where(i =>
                i.Novel.Title.ToLower().Contains(searchText) ||
                i.Novel.Author.ToLower().Contains(searchText)
            ).ToList();

            // Convertir a DisplayItems
            var displayItems = new List<LibraryDisplayItem>();
            foreach (var item in searchResults)
            {
                var coverImage = await _imageService.GetCoverImageAsync(item.Novel.CoverImage);
                var unreadChapters = Math.Max(0, item.Novel.ChapterCount - item.LastReadChapter);

                displayItems.Add(new LibraryDisplayItem
                {
                    LibraryItemId = item.Id,
                    NovelId = item.NovelId,
                    Title = item.Novel.Title,
                    CoverImageSource = coverImage,
                    UnreadCount = unreadChapters,
                    ShowUnreadBadge = unreadChapters > 0,
                    IsFavorite = item.IsFavorite,
                    ReadingStatus = item.ReadingStatus,
                    Progress = item.Novel.ChapterCount > 0 ?
                        (double)item.LastReadChapter / item.Novel.ChapterCount : 0
                });
            }

            novelsCollection.ItemsSource = displayItems;
        }
    }

    /// <summary>
    /// Maneja el tap en una novela
    /// </summary>
    private async void OnNovelTapped(object sender, EventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is LibraryDisplayItem item)
        {
            await Navigation.PushAsync(new NovelDetailPage(item.NovelId));
        }
    }

    /// <summary>
    /// Maneja el long press para mostrar opciones
    /// </summary>
    private async void OnNovelLongPressed(object sender, EventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is LibraryDisplayItem item)
        {
            var action = await DisplayActionSheet(
                item.Title,
                "Cancelar",
                "Eliminar de biblioteca",
                "Marcar como favorito",
                "Cambiar estado de lectura",
                "Ver detalles"
            );

            switch (action)
            {
                case "Eliminar de biblioteca":
                    await RemoveFromLibrary(item);
                    break;
                case "Marcar como favorito":
                    await ToggleFavorite(item);
                    break;
                case "Cambiar estado de lectura":
                    await ChangeReadingStatus(item);
                    break;
                case "Ver detalles":
                    await Navigation.PushAsync(new NovelDetailPage(item.NovelId));
                    break;
            }
        }
    }

    private async Task RemoveFromLibrary(LibraryDisplayItem item)
    {
        var confirm = await DisplayAlert(
            "Confirmar",
            $"¿Eliminar '{item.Title}' de tu biblioteca?",
            "Sí", "No"
        );

        if (confirm)
        {
            var success = await _libraryService.RemoveFromLibraryAsync(item.NovelId);
            if (success)
            {
                await LoadLibrary();
            }
        }
    }

    private async Task ToggleFavorite(LibraryDisplayItem item)
    {
        var success = await _libraryService.ToggleFavoriteAsync(item.NovelId);
        if (success)
        {
            await LoadLibrary();
        }
    }

    private async Task ChangeReadingStatus(LibraryDisplayItem item)
    {
        var newStatus = await DisplayActionSheet(
            "Cambiar estado",
            "Cancelar",
            null,
            "Leyendo",
            "Completado",
            "Abandonado",
            "Planeado"
        );

        if (newStatus != "Cancelar" && newStatus != null)
        {
            var statusMap = new Dictionary<string, string>
            {
                { "Leyendo", "reading" },
                { "Completado", "completed" },
                { "Abandonado", "dropped" },
                { "Planeado", "plan_to_read" }
            };

            var success = await _libraryService.UpdateReadingStatusAsync(
                item.NovelId,
                statusMap[newStatus]
            );

            if (success)
            {
                await LoadLibrary();
            }
        }
    }

    /// <summary>
    /// Mostrar menú de ordenamiento
    /// </summary>
    private async void OnSortClicked(object sender, EventArgs e)
    {
        var action = await DisplayActionSheet(
            "Ordenar por",
            "Cancelar",
            null,
            "Más reciente",
            "Alfabético",
            "Último leído"
        );

        _currentSort = action switch
        {
            "Más reciente" => "recent",
            "Alfabético" => "alphabetical",
            "Último leído" => "lastRead",
            _ => _currentSort
        };

        if (action != "Cancelar" && action != null)
        {
            await ApplyFilterAndSort();
        }
    }
}

/// <summary>
/// Clase para mostrar items en la UI
/// </summary>
public class LibraryDisplayItem
{
    public int LibraryItemId { get; set; }
    public int NovelId { get; set; }
    public string Title { get; set; }
    public ImageSource CoverImageSource { get; set; }
    public int UnreadCount { get; set; }
    public bool ShowUnreadBadge { get; set; }
    public bool IsFavorite { get; set; }
    public string ReadingStatus { get; set; }
    public double Progress { get; set; }
}