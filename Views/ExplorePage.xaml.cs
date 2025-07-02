using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class ExplorePage : ContentPage
{
    // Servicios
    private NovelService _novelService;
    private DatabaseService _databaseService;
    private ImageService _imageService;
    private GenreService _genreService;

    // Datos
    private List<Novel> _allNovels = new List<Novel>();
    private List<Novel> _displayedNovels = new List<Novel>();
    private List<PopularGenre> _popularGenres;
    private string _currentGenreFilter = "🔥 Todas";

    // Clase para datos de novela en UI
    public class NovelDisplayData
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Rating { get; set; }
        public string Chapters { get; set; }
        public ImageSource CoverImageSource { get; set; }
        public string Status { get; set; }
        public Color StatusColor { get; set; }
    }

    // Constructor
    public ExplorePage()
    {
        InitializeComponent();
        InitializeServices();
    }

    /// <summary>
    /// Inicializa los servicios necesarios
    /// </summary>
    private void InitializeServices()
    {
        _databaseService = new DatabaseService();
        _novelService = new NovelService(_databaseService);
        _imageService = new ImageService(_databaseService);
        _genreService = new GenreService(_databaseService);
    }

    /// <summary>
    /// Se ejecuta cuando la página aparece
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Cargar géneros populares la primera vez
        if (_popularGenres == null)
        {
            await LoadPopularGenres();
        }

        // Cargar novelas
        await LoadNovels();
    }

    /// <summary>
    /// Carga los géneros populares para los botones
    /// </summary>
    private async Task LoadPopularGenres()
    {
        try
        {
            // Obtener top 6 géneros más populares
            _popularGenres = await _genreService.GetPopularGenresAsync(6);

            // Actualizar los botones de categorías
            UpdateCategoryButtons();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando géneros: {ex.Message}");
        }
    }

    /// <summary>
    /// Actualiza los botones de categorías con géneros reales
    /// </summary>
    private void UpdateCategoryButtons()
    {
        Device.BeginInvokeOnMainThread(() =>
        {
            // Buscar el HorizontalStackLayout
            if (CategoryButtons != null)
            {
                // Mantener el botón "Todas"
                var allButton = CategoryButtons.Children.FirstOrDefault() as Button;

                // Limpiar otros botones
                while (CategoryButtons.Children.Count > 1)
                {
                    CategoryButtons.Children.RemoveAt(1);
                }

                // Agregar botones para géneros populares
                if (_popularGenres != null)
                {
                    foreach (var genre in _popularGenres)
                    {
                        var button = CreateCategoryButton($"{genre.Icon} {genre.Name}", false);
                        button.CommandParameter = genre.Id;
                        CategoryButtons.Children.Add(button);
                    }
                }

                // Botón para ver más géneros
                var moreButton = new Button
                {
                    Text = "Ver más →",
                    BackgroundColor = Color.FromArgb("#1E1E1E"),
                    TextColor = Color.FromArgb("#E91E63"),
                    CornerRadius = 15,
                    Padding = new Thickness(15, 5),
                    FontAttributes = FontAttributes.Bold
                };
                moreButton.Clicked += OnViewAllGenresClicked;
                CategoryButtons.Children.Add(moreButton);
            }
        });
    }

    /// <summary>
    /// Crea un botón de categoría
    /// </summary>
    private Button CreateCategoryButton(string text, bool isSelected)
    {
        var button = new Button
        {
            Text = text,
            BackgroundColor = isSelected ? Color.FromArgb("#E91E63") : Color.FromArgb("#1E1E1E"),
            TextColor = isSelected ? Colors.White : Color.FromArgb("#808080"),
            CornerRadius = 15,
            Padding = new Thickness(15, 5)
        };

        button.Clicked += OnCategoryClicked;
        return button;
    }

    /// <summary>
    /// Carga las novelas (todas o filtradas por género)
    /// </summary>
    private async Task LoadNovels(int? genreId = null)
    {
        try
        {
            // Mostrar indicador de carga
            ShowLoading(true);

            if (genreId.HasValue)
            {
                // Cargar novelas de un género específico
                _allNovels = await _genreService.GetTopNovelsByGenreAsync(genreId.Value, 50);
            }
            else
            {
                // Cargar todas las novelas
                _allNovels = await _novelService.GetAllNovelsAsync();
            }

            _displayedNovels = _allNovels;
            await DisplayNovels();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar novelas: {ex.Message}", "OK");
        }
        finally
        {
            ShowLoading(false);
        }
    }

    /// <summary>
    /// Muestra las novelas en la UI
    /// </summary>
    private async Task DisplayNovels()
    {
        var displayData = new List<NovelDisplayData>();

        foreach (var novel in _displayedNovels)
        {
            var coverImage = await _imageService.GetCoverImageAsync(novel.CoverImage);

            displayData.Add(new NovelDisplayData
            {
                Id = novel.Id,
                Title = novel.Title,
                Author = novel.Author,
                Rating = novel.Rating.ToString("F1"),
                Chapters = novel.ChapterCount.ToString(),
                CoverImageSource = coverImage,
                Status = GetStatusDisplay(novel.Status),
                StatusColor = GetStatusColor(novel.Status)
            });
        }

        Device.BeginInvokeOnMainThread(() =>
        {
            novelsCollection.ItemsSource = displayData;
        });
    }

    /// <summary>
    /// Maneja el clic en una categoría
    /// </summary>
    private async void OnCategoryClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            // Actualizar visual de los botones
            UpdateCategoryButtonsVisual(button);

            _currentGenreFilter = button.Text;

            if (button.Text.Contains("Todas"))
            {
                // Cargar todas las novelas
                await LoadNovels();
            }
            else if (button.CommandParameter is int genreId)
            {
                // Cargar novelas del género seleccionado
                await LoadNovels(genreId);
            }
        }
    }

    /// <summary>
    /// Actualiza el aspecto visual de los botones de categoría
    /// </summary>
    private void UpdateCategoryButtonsVisual(Button selectedButton)
    {
        if (CategoryButtons != null)
        {
            foreach (var child in CategoryButtons.Children)
            {
                if (child is Button btn && btn.Text != "Ver más →")
                {
                    bool isSelected = btn == selectedButton;
                    btn.BackgroundColor = isSelected ? Color.FromArgb("#E91E63") : Color.FromArgb("#1E1E1E");
                    btn.TextColor = isSelected ? Colors.White : Color.FromArgb("#808080");
                }
            }
        }
    }

    /// <summary>
    /// Maneja la búsqueda de novelas
    /// </summary>
    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(searchText))
        {
            _displayedNovels = _allNovels;
        }
        else
        {
            _displayedNovels = _allNovels.Where(n =>
                n.Title.ToLower().Contains(searchText) ||
                n.Author.ToLower().Contains(searchText)
            ).ToList();
        }

        await DisplayNovels();
    }

    /// <summary>
    /// Maneja el clic en el filtro
    /// </summary>
    private async void OnFilterClicked(object sender, EventArgs e)
    {
        var action = await DisplayActionSheet(
            "Filtrar por",
            "Cancelar",
            null,
            "Mejor calificación",
            "Más recientes",
            "Más capítulos",
            "Estado: En curso",
            "Estado: Completadas"
        );

        if (action != "Cancelar" && !string.IsNullOrEmpty(action))
        {
            ApplyFilter(action);
        }
    }

    /// <summary>
    /// Aplica el filtro seleccionado
    /// </summary>
    private async void ApplyFilter(string filter)
    {
        _displayedNovels = filter switch
        {
            "Mejor calificación" => _allNovels.OrderByDescending(n => n.Rating).ToList(),
            "Más recientes" => _allNovels.OrderByDescending(n => n.UpdatedAt).ToList(),
            "Más capítulos" => _allNovels.OrderByDescending(n => n.ChapterCount).ToList(),
            "Estado: En curso" => _allNovels.Where(n => n.Status == "ongoing").ToList(),
            "Estado: Completadas" => _allNovels.Where(n => n.Status == "completed").ToList(),
            _ => _allNovels
        };

        await DisplayNovels();
    }

    /// <summary>
    /// Navega a la página de todos los géneros
    /// </summary>
    private async void OnViewAllGenresClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new PopularGenresPage());
    }

    /// <summary>
    /// Maneja el tap en una novela
    /// </summary>
    private async void OnNovelTapped(object sender, EventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is NovelDisplayData novel)
        {
            await Navigation.PushAsync(new NovelDetailPage(novel.Id));
        }
    }

    /// <summary>
    /// Muestra/oculta el indicador de carga
    /// </summary>
    private void ShowLoading(bool show)
    {
        Device.BeginInvokeOnMainThread(() =>
        {
            if (LoadingIndicator != null)
            {
                LoadingIndicator.IsVisible = show;
                LoadingIndicator.IsRunning = show;
            }

            if (novelsCollection != null)
            {
                novelsCollection.IsVisible = !show;
            }
        });
    }

    /// <summary>
    /// Obtiene el texto del estado
    /// </summary>
    private string GetStatusDisplay(string status)
    {
        return status?.ToLower() switch
        {
            "ongoing" => "En curso",
            "completed" => "Completada",
            "hiatus" => "Pausada",
            "cancelled" => "Cancelada",
            _ => status ?? ""
        };
    }

    /// <summary>
    /// Obtiene el color del estado
    /// </summary>
    private Color GetStatusColor(string status)
    {
        return status?.ToLower() switch
        {
            "ongoing" => Color.FromArgb("#4CAF50"),
            "completed" => Color.FromArgb("#2196F3"),
            "hiatus" => Color.FromArgb("#FF9800"),
            "cancelled" => Color.FromArgb("#F44336"),
            _ => Color.FromArgb("#808080")
        };
    }
}