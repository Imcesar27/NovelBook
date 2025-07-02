using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class GenreDetailPage : ContentPage
{
    // Servicios
    private readonly GenreService _genreService;
    private readonly NovelService _novelService;
    private readonly DatabaseService _databaseService;
    private readonly ImageService _imageService;

    // Datos
    private readonly int _genreId;
    private List<Novel> _novels;
    private List<Novel> _displayedNovels;
    private GenreStats _genreStats;
    private string _currentSort = "⭐ Mejor calificación";

    public GenreDetailPage(int genreId, string genreName)
    {
        InitializeComponent();

        _genreId = genreId;
        Title = genreName;

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _genreService = new GenreService(_databaseService);
        _novelService = new NovelService(_databaseService);
        _imageService = new ImageService(_databaseService);

        // Configurar UI
        GenreTitleLabel.Text = genreName;
        SortPicker.SelectedIndex = 0;

        // Cargar datos
        LoadGenreData();
    }

    /// <summary>
    /// Carga los datos del género
    /// </summary>
    private async void LoadGenreData()
    {
        try
        {
            // Cargar estadísticas
            _genreStats = await _genreService.GetGenreStatsAsync(_genreId);
            UpdateStatistics();

            // Cargar novelas
            await LoadNovels();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error al cargar datos: " + ex.Message, "OK");
        }
    }

    /// <summary>
    /// Actualiza las estadísticas en el header
    /// </summary>
    private void UpdateStatistics()
    {
        if (_genreStats != null)
        {
            GenreTitleLabel.Text = _genreStats.GenreName;
            GenreDescriptionLabel.Text = string.IsNullOrEmpty(_genreStats.Description) ?
                "Explora las mejores novelas de este género" : _genreStats.Description;

            TotalNovelsLabel.Text = _genreStats.TotalNovels.ToString();
            AvgRatingLabel.Text = _genreStats.AverageRating.ToString("F1");
            ReadersLabel.Text = FormatNumber(_genreStats.UniqueReaders);
            ReviewsLabel.Text = FormatNumber(_genreStats.TotalReviews);

            // Cambiar color del header según el género
            if (_genreStats.GenreName != null)
            {
                var popularGenre = new PopularGenre { Name = _genreStats.GenreName };
                HeaderFrame.BackgroundColor = Color.FromArgb(popularGenre.ThemeColor).WithAlpha(0.3f);
            }
        }
    }

    /// <summary>
    /// Carga las novelas del género
    /// </summary>
    private async Task LoadNovels()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        NovelsContainer.Children.Clear();

        try
        {
            // Obtener las novelas más populares del género
            _novels = await _genreService.GetTopNovelsByGenreAsync(_genreId, 50);

            if (_novels.Count == 0)
            {
                ShowNoNovelsMessage();
                return;
            }

            // Aplicar ordenamiento inicial
            ApplySort();
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    /// <summary>
    /// Aplica el ordenamiento seleccionado
    /// </summary>
    private void ApplySort()
    {
        if (_novels == null || _novels.Count == 0) return;

        _displayedNovels = _currentSort switch
        {
            "⭐ Mejor calificación" => _novels.OrderByDescending(n => n.Rating).ToList(),
            "📚 Más populares" => _novels.ToList(), // Ya vienen ordenadas por popularidad
            "🆕 Más recientes" => _novels.OrderByDescending(n => n.UpdatedAt).ToList(),
            "📖 Más capítulos" => _novels.OrderByDescending(n => n.ChapterCount).ToList(),
            "✅ Completadas" => _novels.Where(n => n.Status == "completed")
                                      .OrderByDescending(n => n.Rating).ToList(),
            _ => _novels
        };

        DisplayNovels();
    }

    /// <summary>
    /// Muestra las novelas en la UI
    /// </summary>
    private async void DisplayNovels()
    {
        NovelsContainer.Children.Clear();

        if (_displayedNovels == null || _displayedNovels.Count == 0)
        {
            ShowNoNovelsMessage();
            return;
        }

        foreach (var novel in _displayedNovels)
        {
            var novelCard = await CreateNovelCard(novel);
            NovelsContainer.Children.Add(novelCard);
        }
    }

    /// <summary>
    /// Crea una tarjeta de novela
    /// </summary>
    private async Task<Frame> CreateNovelCard(Novel novel)
    {
        var frame = new Frame
        {
            BackgroundColor = Color.FromArgb("#1E1E1E"),
            CornerRadius = 10,
            Padding = 10,
            HasShadow = false
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = 80 },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10
        };

        // Portada
        var coverImage = new Image
        {
            Source = await _imageService.GetCoverImageAsync(novel.CoverImage),
            WidthRequest = 80,
            HeightRequest = 110,
            Aspect = Aspect.AspectFill
        };

        var coverFrame = new Frame
        {
            CornerRadius = 5,
            Padding = 0,
            IsClippedToBounds = true,
            HasShadow = false,
            Content = coverImage
        };
        grid.Children.Add(coverFrame);
        Grid.SetColumn(coverFrame, 0);

        // Información
        var infoStack = new StackLayout { Spacing = 5 };

        var titleLabel = new Label
        {
            Text = novel.Title,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 16,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 2
        };
        infoStack.Children.Add(titleLabel);

        var authorLabel = new Label
        {
            Text = novel.Author,
            TextColor = Color.FromArgb("#CCCCCC"),
            FontSize = 14,
            LineBreakMode = LineBreakMode.TailTruncation
        };
        infoStack.Children.Add(authorLabel);

        // Estadísticas
        var statsStack = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            Spacing = 15
        };

        var ratingLabel = new Label
        {
            Text = $"⭐ {novel.Rating:F1}",
            TextColor = Color.FromArgb("#FFD700"),
            FontSize = 12
        };
        statsStack.Children.Add(ratingLabel);

        var chaptersLabel = new Label
        {
            Text = $"📖 {novel.ChapterCount} cap",
            TextColor = Color.FromArgb("#4CAF50"),
            FontSize = 12
        };
        statsStack.Children.Add(chaptersLabel);

        var statusFrame = new Frame
        {
            BackgroundColor = GetStatusColor(novel.Status),
            CornerRadius = 10,
            Padding = new Thickness(8, 4),
            HasShadow = false
        };

        var statusLabel = new Label
        {
            Text = GetStatusText(novel.Status),
            TextColor = Colors.White,
            FontSize = 10,
            FontAttributes = FontAttributes.Bold
        };
        statusFrame.Content = statusLabel;
        statsStack.Children.Add(statusFrame);

        infoStack.Children.Add(statsStack);

        // Sinopsis corta
        if (!string.IsNullOrEmpty(novel.Synopsis))
        {
            var synopsisLabel = new Label
            {
                Text = novel.Synopsis,
                TextColor = Color.FromArgb("#808080"),
                FontSize = 12,
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 2,
                Margin = new Thickness(0, 5, 0, 0)
            };
            infoStack.Children.Add(synopsisLabel);
        }

        grid.Children.Add(infoStack);
        Grid.SetColumn(infoStack, 1);

        // Flecha
        var arrowLabel = new Label
        {
            Text = "›",
            TextColor = Color.FromArgb("#808080"),
            FontSize = 24,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End
        };
        grid.Children.Add(arrowLabel);
        Grid.SetColumn(arrowLabel, 2);

        frame.Content = grid;

        // Agregar tap para navegar
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) =>
        {
            await Navigation.PushAsync(new NovelDetailPage(novel.Id));
        };
        frame.GestureRecognizers.Add(tapGesture);

        return frame;
    }

    /// <summary>
    /// Obtiene el color según el estado
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

    /// <summary>
    /// Obtiene el texto del estado
    /// </summary>
    private string GetStatusText(string status)
    {
        return status?.ToLower() switch
        {
            "ongoing" => "En curso",
            "completed" => "Completada",
            "hiatus" => "Pausada",
            "cancelled" => "Cancelada",
            _ => status
        };
    }

    /// <summary>
    /// Formatea números grandes
    /// </summary>
    private string FormatNumber(int number)
    {
        if (number >= 1000000)
            return $"{number / 1000000.0:F1}M";
        else if (number >= 1000)
            return $"{number / 1000.0:F1}K";
        else
            return number.ToString();
    }

    /// <summary>
    /// Muestra mensaje cuando no hay novelas
    /// </summary>
    private void ShowNoNovelsMessage()
    {
        var label = new Label
        {
            Text = _currentSort == "✅ Completadas" ?
                "No hay novelas completadas en este género" :
                "No hay novelas disponibles en este género",
            TextColor = Color.FromArgb("#808080"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            FontSize = 16
        };
        NovelsContainer.Children.Add(label);
    }

    /// <summary>
    /// Maneja el cambio de ordenamiento
    /// </summary>
    private void OnSortChanged(object sender, EventArgs e)
    {
        if (SortPicker.SelectedItem != null)
        {
            _currentSort = SortPicker.SelectedItem.ToString();
            ApplySort();
        }
    }

    /// <summary>
    /// Actualiza los datos
    /// </summary>
    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        RefreshButton.IsEnabled = false;
        RefreshButton.Text = "⏳";

        await LoadNovels();

        RefreshButton.Text = "🔄";
        RefreshButton.IsEnabled = true;
    }
}