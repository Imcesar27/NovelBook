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

    // Nuevas propiedades para mantener el estado del filtro
    private int? _currentGenreId = null;
    private Button _currentSelectedButton = null;

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
        InitializeUI();
    }

    private void InitializeUI()
    {
        // Inicializar el texto del botón "Todas"
        AllButton.Text = $"🔥 {LocalizationService.GetString("AllGenres")}";
        _currentGenreFilter = AllButton.Text;
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

        // Actualizar texto del botón "Todas" por si cambió el idioma
        if (AllButton != null)
        {
            AllButton.Text = $"🔥 {LocalizationService.GetString("AllGenres")}";
        }

        // Cargar géneros populares la primera vez
        if (_popularGenres == null)
        {
            await LoadPopularGenres();
        }

        // Cargar novelas manteniendo el filtro actual
        await LoadNovels(_currentGenreId);

        // Restaurar el estado visual del botón seleccionado
        if (_currentSelectedButton != null)
        {
            UpdateCategoryButtonsVisual(_currentSelectedButton);
        }
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

                // Si no hay botón seleccionado, seleccionar "Todas" por defecto
                if (_currentSelectedButton == null && allButton != null)
                {
                    _currentSelectedButton = allButton;
                }

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
                    Text = $"{LocalizationService.GetString("ViewMore")} →",
                    BackgroundColor = Color.FromArgb("#1E1E1E"),
                    TextColor = Color.FromArgb("#E91E63"),
                    CornerRadius = 15,
                    Padding = new Thickness(15, 5),
                    FontAttributes = FontAttributes.Bold
                };
                moreButton.Clicked += OnViewAllGenresClicked;
                CategoryButtons.Children.Add(moreButton);

                // Restaurar la selección visual si existe
                if (_currentSelectedButton != null)
                {
                    UpdateCategoryButtonsVisual(_currentSelectedButton);
                }
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
            await DisplayAlert(
                LocalizationService.GetString("Error"),
                $"{LocalizationService.GetString("ErrorLoadingNovels")}: {ex.Message}",
                LocalizationService.GetString("OK"));
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
            // Guardar el botón seleccionado
            _currentSelectedButton = button;

            // Actualizar visual de los botones
            UpdateCategoryButtonsVisual(button);

            _currentGenreFilter = button.Text;

            if (button.Text.Contains(LocalizationService.GetString("AllGenres")))
            {
                // Cargar todas las novelas
                _currentGenreId = null;
                await LoadNovels();
            }
            else if (button.CommandParameter is int genreId)
            {
                // Cargar novelas del género seleccionado
                _currentGenreId = genreId;
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
                    btn.BackgroundColor = isSelected ?
                        Color.FromArgb("#E91E63") : Color.FromArgb("#1E1E1E");
                    btn.TextColor = isSelected ? Colors.White : Color.FromArgb("#808080");
                }
            }
        }
    }

    /// <summary>
    /// Muestra u oculta el indicador de carga
    /// </summary>
    private void ShowLoading(bool show)
    {
        Device.BeginInvokeOnMainThread(() =>
        {
            if (LoadingIndicator != null)
            {
                LoadingIndicator.IsRunning = show;
                LoadingIndicator.IsVisible = show;
            }

            if (novelsCollection != null)
            {
                novelsCollection.IsVisible = !show;
            }
        });
    }

    /// <summary>
    /// Obtiene el texto de estado según el valor
    /// </summary>
    private string GetStatusDisplay(string status)
    {
        return status?.ToLower() switch
        {
            "ongoing" => LocalizationService.GetString("Ongoing"),
            "completed" => LocalizationService.GetString("Completed"),
            "hiatus" => LocalizationService.GetString("Hiatus"),
            "cancelled" => LocalizationService.GetString("Cancelled"),
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
            "ongoing" => Color.FromArgb("#F59E0B"),      // Naranja
            "completed" => Color.FromArgb("#10B981"),    // Verde
            "hiatus" => Color.FromArgb("#6B7280"),       // Gris
            "cancelled" => Color.FromArgb("#EF4444"),    // Rojo para cancelada
            _ => Color.FromArgb("#6B7280")
        };
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
        LocalizationService.GetString("FilterBy"),
        LocalizationService.GetString("Cancel"),
        null,
        LocalizationService.GetString("BestRating"),
        LocalizationService.GetString("MostRecent"),
        LocalizationService.GetString("MostChapters"),
        LocalizationService.GetString("StatusOngoing"),
        LocalizationService.GetString("StatusCompleted")
        );

        if (action != LocalizationService.GetString("Cancel") && !string.IsNullOrEmpty(action))
        {
            ApplyFilter(action);
        }
    }

    /// <summary>
    /// Aplica el filtro seleccionado
    /// </summary>
    private async void ApplyFilter(string filter)
    {
        var bestRating = LocalizationService.GetString("BestRating");
        var mostRecent = LocalizationService.GetString("MostRecent");
        var mostChapters = LocalizationService.GetString("MostChapters");
        var statusOngoing = LocalizationService.GetString("StatusOngoing");
        var statusCompleted = LocalizationService.GetString("StatusCompleted");

        _displayedNovels = filter switch
        {
            var f when f == bestRating => _allNovels.OrderByDescending(n => n.Rating).ToList(),
            var f when f == mostRecent => _allNovels.OrderByDescending(n => n.UpdatedAt).ToList(),
            var f when f == mostChapters => _allNovels.OrderByDescending(n => n.ChapterCount).ToList(),
            var f when f == statusOngoing => _allNovels.Where(n => n.Status == "ongoing").ToList(),
            var f when f == statusCompleted => _allNovels.Where(n => n.Status == "completed").ToList(),
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
        if (sender is Frame frame && frame.BindingContext is NovelDisplayData novelData)
        {
            await Navigation.PushAsync(new NovelDetailPage(novelData.Id));
        }
    }

    /// <summary>
    /// Maneja el tap en el nombre del autor
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

}