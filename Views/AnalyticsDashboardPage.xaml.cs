using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

/// <summary>
/// Página del Dashboard de Analytics para administradores
/// Muestra métricas, recomendaciones y patrones de lectura
/// </summary>
public partial class AnalyticsDashboardPage : ContentPage
{
    // Servicios
    private readonly AnalyticsService _analyticsService;
    private readonly DatabaseService _databaseService;

    /// <summary>
    /// Constructor de la página
    /// </summary>
    public AnalyticsDashboardPage()
    {
        InitializeComponent();

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _analyticsService = new AnalyticsService(_databaseService);
    }

    /// <summary>
    /// Se ejecuta cuando la página aparece
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Verificar si el usuario es administrador
        if (AuthService.CurrentUser == null || AuthService.CurrentUser.Role != "admin")
        {
            await DisplayAlert("Acceso Denegado",
                "Esta sección es solo para administradores.", "OK");
            await Navigation.PopAsync();
            return;
        }

        // Cargar datos iniciales
        await LoadDashboardDataAsync();
    }

    /// <summary>
    /// Carga todos los datos del dashboard
    /// </summary>
    private async Task LoadDashboardDataAsync()
    {
        try
        {
            ShowLoading(true);

            // Cargar estadísticas generales
            await LoadGeneralStatsAsync();

            // Cargar top novelas
            await LoadTopNovelsAsync();

            // Cargar top géneros
            await LoadTopGenresAsync();

            // Cargar top autores
            await LoadTopAuthorsAsync();

            // Cargar recomendaciones existentes
            await LoadExistingRecommendationsAsync();

            // Cargar patrones existentes
            await LoadExistingPatternsAsync();

            // Actualizar fecha
            LastUpdateLabel.Text = $"Última actualización: {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar datos: {ex.Message}", "OK");
        }
        finally
        {
            ShowLoading(false);
        }
    }

    /// <summary>
    /// Carga las estadísticas generales
    /// </summary>
    private async Task LoadGeneralStatsAsync()
    {
        var stats = await _analyticsService.GetGeneralStatsAsync();

        // Actualizar labels en el hilo principal
        MainThread.BeginInvokeOnMainThread(() =>
        {
            TotalUsersLabel.Text = stats.ContainsKey("TotalUsers") ? stats["TotalUsers"].ToString() : "0";
            TotalNovelsLabel.Text = stats.ContainsKey("TotalNovels") ? stats["TotalNovels"].ToString() : "0";
            TotalChaptersLabel.Text = stats.ContainsKey("TotalChapters") ? stats["TotalChapters"].ToString() : "0";
            TotalReviewsLabel.Text = stats.ContainsKey("TotalReviews") ? stats["TotalReviews"].ToString() : "0";
            TotalGenresLabel.Text = stats.ContainsKey("TotalGenres") ? stats["TotalGenres"].ToString() : "0";

            if (stats.ContainsKey("AverageRating"))
            {
                var avgRating = Convert.ToDecimal(stats["AverageRating"]);
                AvgRatingLabel.Text = $"{avgRating:F1}";
            }
        });

        // Cargar tasa de abandono y usuarios activos
        var abandonmentRate = await _analyticsService.CalculateAbandonmentRateAsync();
        var activeUsers = await _analyticsService.GetActiveUsersCountAsync();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            AbandonmentRateLabel.Text = $"{abandonmentRate:F1}%";
            ActiveUsersLabel.Text = activeUsers.ToString();
        });
    }

    /// <summary>
    /// Carga las novelas más leídas
    /// </summary>
    private async Task LoadTopNovelsAsync()
    {
        var topNovels = await _analyticsService.GetMostReadNovelsAsync(5);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            TopNovelsContainer.Children.Clear();

            if (topNovels.Count == 0)
            {
                TopNovelsContainer.Children.Add(CreateEmptyMessage("No hay datos de lectura disponibles"));
                return;
            }

            int rank = 1;
            foreach (var (novel, readCount) in topNovels)
            {
                var card = CreateNovelCard(rank, novel, readCount);
                TopNovelsContainer.Children.Add(card);
                rank++;
            }
        });
    }

    /// <summary>
    /// Carga los géneros más populares
    /// </summary>
    private async Task LoadTopGenresAsync()
    {
        var topGenres = await _analyticsService.GetPopularGenresStatsAsync(5);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            TopGenresContainer.Children.Clear();

            if (topGenres.Count == 0)
            {
                TopGenresContainer.Children.Add(CreateEmptyMessage("No hay datos de géneros disponibles"));
                return;
            }

            int rank = 1;
            foreach (var (genre, novelCount, readCount, avgRating) in topGenres)
            {
                var card = CreateGenreCard(rank, genre, novelCount, readCount, avgRating);
                TopGenresContainer.Children.Add(card);
                rank++;
            }
        });
    }

    /// <summary>
    /// Carga los autores con mejor engagement
    /// </summary>
    private async Task LoadTopAuthorsAsync()
    {
        var topAuthors = await _analyticsService.GetTopAuthorsAsync(5);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            TopAuthorsContainer.Children.Clear();

            if (topAuthors.Count == 0)
            {
                TopAuthorsContainer.Children.Add(CreateEmptyMessage("No hay datos de autores disponibles"));
                return;
            }

            int rank = 1;
            foreach (var (author, novelCount, readCount, avgRating) in topAuthors)
            {
                var card = CreateAuthorCard(rank, author, novelCount, readCount, avgRating);
                TopAuthorsContainer.Children.Add(card);
                rank++;
            }
        });
    }

    /// <summary>
    /// Carga las recomendaciones existentes
    /// </summary>
    private async Task LoadExistingRecommendationsAsync()
    {
        var recommendations = await _analyticsService.GetAllRecommendationsAsync(10);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            RecommendationsContainer.Children.Clear();

            if (recommendations.Count == 0)
            {
                RecommendationsContainer.Children.Add(
                    CreateEmptyMessage("Presiona 'Generar' para analizar datos y obtener recomendaciones..."));
                return;
            }

            foreach (var rec in recommendations)
            {
                var card = CreateRecommendationCard(rec);
                RecommendationsContainer.Children.Add(card);
            }
        });
    }

    /// <summary>
    /// Carga los patrones existentes
    /// </summary>
    private async Task LoadExistingPatternsAsync()
    {
        var patterns = await _analyticsService.GetPatternsAsync(null, 10);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            PatternsContainer.Children.Clear();

            if (patterns.Count == 0)
            {
                PatternsContainer.Children.Add(
                    CreateEmptyMessage("Los patrones se generan junto con las recomendaciones..."));
                return;
            }

            foreach (var pattern in patterns)
            {
                var card = CreatePatternCard(pattern);
                PatternsContainer.Children.Add(card);
            }
        });
    }

    #region ========== EVENTOS ==========

    /// <summary>
    /// Evento al presionar el botón de actualizar
    /// </summary>
    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadDashboardDataAsync();
    }

    /// <summary>
    /// Evento al presionar el botón de generar recomendaciones
    /// </summary>
    private async void OnGenerateRecommendationsClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Generar Análisis",
            "¿Deseas ejecutar el análisis completo y generar nuevas recomendaciones?\n\n" +
            "Esto puede tomar unos segundos.", "Sí, generar", "Cancelar");

        if (!confirm) return;

        try
        {
            ShowLoading(true);

            // Generar recomendaciones
            var recommendations = await _analyticsService.GenerateAllRecommendationsAsync();

            // Generar patrones
            var patterns = await _analyticsService.IdentifyAllPatternsAsync();

            // Recargar datos
            await LoadExistingRecommendationsAsync();
            await LoadExistingPatternsAsync();

            await DisplayAlert("Análisis Completado",
                $"Se generaron {recommendations.Count} recomendaciones y {patterns.Count} patrones.",
                "OK");

            LastUpdateLabel.Text = $"Última actualización: {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al generar análisis: {ex.Message}", "OK");
        }
        finally
        {
            ShowLoading(false);
        }
    }

    /// <summary>
    /// Marca una recomendación como leída
    /// </summary>
    private async void OnMarkAsReadClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int recId)
        {
            await _analyticsService.MarkRecommendationAsReadAsync(recId);
            await LoadExistingRecommendationsAsync();
        }
    }

    /// <summary>
    /// Marca una recomendación como implementada
    /// </summary>
    private async void OnMarkAsImplementedClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int recId)
        {
            await _analyticsService.MarkRecommendationAsImplementedAsync(recId);
            await LoadExistingRecommendationsAsync();
        }
    }

    #endregion

    #region ========== CREACIÓN DE TARJETAS UI ==========

    /// <summary>
    /// Crea una tarjeta para mostrar una novela
    /// </summary>
    private Frame CreateNovelCard(int rank, Novel novel, int readCount)
    {
        var frame = new Frame
        {
            BackgroundColor = Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#FFFFFF")
                : Color.FromArgb("#1E1E1E"),
            CornerRadius = 10,
            Padding = 12,
            HasShadow = false
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(new GridLength(40)),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        // Ranking
        var rankLabel = new Label
        {
            Text = $"#{rank}",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = GetRankColor(rank),
            VerticalOptions = LayoutOptions.Center
        };

        // Info de la novela
        var infoStack = new VerticalStackLayout { Margin = new Thickness(10, 0, 0, 0) };
        infoStack.Children.Add(new Label
        {
            Text = novel.Title,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#1A1A1A")
                : Color.FromArgb("#FFFFFF"),
            LineBreakMode = LineBreakMode.TailTruncation
        });
        infoStack.Children.Add(new Label
        {
            Text = $"por {novel.Author}",
            FontSize = 12,
            TextColor = Color.FromArgb("#888888")
        });

        // Estadísticas
        var statsStack = new VerticalStackLayout { HorizontalOptions = LayoutOptions.End };
        statsStack.Children.Add(new Label
        {
            Text = $"📖 {readCount}",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#4CAF50"),
            HorizontalOptions = LayoutOptions.End
        });
        statsStack.Children.Add(new Label
        {
            Text = $"⭐ {novel.Rating:F1}",
            FontSize = 12,
            TextColor = Color.FromArgb("#FFC107"),
            HorizontalOptions = LayoutOptions.End
        });

        grid.Children.Add(rankLabel);
        Grid.SetColumn(rankLabel, 0);

        grid.Children.Add(infoStack);
        Grid.SetColumn(infoStack, 1);

        grid.Children.Add(statsStack);
        Grid.SetColumn(statsStack, 2);

        frame.Content = grid;
        return frame;
    }

    /// <summary>
    /// Crea una tarjeta para mostrar un género
    /// </summary>
    private Frame CreateGenreCard(int rank, string genre, int novelCount, int readCount, decimal avgRating)
    {
        var frame = new Frame
        {
            BackgroundColor = Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#FFFFFF")
                : Color.FromArgb("#1E1E1E"),
            CornerRadius = 10,
            Padding = 12,
            HasShadow = false
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(new GridLength(40)),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        // Ranking
        var rankLabel = new Label
        {
            Text = $"#{rank}",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = GetRankColor(rank),
            VerticalOptions = LayoutOptions.Center
        };

        // Info del género
        var infoStack = new VerticalStackLayout { Margin = new Thickness(10, 0, 0, 0) };
        infoStack.Children.Add(new Label
        {
            Text = genre,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#1A1A1A")
                : Color.FromArgb("#FFFFFF")
        });
        infoStack.Children.Add(new Label
        {
            Text = $"{novelCount} novelas",
            FontSize = 12,
            TextColor = Color.FromArgb("#888888")
        });

        // Estadísticas
        var statsStack = new VerticalStackLayout { HorizontalOptions = LayoutOptions.End };
        statsStack.Children.Add(new Label
        {
            Text = $"📖 {readCount}",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#4CAF50"),
            HorizontalOptions = LayoutOptions.End
        });
        statsStack.Children.Add(new Label
        {
            Text = $"⭐ {avgRating:F1}",
            FontSize = 12,
            TextColor = Color.FromArgb("#FFC107"),
            HorizontalOptions = LayoutOptions.End
        });

        grid.Children.Add(rankLabel);
        Grid.SetColumn(rankLabel, 0);

        grid.Children.Add(infoStack);
        Grid.SetColumn(infoStack, 1);

        grid.Children.Add(statsStack);
        Grid.SetColumn(statsStack, 2);

        frame.Content = grid;
        return frame;
    }

    /// <summary>
    /// Crea una tarjeta para mostrar un autor
    /// </summary>
    private Frame CreateAuthorCard(int rank, string author, int novelCount, int readCount, decimal avgRating)
    {
        var frame = new Frame
        {
            BackgroundColor = Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#FFFFFF")
                : Color.FromArgb("#1E1E1E"),
            CornerRadius = 10,
            Padding = 12,
            HasShadow = false
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(new GridLength(40)),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        // Ranking
        var rankLabel = new Label
        {
            Text = $"#{rank}",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = GetRankColor(rank),
            VerticalOptions = LayoutOptions.Center
        };

        // Info del autor
        var infoStack = new VerticalStackLayout { Margin = new Thickness(10, 0, 0, 0) };
        infoStack.Children.Add(new Label
        {
            Text = author,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#1A1A1A")
                : Color.FromArgb("#FFFFFF"),
            LineBreakMode = LineBreakMode.TailTruncation
        });
        infoStack.Children.Add(new Label
        {
            Text = $"{novelCount} novela(s)",
            FontSize = 12,
            TextColor = Color.FromArgb("#888888")
        });

        // Estadísticas
        var statsStack = new VerticalStackLayout { HorizontalOptions = LayoutOptions.End };
        statsStack.Children.Add(new Label
        {
            Text = $"📖 {readCount}",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#4CAF50"),
            HorizontalOptions = LayoutOptions.End
        });
        statsStack.Children.Add(new Label
        {
            Text = $"⭐ {avgRating:F1}",
            FontSize = 12,
            TextColor = Color.FromArgb("#FFC107"),
            HorizontalOptions = LayoutOptions.End
        });

        grid.Children.Add(rankLabel);
        Grid.SetColumn(rankLabel, 0);

        grid.Children.Add(infoStack);
        Grid.SetColumn(infoStack, 1);

        grid.Children.Add(statsStack);
        Grid.SetColumn(statsStack, 2);

        frame.Content = grid;
        return frame;
    }

    /// <summary>
    /// Crea una tarjeta para mostrar una recomendación
    /// </summary>
    private Frame CreateRecommendationCard(AdminRecommendation rec)
    {
        var borderColor = rec.Priority switch
        {
            3 => Color.FromArgb("#F44336"),  // Alta - Rojo
            2 => Color.FromArgb("#FF9800"),  // Media - Naranja
            _ => Color.FromArgb("#4CAF50")   // Baja - Verde
        };

        var frame = new Frame
        {
            BackgroundColor = Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#FFFFFF")
                : Color.FromArgb("#1E1E1E"),
            BorderColor = borderColor,
            CornerRadius = 10,
            Padding = 15,
            HasShadow = false
        };

        var mainStack = new VerticalStackLayout { Spacing = 8 };

        // Header con icono y título
        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        var iconLabel = new Label
        {
            Text = rec.GetIcon(),
            FontSize = 24,
            VerticalOptions = LayoutOptions.Center
        };

        var titleLabel = new Label
        {
            Text = rec.Title,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#1A1A1A")
                : Color.FromArgb("#FFFFFF"),
            Margin = new Thickness(10, 0, 0, 0),
            VerticalOptions = LayoutOptions.Center
        };

        var priorityBadge = new Frame
        {
            BackgroundColor = borderColor,
            CornerRadius = 10,
            Padding = new Thickness(8, 2),
            HasShadow = false,
            Content = new Label
            {
                Text = rec.GetPriorityText(),
                FontSize = 10,
                TextColor = Colors.White
            }
        };

        headerGrid.Children.Add(iconLabel);
        Grid.SetColumn(iconLabel, 0);

        headerGrid.Children.Add(titleLabel);
        Grid.SetColumn(titleLabel, 1);

        headerGrid.Children.Add(priorityBadge);
        Grid.SetColumn(priorityBadge, 2);

        mainStack.Children.Add(headerGrid);

        // Descripción
        mainStack.Children.Add(new Label
        {
            Text = rec.Description,
            FontSize = 12,
            TextColor = Color.FromArgb("#888888"),
            LineBreakMode = LineBreakMode.WordWrap
        });

        // Footer con confianza y acciones
        var footerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            },
            Margin = new Thickness(0, 5, 0, 0)
        };

        var confidenceLabel = new Label
        {
            Text = $"Confianza: {rec.GetConfidenceText()}",
            FontSize = 11,
            TextColor = Color.FromArgb(rec.GetConfidenceColor()),
            VerticalOptions = LayoutOptions.Center
        };

        footerGrid.Children.Add(confidenceLabel);
        Grid.SetColumn(confidenceLabel, 0);

        // Botones de acción (solo si no está implementada)
        if (!rec.IsImplemented)
        {
            if (!rec.IsRead)
            {
                var readBtn = new Button
                {
                    Text = "✓ Leída",
                    FontSize = 10,
                    BackgroundColor = Color.FromArgb("#2196F3"),
                    TextColor = Colors.White,
                    CornerRadius = 10,
                    Padding = new Thickness(10, 5),
                    CommandParameter = rec.Id
                };
                readBtn.Clicked += OnMarkAsReadClicked;

                footerGrid.Children.Add(readBtn);
                Grid.SetColumn(readBtn, 1);
            }

            var implementBtn = new Button
            {
                Text = "✓ Implementada",
                FontSize = 10,
                BackgroundColor = Color.FromArgb("#4CAF50"),
                TextColor = Colors.White,
                CornerRadius = 10,
                Padding = new Thickness(10, 5),
                Margin = new Thickness(5, 0, 0, 0),
                CommandParameter = rec.Id
            };
            implementBtn.Clicked += OnMarkAsImplementedClicked;

            footerGrid.Children.Add(implementBtn);
            Grid.SetColumn(implementBtn, 2);
        }
        else
        {
            var implementedLabel = new Label
            {
                Text = "✓ Implementada",
                FontSize = 11,
                TextColor = Color.FromArgb("#4CAF50"),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End
            };

            footerGrid.Children.Add(implementedLabel);
            Grid.SetColumn(implementedLabel, 2);
        }

        mainStack.Children.Add(footerGrid);

        frame.Content = mainStack;
        return frame;
    }

    /// <summary>
    /// Crea una tarjeta para mostrar un patrón
    /// </summary>
    private Frame CreatePatternCard(ReadingPattern pattern)
    {
        var frame = new Frame
        {
            BackgroundColor = Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#FFFFFF")
                : Color.FromArgb("#1E1E1E"),
            BorderColor = Color.FromArgb(pattern.GetThemeColor()),
            CornerRadius = 10,
            Padding = 15,
            HasShadow = false
        };

        var mainStack = new VerticalStackLayout { Spacing = 5 };

        // Header
        var headerStack = new HorizontalStackLayout { Spacing = 10 };
        headerStack.Children.Add(new Label
        {
            Text = pattern.GetIcon(),
            FontSize = 20,
            VerticalOptions = LayoutOptions.Center
        });
        headerStack.Children.Add(new Label
        {
            Text = pattern.PatternName,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#1A1A1A")
                : Color.FromArgb("#FFFFFF"),
            VerticalOptions = LayoutOptions.Center
        });

        mainStack.Children.Add(headerStack);

        // Valor/Descripción
        mainStack.Children.Add(new Label
        {
            Text = pattern.PatternValue,
            FontSize = 12,
            TextColor = Color.FromArgb("#888888"),
            LineBreakMode = LineBreakMode.WordWrap
        });

        // Confianza
        mainStack.Children.Add(new Label
        {
            Text = $"Confianza: {pattern.GetConfidenceText()} • Tipo: {pattern.GetPatternTypeText()}",
            FontSize = 10,
            TextColor = Color.FromArgb(pattern.GetConfidenceColor()),
            Margin = new Thickness(0, 5, 0, 0)
        });

        frame.Content = mainStack;
        return frame;
    }

    /// <summary>
    /// Crea un mensaje de estado vacío
    /// </summary>
    private Frame CreateEmptyMessage(string message)
    {
        return new Frame
        {
            BackgroundColor = Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#FFFFFF")
                : Color.FromArgb("#1E1E1E"),
            CornerRadius = 10,
            Padding = 15,
            HasShadow = false,
            Content = new Label
            {
                Text = message,
                TextColor = Color.FromArgb("#888888"),
                HorizontalOptions = LayoutOptions.Center
            }
        };
    }

    /// <summary>
    /// Obtiene el color según el ranking
    /// </summary>
    private Color GetRankColor(int rank)
    {
        return rank switch
        {
            1 => Color.FromArgb("#FFD700"),  // Oro
            2 => Color.FromArgb("#C0C0C0"),  // Plata
            3 => Color.FromArgb("#CD7F32"),  // Bronce
            _ => Color.FromArgb("#888888")   // Gris
        };
    }

    /// <summary>
    /// Muestra u oculta el indicador de carga
    /// </summary>
    private void ShowLoading(bool show)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LoadingIndicator.IsRunning = show;
            LoadingIndicator.IsVisible = show;
        });
    }

    #endregion
}