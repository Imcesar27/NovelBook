using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class StatsPage : ContentPage
{
    // Servicios
    private readonly StatsService _statsService;
    private readonly DatabaseService _databaseService;

    // Datos
    private ExtendedStats _stats;

    public StatsPage()
    {
        InitializeComponent();

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _statsService = new StatsService(_databaseService);
    }

    /// <summary>
    /// Se ejecuta cuando la página aparece
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Verificar usuario logueado
        if (AuthService.CurrentUser == null)
        {
            await DisplayAlert("Aviso", "Debes iniciar sesión para ver tus estadísticas", "OK");
            await Navigation.PopAsync();
            return;
        }

        await LoadStatistics();
    }

    /// <summary>
    /// Carga todas las estadísticas
    /// </summary>
    private async Task LoadStatistics()
    {
        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            // Obtener estadísticas
            _stats = await _statsService.GetExtendedStatsAsync();

            // Actualizar UI
            UpdateBasicStats();
            UpdateLibraryStats();
            UpdateWeeklyChart();
            UpdateGenreStats();
            UpdateAuthorStats();
            UpdateAchievements();
            UpdateHourlyChart();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error al cargar estadísticas: " + ex.Message, "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    /// <summary>
    /// Actualiza las estadísticas básicas
    /// </summary>
    private void UpdateBasicStats()
    {
        // Header principal
        TotalChaptersLabel.Text = _stats.TotalChaptersRead.ToString();
        TotalNovelsLabel.Text = _stats.TotalNovelsRead.ToString();
        TotalTimeLabel.Text = _stats.FormattedTotalTime;
        CurrentStreakLabel.Text = _stats.CurrentStreak.ToString();

        // Promedios
        AvgChaptersLabel.Text = $"📖 {_stats.AverageChaptersPerDay:F1} cap/día";
        AvgTimeLabel.Text = $"⏱️ {(int)(_stats.AverageReadingTimePerDay / 60)} min/día";

        // Fecha de registro
        if (_stats.FirstReadingDate != default)
        {
            MemberSinceLabel.Text = $"Leyendo desde: {_stats.FirstReadingDate:dd/MM/yyyy}";
        }
    }

    /// <summary>
    /// Actualiza las estadísticas de biblioteca
    /// </summary>
    private void UpdateLibraryStats()
    {
        // Contadores
        ReadingCountLabel.Text = $"Leyendo: {_stats.NovelsReading}";
        CompletedCountLabel.Text = $"Completados: {_stats.NovelsCompleted}";
        PlanToReadCountLabel.Text = $"Por leer: {_stats.NovelsPlanToRead}";
        FavoritesCountLabel.Text = $"Favoritos: {_stats.TotalFavorites}";

        // Tasa de completación
        var completionRate = _stats.CompletionRate / 100;
        CompletionProgressBar.Progress = completionRate;
        CompletionRateLabel.Text = $"{_stats.CompletionRate:F1}% completado";

        // Crear gráfico circular simple
        CreatePieChart();
    }

    /// <summary>
    /// Crea un gráfico circular simple para el estado de la biblioteca
    /// </summary>
    private void CreatePieChart()
    {
        PieChartContainer.Children.Clear();

        // Crear un Grid para simular un gráfico circular con BoxViews
        var total = _stats.TotalNovelsInLibrary;
        if (total == 0) return;

        var centerX = 75;
        var centerY = 75;
        var radius = 60;

        // Datos para el gráfico
        var data = new[]
        {
            (_stats.NovelsReading, Color.FromArgb("#4CAF50")),
            (_stats.NovelsCompleted, Color.FromArgb("#2196F3")),
            (_stats.NovelsPlanToRead, Color.FromArgb("#FF9800")),
            (total - _stats.NovelsReading - _stats.NovelsCompleted - _stats.NovelsPlanToRead, Color.FromArgb("#757575"))
        };

        // Círculo de fondo
        var backgroundCircle = new Ellipse
        {
            Fill = new SolidColorBrush(Color.FromArgb("#2D2D2D")),
            WidthRequest = radius * 2,
            HeightRequest = radius * 2,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        PieChartContainer.Children.Add(backgroundCircle);

        // Texto central
        var centerLabel = new Label
        {
            Text = total.ToString(),
            FontSize = 24,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        PieChartContainer.Children.Add(centerLabel);
    }

    /// <summary>
    /// Actualiza el gráfico semanal
    /// </summary>
    private void UpdateWeeklyChart()
    {
        WeeklyChartContainer.Children.Clear();

        // Preparar datos para los últimos 7 días
        var today = DateTime.Today;
        var weekData = new List<(DateTime date, int chapters)>();

        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var dayStats = _stats.Last7DaysStats.FirstOrDefault(d => d.Date.Date == date);
            weekData.Add((date, dayStats?.ChaptersRead ?? 0));
        }

        // Encontrar el valor máximo para escalar
        var maxChapters = weekData.Max(d => d.chapters);
        if (maxChapters == 0) maxChapters = 1;

        // Crear barras
        for (int i = 0; i < weekData.Count; i++)
        {
            var (date, chapters) = weekData[i];
            var height = (double)chapters / maxChapters;

            // Barra
            var bar = new Frame
            {
                BackgroundColor = date == today ?
                    Color.FromArgb("#8B5CF6") :
                    Color.FromArgb("#4A4A4A"),
                CornerRadius = 5,
                VerticalOptions = LayoutOptions.End,
                HeightRequest = Math.Max(5, height * 120),
                Margin = new Thickness(2, 0)
            };

            Grid.SetRow(bar, 0);
            Grid.SetColumn(bar, i);
            WeeklyChartContainer.Children.Add(bar);

            // Etiqueta del día
            var dayLabel = new Label
            {
                Text = date.ToString("ddd")[0].ToString(),
                FontSize = 12,
                TextColor = Color.FromArgb("#B0B0B0"),
                HorizontalTextAlignment = TextAlignment.Center
            };

            Grid.SetRow(dayLabel, 1);
            Grid.SetColumn(dayLabel, i);
            WeeklyChartContainer.Children.Add(dayLabel);

            // Número de capítulos (si hay)
            if (chapters > 0)
            {
                var countLabel = new Label
                {
                    Text = chapters.ToString(),
                    FontSize = 10,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.End,
                    Margin = new Thickness(0, 0, 0, 5)
                };

                Grid.SetRow(countLabel, 0);
                Grid.SetColumn(countLabel, i);
                WeeklyChartContainer.Children.Add(countLabel);
            }
        }

        // Total semanal
        var weeklyTotal = weekData.Sum(d => d.chapters);
        WeeklyTotalLabel.Text = $"Total esta semana: {weeklyTotal} capítulos";
    }

    /// <summary>
    /// Actualiza las estadísticas de géneros
    /// </summary>
    private void UpdateGenreStats()
    {
        GenreStatsContainer.Children.Clear();

        if (!_stats.GenreStats.Any())
        {
            var noDataLabel = new Label
            {
                Text = "No hay datos de géneros aún",
                TextColor = Color.FromArgb("#B0B0B0"),
                FontSize = 14
            };
            GenreStatsContainer.Children.Add(noDataLabel);
            return;
        }

        foreach (var genre in _stats.GenreStats.Take(5))
        {
            var genreFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#3D3D3D"),
                CornerRadius = 10,
                Padding = 10,
                HasShadow = false
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            // Información del género
            var infoStack = new StackLayout { Spacing = 3 };

            var nameLabel = new Label
            {
                Text = genre.GenreName,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            };
            infoStack.Children.Add(nameLabel);

            var statsLabel = new Label
            {
                Text = $"{genre.ChaptersRead} capítulos • {genre.NovelsRead} novelas",
                FontSize = 12,
                TextColor = Color.FromArgb("#B0B0B0")
            };
            infoStack.Children.Add(statsLabel);

            // Barra de progreso
            var progressBar = new ProgressBar
            {
                Progress = genre.Percentage / 100,
                ProgressColor = Color.FromArgb("#8B5CF6"),
                HeightRequest = 4
            };
            infoStack.Children.Add(progressBar);

            grid.Children.Add(infoStack);
            Grid.SetColumn(infoStack, 0);

            // Porcentaje
            var percentLabel = new Label
            {
                Text = $"{genre.Percentage:F1}%",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#8B5CF6"),
                VerticalOptions = LayoutOptions.Center
            };
            grid.Children.Add(percentLabel);
            Grid.SetColumn(percentLabel, 1);

            genreFrame.Content = grid;
            GenreStatsContainer.Children.Add(genreFrame);
        }
    }

    /// <summary>
    /// Actualiza las estadísticas de autores
    /// </summary>
    private void UpdateAuthorStats()
    {
        AuthorStatsContainer.Children.Clear();

        if (!_stats.AuthorStats.Any())
        {
            var noDataLabel = new Label
            {
                Text = "No hay datos de autores aún",
                TextColor = Color.FromArgb("#B0B0B0"),
                FontSize = 14
            };
            AuthorStatsContainer.Children.Add(noDataLabel);
            return;
        }

        foreach (var author in _stats.AuthorStats.Take(3))
        {
            var authorFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#3D3D3D"),
                CornerRadius = 10,
                Padding = 10,
                HasShadow = false
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            // Información del autor
            var infoStack = new StackLayout { Spacing = 3 };

            var nameLabel = new Label
            {
                Text = author.AuthorName,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            };
            infoStack.Children.Add(nameLabel);

            var statsLabel = new Label
            {
                Text = $"{author.ChaptersRead} capítulos • {author.NovelsRead} novelas",
                FontSize = 12,
                TextColor = Color.FromArgb("#B0B0B0")
            };
            infoStack.Children.Add(statsLabel);

            grid.Children.Add(infoStack);
            Grid.SetColumn(infoStack, 0);

            // Rating promedio
            if (author.AverageRating > 0)
            {
                var ratingLabel = new Label
                {
                    Text = $"⭐ {author.AverageRating:F1}",
                    FontSize = 16,
                    TextColor = Color.FromArgb("#FFD700"),
                    VerticalOptions = LayoutOptions.Center
                };
                grid.Children.Add(ratingLabel);
                Grid.SetColumn(ratingLabel, 1);
            }

            authorFrame.Content = grid;
            AuthorStatsContainer.Children.Add(authorFrame);
        }
    }

    /// <summary>
    /// Actualiza los logros
    /// </summary>
    private void UpdateAchievements()
    {
        AchievementsContainer.Children.Clear();

        // Mostrar solo los primeros 5 logros desbloqueados
        foreach (var achievement in _stats.UnlockedAchievements.Take(5))
        {
            var achievementFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#3D3D3D"),
                CornerRadius = 10,
                Padding = 15,
                WidthRequest = 80,
                HeightRequest = 80,
                HasShadow = false
            };

            var stack = new StackLayout
            {
                Spacing = 5,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            var iconLabel = new Label
            {
                Text = achievement.Icon,
                FontSize = 28,
                HorizontalTextAlignment = TextAlignment.Center
            };
            stack.Children.Add(iconLabel);

            var nameLabel = new Label
            {
                Text = achievement.Name,
                FontSize = 10,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                MaxLines = 2,
                LineBreakMode = LineBreakMode.TailTruncation
            };
            stack.Children.Add(nameLabel);

            achievementFrame.Content = stack;
            AchievementsContainer.Children.Add(achievementFrame);
        }

        // Actualizar contador
        AchievementCountLabel.Text = $"{_stats.UnlockedAchievements.Count}/10 logros desbloqueados";
    }

    /// <summary>
    /// Actualiza el gráfico de horas
    /// </summary>
    private void UpdateHourlyChart()
    {
        HourlyChartContainer.Children.Clear();

        if (!_stats.HourlyDistribution.Any())
        {
            var noDataLabel = new Label
            {
                Text = "No hay suficientes datos aún",
                TextColor = Color.FromArgb("#B0B0B0"),
                FontSize = 14,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            HourlyChartContainer.Children.Add(noDataLabel);
            return;
        }

        // Crear un grid simple para mostrar las horas más activas
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection()
        };

        // Agregar 24 columnas (una por hora)
        for (int i = 0; i < 24; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        }

        var maxValue = _stats.HourlyDistribution.Values.Max();
        if (maxValue == 0) maxValue = 1;

        // Crear barras para cada hora
        for (int hour = 0; hour < 24; hour++)
        {
            var value = _stats.HourlyDistribution.ContainsKey(hour) ?
                        _stats.HourlyDistribution[hour] : 0;
            var height = (double)value / maxValue;

            var bar = new BoxView
            {
                Color = GetHourColor(hour, value > 0),
                VerticalOptions = LayoutOptions.End,
                HeightRequest = Math.Max(2, height * 60),
                Margin = new Thickness(1, 0)
            };

            grid.Children.Add(bar);
            Grid.SetColumn(bar, hour);

            // Etiquetas para horas importantes
            if (hour % 6 == 0)
            {
                var label = new Label
                {
                    Text = $"{hour}h",
                    FontSize = 10,
                    TextColor = Color.FromArgb("#808080"),
                    VerticalOptions = LayoutOptions.End,
                    HorizontalTextAlignment = TextAlignment.Center,
                    TranslationY = 15
                };
                grid.Children.Add(label);
                Grid.SetColumn(label, hour);
            }
        }

        HourlyChartContainer.Children.Add(grid);
    }

    /// <summary>
    /// Obtiene el color para una hora específica
    /// </summary>
    private Color GetHourColor(int hour, bool hasActivity)
    {
        if (!hasActivity) return Color.FromArgb("#2D2D2D");

        // Colores según la hora del día
        if (hour >= 6 && hour < 12) return Color.FromArgb("#FFC107"); // Mañana
        if (hour >= 12 && hour < 18) return Color.FromArgb("#4CAF50"); // Tarde
        if (hour >= 18 && hour < 22) return Color.FromArgb("#2196F3"); // Noche
        return Color.FromArgb("#9C27B0"); // Madrugada
    }

    /// <summary>
    /// Muestra todos los logros
    /// </summary>
    private async void OnViewAllAchievements(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AchievementsPage(_stats));
    }
}