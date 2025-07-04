using NovelBook.Models;
using NovelBook.Services;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;

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
        // Contadores - Nota: Los favoritos pueden estar en cualquier estado
        ReadingCountLabel.Text = $"Leyendo: {_stats.NovelsReading}";
        CompletedCountLabel.Text = $"Completados: {_stats.NovelsCompleted}";
        PlanToReadCountLabel.Text = $"Por leer: {_stats.NovelsPlanToRead}";
        FavoritesCountLabel.Text = $"⭐ Favoritos: {_stats.TotalFavorites}";

        // Tasa de completación
        var completionRate = _stats.CompletionRate / 100;
        CompletionProgressBar.Progress = completionRate;
        CompletionRateLabel.Text = $"{_stats.CompletionRate:F1}% completado";

        // Crear gráfico circular
        CreatePieChart();
    }

    /// <summary>
    /// Crea un gráfico circular real para el estado de la biblioteca
    /// </summary>
    private void CreatePieChart()
    {
        PieChartContainer.Children.Clear();

        var total = _stats.TotalNovelsInLibrary;
        if (total == 0)
        {
            var emptyLabel = new Label
            {
                Text = "Sin novelas en tu biblioteca",
                FontSize = 14,
                TextColor = Color.FromArgb("#808080"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            PieChartContainer.Children.Add(emptyLabel);
            return;
        }

        // Crear el GraphicsView para dibujar el gráfico circular
        var pieChart = new GraphicsView
        {
            Drawable = new PieChartDrawable(_stats),
            HeightRequest = 150,
            WidthRequest = 150,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        PieChartContainer.Children.Add(pieChart);
    }

    /// <summary>
    /// Clase interna para dibujar el gráfico circular
    /// </summary>
    private class PieChartDrawable : IDrawable
    {
        private readonly ExtendedStats _stats;
        private readonly List<(string label, int count, Color color)> _segments;

        public PieChartDrawable(ExtendedStats stats)
        {
            _stats = stats;
            _segments = new List<(string label, int count, Color color)>
            {
                ("Leyendo", stats.NovelsReading, Color.FromArgb("#4CAF50")),
                ("Completados", stats.NovelsCompleted, Color.FromArgb("#2196F3")),
                ("Por leer", stats.NovelsPlanToRead, Color.FromArgb("#FF9800"))
            };
        }

        //método manual, calcula cada punto del arco explícitamente

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // Centro y radio del círculo
            var centerX = dirtyRect.Width / 2;
            var centerY = dirtyRect.Height / 2;
            var radius = Math.Min(centerX, centerY) - 10;

            // Total de novelas
            var total = _stats.TotalNovelsInLibrary;
            if (total == 0) return;

            // Convertir grados a radianes para cálculos manuales
            float DegreesToRadians(float degrees) => degrees * (float)Math.PI / 180f;

            // Ángulo inicial en radianes (arriba = -90 grados = -π/2 radianes)
            float currentAngle = DegreesToRadians(-90);

            // Dibujar cada segmento
            foreach (var (label, count, color) in _segments)
            {
                if (count > 0)
                {
                    // Calcular el ángulo del segmento en radianes
                    float sweepAngle = DegreesToRadians((float)(count * 360.0 / total));
                    float endAngle = currentAngle + sweepAngle;

                    // Crear path manualmente
                    var path = new PathF();

                    // Mover al centro
                    path.MoveTo(centerX, centerY);

                    // Línea al inicio del arco
                    float startX = centerX + radius * (float)Math.Cos(currentAngle);
                    float startY = centerY + radius * (float)Math.Sin(currentAngle);
                    path.LineTo(startX, startY);

                    // Dibujar el arco manualmente con pequeños segmentos
                    int segments = Math.Max(1, (int)(Math.Abs(sweepAngle) * 180 / Math.PI / 5)); // Un segmento cada 5 grados
                    for (int i = 1; i <= segments; i++)
                    {
                        float angle = currentAngle + (sweepAngle * i / segments);
                        float x = centerX + radius * (float)Math.Cos(angle);
                        float y = centerY + radius * (float)Math.Sin(angle);
                        path.LineTo(x, y);
                    }

                    // Volver al centro
                    path.LineTo(centerX, centerY);
                    path.Close();

                    // Rellenar el segmento
                    canvas.FillColor = color;
                    canvas.FillPath(path);

                    // Dibujar borde
                    canvas.StrokeColor = Color.FromArgb("#1A1A1A");
                    canvas.StrokeSize = 2;
                    canvas.DrawPath(path);

                    // Actualizar ángulo para el siguiente segmento
                    currentAngle = endAngle;
                }
            }

            // Dibujar círculo central (efecto donut)
            var innerRadius = radius * 0.5f;
            canvas.FillColor = Color.FromArgb("#1A1A1A");
            canvas.FillCircle(centerX, centerY, innerRadius);

            // Dibujar el total en el centro
            canvas.FontColor = Colors.White;
            canvas.FontSize = 24;
            canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.DrawString(
                total.ToString(),
                centerX - 20,
                centerY - 12,
                40,
                24,
                HorizontalAlignment.Center,
                VerticalAlignment.Center
            );

            // Texto "Total" debajo del número
            canvas.FontSize = 12;
            canvas.Font = Microsoft.Maui.Graphics.Font.Default;
            canvas.FontColor = Color.FromArgb("#B0B0B0");
            canvas.DrawString(
                "Total",
                centerX - 20,
                centerY + 8,
                40,
                16,
                HorizontalAlignment.Center,
                VerticalAlignment.Center
            );
        }
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
            var dayStats = _stats.Last7DaysStats?.FirstOrDefault(d => d.Date.Date == date);
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

        if (_stats.GenreStats == null || !_stats.GenreStats.Any())
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

            var nameLabel = new Label
            {
                Text = genre.GenreName,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                VerticalOptions = LayoutOptions.Center
            };
            grid.Children.Add(nameLabel);
            Grid.SetColumn(nameLabel, 0);

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

        if (_stats.AuthorStats == null || !_stats.AuthorStats.Any())
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
                Padding = 10,
                WidthRequest = 100,
                HeightRequest = 100,
                HasShadow = false
            };

            var stack = new StackLayout
            {
                Spacing = 3,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            var iconLabel = new Label
            {
                Text = achievement.Icon,
                FontSize = 26,
                HorizontalTextAlignment = TextAlignment.Center
            };
            stack.Children.Add(iconLabel);

            var nameLabel = new Label
            {
                Text = achievement.Name,
                FontSize = 11,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                MaxLines = 2,
                LineBreakMode = LineBreakMode.WordWrap,
                WidthRequest = 80
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
                HeightRequest = Math.Max(3, height * 100),
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
        if (hour >= 6 && hour < 12) return Color.FromArgb("#FFC107"); // Mañana Amarillo
        if (hour >= 12 && hour < 18) return Color.FromArgb("#4CAF50"); // Tarde Verde
        if (hour >= 18 && hour < 22) return Color.FromArgb("#2196F3"); // Noche Azul
        return Color.FromArgb("#9C27B0"); // Madrugada Morado
    }

    /// <summary>
    /// Obtiene el color para una hora basado en su actividad
    /// </summary>
   /* private Color GetColorForHour(int hour, int value, int maxValue)
    {
        if (value == 0) return Color.FromArgb("#2A2A2A");

        var intensity = (double)value / maxValue;

        if (intensity > 0.8) return Color.FromArgb("#8B5CF6"); // Muy activo
        if (intensity > 0.5) return Color.FromArgb("#A78BFA"); // Activo
        if (intensity > 0.2) return Color.FromArgb("#C4B5FD"); // Moderado
        return Color.FromArgb("#4A4A4A"); // Bajo
    }*/

    /// <summary>
    /// Muestra todos los logros
    /// </summary>
    private async void OnViewAllAchievements(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AchievementsPage(_stats));
    }

    /// <summary>
    /// Maneja el tap en el gráfico de géneros para ver más detalles
    /// </summary>
    private async void OnGenreStatsTapped(object sender, EventArgs e)
    {
        // TODO: Navegar a una página de detalles de géneros
        await DisplayAlert("Géneros", "Próximamente: Vista detallada de géneros", "OK");
    }

    /// <summary>
    /// Maneja el tap en el gráfico de autores para ver más detalles
    /// </summary>
    private async void OnAuthorStatsTapped(object sender, EventArgs e)
    {
        // TODO: Navegar a una página de detalles de autores
        await DisplayAlert("Autores", "Próximamente: Vista detallada de autores", "OK");
    }

    /// <summary>
    /// Maneja el tap en logros para ver todos
    /// </summary>
    private async void OnAchievementsTapped(object sender, EventArgs e)
    {
        // TODO: Navegar a una página de logros completa
        await DisplayAlert("Logros", "Próximamente: Vista completa de logros", "OK");
    }

    /// <summary>
    /// Exporta las estadísticas
    /// </summary>
    private async void OnExportStatsTapped(object sender, EventArgs e)
    {
        try
        {
            var action = await DisplayActionSheet(
                "Exportar estadísticas",
                "Cancelar",
                null,
                "📄 Exportar como PDF",
                "📊 Exportar como CSV",
                "📱 Compartir resumen"
            );

            switch (action)
            {
                case "📄 Exportar como PDF":
                    await ExportAsPDF();
                    break;
                case "📊 Exportar como CSV":
                    await ExportAsCSV();
                    break;
                case "📱 Compartir resumen":
                    await ShareSummary();
                    break;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al exportar: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Exporta las estadísticas como PDF
    /// </summary>
    private async Task ExportAsPDF()
    {
        // TODO: Implementar exportación a PDF
        await DisplayAlert("PDF", "Próximamente: Exportación a PDF", "OK");
    }

    /// <summary>
    /// Exporta las estadísticas como CSV
    /// </summary>
    private async Task ExportAsCSV()
    {
        // TODO: Implementar exportación a CSV
        await DisplayAlert("CSV", "Próximamente: Exportación a CSV", "OK");
    }

    /// <summary>
    /// Comparte un resumen de las estadísticas
    /// </summary>
    private async Task ShareSummary()
    {
        var summary = $"📚 Mis estadísticas en NovelBook\n\n" +
                     $"📖 Capítulos leídos: {_stats.TotalChaptersRead}\n" +
                     $"📚 Novelas leídas: {_stats.TotalNovelsRead}\n" +
                     $"⏱️ Tiempo total: {_stats.FormattedTotalTime}\n" +
                     $"🔥 Racha actual: {_stats.CurrentStreak} días\n" +
                     $"⭐ Género favorito: {_stats.FavoriteGenre ?? "N/A"}\n" +
                     $"✍️ Autor favorito: {_stats.FavoriteAuthor ?? "N/A"}\n" +
                     $"🏆 Logros: {_stats.UnlockedAchievements?.Count ?? 0}/10\n\n" +
                     $"¡Únete a NovelBook y comienza tu aventura de lectura!";

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Text = summary,
            Title = "Mis estadísticas de lectura"
        });
    }
}