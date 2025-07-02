using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class AuthorNovelsPage : ContentPage
{
    // Servicios
    private readonly NovelService _novelService;
    private readonly DatabaseService _databaseService;
    private readonly ImageService _imageService;

    // Datos
    private readonly string _authorName;
    private List<Novel> _novels;
    private List<Novel> _displayedNovels;
    private bool _isGridView = false;

    public AuthorNovelsPage(string authorName)
    {
        InitializeComponent();

        _authorName = authorName;
        Title = $"Novelas de {authorName}";

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _novelService = new NovelService(_databaseService);
        _imageService = new ImageService(_databaseService);

        // Configurar UI
        AuthorNameLabel.Text = _authorName;
        SortPicker.SelectedIndex = 0;
        SortPicker.SelectedIndexChanged += OnSortChanged;

        // Cargar datos
        LoadAuthorNovels();
    }

    /// <summary>
    /// Carga todas las novelas del autor
    /// </summary>
    private async void LoadAuthorNovels()
    {
        try
        {
            // Obtener todas las novelas del autor
            _novels = await _novelService.GetNovelsByAuthorAsync(_authorName);

            // Ocultar indicador de carga
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;

            if (_novels.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            // Actualizar estadísticas
            UpdateAuthorStatistics();

            // Aplicar ordenamiento inicial
            ApplySort();
        }
        catch (Exception ex)
        {
            LoadingIndicator.IsVisible = false;
            await DisplayAlert("Error", "Error al cargar novelas: " + ex.Message, "OK");
        }
    }

    /// <summary>
    /// Actualiza las estadísticas del autor
    /// </summary>
    private void UpdateAuthorStatistics()
    {
        if (_novels == null || _novels.Count == 0) return;

        // Actualizar contadores
        NovelCountLabel.Text = $"{_novels.Count} novela{(_novels.Count != 1 ? "s" : "")}";

        // Total de capítulos
        var totalChapters = _novels.Sum(n => n.ChapterCount);
        TotalChaptersLabel.Text = totalChapters.ToString();

        // Rating promedio
        var averageRating = _novels.Average(n => (double)n.Rating);
        AverageRatingLabel.Text = averageRating.ToString("F1");

        // Novelas completadas
        var completedCount = _novels.Count(n => n.Status?.ToLower() == "completed");
        CompletedNovelsLabel.Text = completedCount.ToString();
    }

    /// <summary>
    /// Aplica el ordenamiento seleccionado
    /// </summary>
    private void ApplySort()
    {
        if (_novels == null || _novels.Count == 0) return;

        var selectedSort = SortPicker.SelectedItem?.ToString() ?? "";

        _displayedNovels = selectedSort switch
        {
            "📚 Más recientes" => _novels.OrderByDescending(n => n.UpdatedAt).ToList(),
            "⭐ Mejor calificación" => _novels.OrderByDescending(n => n.Rating).ToList(),
            "📖 Más capítulos" => _novels.OrderByDescending(n => n.ChapterCount).ToList(),
            "✅ Completadas primero" => _novels.OrderByDescending(n => n.Status == "completed")
                                               .ThenByDescending(n => n.Rating).ToList(),
            "🔤 Alfabético" => _novels.OrderBy(n => n.Title).ToList(),
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

        if (_isGridView)
        {
            // Vista de cuadrícula
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                RowSpacing = 10,
                ColumnSpacing = 10
            };

            int row = 0;
            int col = 0;

            foreach (var novel in _displayedNovels)
            {
                var novelCard = await CreateGridNovelCard(novel);

                grid.Children.Add(novelCard);
                Grid.SetRow(novelCard, row);
                Grid.SetColumn(novelCard, col);

                col++;
                if (col > 1)
                {
                    col = 0;
                    row++;
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }
            }

            NovelsContainer.Children.Add(grid);
        }
        else
        {
            // Vista de lista
            foreach (var novel in _displayedNovels)
            {
                var novelCard = await CreateListNovelCard(novel);
                NovelsContainer.Children.Add(novelCard);
            }
        }
    }

    /// <summary>
    /// Crea una tarjeta de novela para vista de lista
    /// </summary>
    private async Task<Frame> CreateListNovelCard(Novel novel)
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
    /// Crea una tarjeta de novela para vista de cuadrícula
    /// </summary>
    private async Task<Frame> CreateGridNovelCard(Novel novel)
    {
        var frame = new Frame
        {
            BackgroundColor = Color.FromArgb("#1E1E1E"),
            CornerRadius = 10,
            Padding = 0,
            HasShadow = false,
            HeightRequest = 280
        };

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = 180 },
                new RowDefinition { Height = GridLength.Star }
            }
        };

        // Imagen de portada
        var coverImage = new Image
        {
            Source = await _imageService.GetCoverImageAsync(novel.CoverImage),
            Aspect = Aspect.AspectFill
        };
        grid.Children.Add(coverImage);
        Grid.SetRow(coverImage, 0);

        // Información
        var infoStack = new StackLayout
        {
            Padding = 10,
            Spacing = 5
        };

        var titleLabel = new Label
        {
            Text = novel.Title,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 14,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 2
        };
        infoStack.Children.Add(titleLabel);

        var statsLabel = new Label
        {
            Text = $"⭐ {novel.Rating:F1} • 📖 {novel.ChapterCount}",
            TextColor = Color.FromArgb("#808080"),
            FontSize = 12
        };
        infoStack.Children.Add(statsLabel);

        grid.Children.Add(infoStack);
        Grid.SetRow(infoStack, 1);

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
    /// Maneja el cambio de ordenamiento
    /// </summary>
    private void OnSortChanged(object sender, EventArgs e)
    {
        ApplySort();
    }

    /// <summary>
    /// Alterna entre vista de lista y cuadrícula
    /// </summary>
    private void OnViewModeToggled(object sender, EventArgs e)
    {
        _isGridView = !_isGridView;
        ViewModeLabel.Text = _isGridView ? "Vista: Cuadrícula" : "Vista: Lista";
        DisplayNovels();
    }

    /// <summary>
    /// Muestra el estado vacío
    /// </summary>
    private void ShowEmptyState()
    {
        NovelsContainer.IsVisible = false;
        EmptyStateLayout.IsVisible = true;
    }

    /// <summary>
    /// Obtiene el color según el estado
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
}