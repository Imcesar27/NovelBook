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

    // Filtro actual de recomendaciones
    private string _currentRecommendationFilter = "pending";

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

            // Cargar tags populares
            await LoadPopularTagsAsync();

            // Cargar tags recientes
            await LoadRecentTagsAsync();

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
    /// Carga las etiquetas más populares
    /// </summary>
    private async Task LoadPopularTagsAsync()
    {
        try
        {
            var tags = await _analyticsService.GetPopularTagsStatsAsync(10);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                PopularTagsLayout.Children.Clear();

                if (tags.Count == 0)
                {
                    PopularTagsLayout.Children.Add(new Label
                    {
                        Text = "Aún no hay etiquetas populares",
                        TextColor = Color.FromArgb("#888888"),
                        FontSize = 12,
                        FontAttributes = FontAttributes.Italic
                    });
                    return;
                }

                foreach (var tag in tags)
                {
                    var frame = new Frame
                    {
                        BackgroundColor = Color.FromArgb("#6200EE"),
                        CornerRadius = 15,
                        Padding = new Thickness(12, 6),
                        HasShadow = false,
                        Margin = new Thickness(0, 0, 8, 8)
                    };

                    var stack = new HorizontalStackLayout { Spacing = 5 };

                    stack.Children.Add(new Label
                    {
                        Text = tag.TagName,
                        TextColor = Colors.White,
                        FontSize = 12,
                        FontAttributes = FontAttributes.Bold
                    });

                    stack.Children.Add(new Label
                    {
                        Text = $"({tag.NovelCount} nov, {tag.TotalVotes} votos)",
                        TextColor = Colors.White,
                        FontSize = 10,
                        Opacity = 0.8
                    });

                    frame.Content = stack;
                    PopularTagsLayout.Children.Add(frame);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando etiquetas populares: {ex.Message}");
        }
    }

    /// <summary>
    /// Carga las etiquetas más recientes
    /// </summary>
    private async Task LoadRecentTagsAsync()
    {
        try
        {
            var tags = await _analyticsService.GetRecentTagsAsync(10);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                RecentTagsLayout.Children.Clear();

                if (tags.Count == 0)
                {
                    RecentTagsLayout.Children.Add(new Label
                    {
                        Text = "No hay etiquetas aún",
                        TextColor = Color.FromArgb("#888888"),
                        FontSize = 12
                    });
                    return;
                }

                foreach (var tag in tags)
                {
                    // Calcular tiempo transcurrido
                    var timeAgo = GetTimeAgo(tag.CreatedAt);

                    var frame = new Frame
                    {
                        BackgroundColor = Application.Current.RequestedTheme == AppTheme.Light
                            ? Color.FromArgb("#E8E8E8")
                            : Color.FromArgb("#3D3D3D"),
                        CornerRadius = 15,
                        Padding = new Thickness(12, 6),
                        HasShadow = false,
                        Margin = new Thickness(0, 0, 8, 8)
                    };

                    var stack = new HorizontalStackLayout { Spacing = 5 };

                    stack.Children.Add(new Label
                    {
                        Text = tag.TagName,
                        TextColor = Application.Current.RequestedTheme == AppTheme.Light
                            ? Color.FromArgb("#333333")
                            : Colors.White,
                        FontSize = 12
                    });

                    stack.Children.Add(new Label
                    {
                        Text = $"· {timeAgo}",
                        TextColor = Color.FromArgb("#888888"),
                        FontSize = 10
                    });

                    frame.Content = stack;
                    RecentTagsLayout.Children.Add(frame);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando etiquetas recientes: {ex.Message}");
        }
    }

    /// <summary>
    /// Convierte una fecha en texto relativo (hace X minutos/horas/días)
    /// </summary>
    private string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.Now - dateTime;

        if (span.TotalMinutes < 1)
            return "ahora";
        if (span.TotalMinutes < 60)
            return $"hace {(int)span.TotalMinutes}m";
        if (span.TotalHours < 24)
            return $"hace {(int)span.TotalHours}h";
        if (span.TotalDays < 7)
            return $"hace {(int)span.TotalDays}d";
        if (span.TotalDays < 30)
            return $"hace {(int)(span.TotalDays / 7)}sem";

        return $"hace {(int)(span.TotalDays / 30)}mes";
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
    /// <summary>
    /// Carga las recomendaciones según el filtro actual
    /// </summary>
    private async Task LoadExistingRecommendationsAsync()
    {
        var recommendations = await _analyticsService.GetRecommendationsByStatusAsync(_currentRecommendationFilter, 20);
        var counts = await _analyticsService.GetRecommendationCountsAsync();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Actualizar contador
            int currentCount = _currentRecommendationFilter switch
            {
                "pending" => counts.Pending,
                "read" => counts.Read,
                "implemented" => counts.Implemented,
                _ => 0
            };
            RecommendationCountLabel.Text = $"{currentCount} recomendación(es) • Total: {counts.Pending + counts.Read + counts.Implemented}";

            // Actualizar contenedor
            RecommendationsContainer.Children.Clear();

            if (recommendations.Count == 0)
            {
                string emptyMessage = _currentRecommendationFilter switch
                {
                    "pending" => "No hay recomendaciones pendientes. ¡Presiona 'Generar' para crear nuevas!",
                    "read" => "No hay recomendaciones marcadas como leídas.",
                    "implemented" => "No hay recomendaciones implementadas aún.",
                    _ => "No hay recomendaciones."
                };
                RecommendationsContainer.Children.Add(CreateEmptyMessage(emptyMessage));
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
    /// Carga los patrones existentes (máximo 6)
    /// </summary>
    private async Task LoadExistingPatternsAsync()
    {
        // Limitar a 6 patrones para no saturar la pantalla
        var patterns = await _analyticsService.GetPatternsAsync(null, 6);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Actualizar contador
            PatternsCountLabel.Text = $"{patterns.Count} patrón(es) identificado(s)";

            PatternsContainer.Children.Clear();

            if (patterns.Count == 0)
            {
                PatternsContainer.Children.Add(CreateEmptyMessage("No hay patrones identificados. Se generarán automáticamente con las recomendaciones."));
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

    /// <summary>
    /// Desmarca una recomendación como leída
    /// </summary>
    private async void OnUnmarkAsReadClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int recId)
        {
            bool confirm = await DisplayAlert("Confirmar",
                "¿Deseas desmarcar esta recomendación como no leída?",
                "Sí", "No");

            if (confirm)
            {
                await _analyticsService.UnmarkRecommendationAsReadAsync(recId);
                await LoadExistingRecommendationsAsync();
            }
        }
    }

    /// <summary>
    /// Desmarca una recomendación como implementada
    /// </summary>
    private async void OnUnmarkAsImplementedClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int recId)
        {
            bool confirm = await DisplayAlert("Confirmar",
                "¿Deseas revertir esta recomendación a pendiente?",
                "Sí", "No");

            if (confirm)
            {
                await _analyticsService.UnmarkRecommendationAsImplementedAsync(recId);
                await LoadExistingRecommendationsAsync();
            }
        }
    }

    /// <summary>
    /// Evento al cambiar de pestaña en recomendaciones
    /// </summary>
    private async void OnRecommendationTabClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            // Determinar qué filtro aplicar
            if (button == PendingTabButton)
                _currentRecommendationFilter = "pending";
            else if (button == ReadTabButton)
                _currentRecommendationFilter = "read";
            else if (button == ImplementedTabButton)
                _currentRecommendationFilter = "implemented";

            // Actualizar visual de pestañas
            UpdateRecommendationTabsVisual();

            // Recargar recomendaciones
            await LoadExistingRecommendationsAsync();
        }
    }

    /// <summary>
    /// Actualiza el estilo visual de las pestañas de recomendaciones
    /// </summary>
    private void UpdateRecommendationTabsVisual()
    {
        var activeColor = Color.FromArgb("#6200EE");
        var inactiveColor = Application.Current.RequestedTheme == AppTheme.Light
            ? Color.FromArgb("#E0E0E0")
            : Color.FromArgb("#2D2D2D");
        var activeTextColor = Colors.White;
        var inactiveTextColor = Application.Current.RequestedTheme == AppTheme.Light
            ? Color.FromArgb("#666666")
            : Color.FromArgb("#AAAAAA");

        // Resetear todos
        PendingTabButton.BackgroundColor = inactiveColor;
        PendingTabButton.TextColor = inactiveTextColor;
        ReadTabButton.BackgroundColor = inactiveColor;
        ReadTabButton.TextColor = inactiveTextColor;
        ImplementedTabButton.BackgroundColor = inactiveColor;
        ImplementedTabButton.TextColor = inactiveTextColor;

        // Activar el seleccionado
        switch (_currentRecommendationFilter)
        {
            case "pending":
                PendingTabButton.BackgroundColor = activeColor;
                PendingTabButton.TextColor = activeTextColor;
                break;
            case "read":
                ReadTabButton.BackgroundColor = activeColor;
                ReadTabButton.TextColor = activeTextColor;
                break;
            case "implemented":
                ImplementedTabButton.BackgroundColor = activeColor;
                ImplementedTabButton.TextColor = activeTextColor;
                break;
        }
    }

    /// <summary>
    /// Evento al presionar el botón de limpiar recomendaciones
    /// </summary>
    private async void OnClearRecommendationsClicked(object sender, EventArgs e)
    {
        string filterName = _currentRecommendationFilter switch
        {
            "pending" => "pendientes",
            "read" => "leídas",
            "implemented" => "implementadas",
            _ => "todas"
        };

        var action = await DisplayActionSheet(
            $"¿Qué deseas eliminar?",
            "Cancelar",
            null,
            $"Solo {filterName} (pestaña actual)",
            "Todas las recomendaciones");

        if (action == null || action == "Cancelar")
            return;

        string filterToDelete;
        string confirmMessage;

        if (action.StartsWith("Solo"))
        {
            filterToDelete = _currentRecommendationFilter;
            confirmMessage = $"¿Estás seguro de eliminar todas las recomendaciones {filterName}?";
        }
        else
        {
            filterToDelete = "all";
            confirmMessage = "¿Estás seguro de eliminar TODAS las recomendaciones?\n\nEsta acción no se puede deshacer.";
        }

        bool confirm = await DisplayAlert("Confirmar eliminación", confirmMessage, "Sí, eliminar", "Cancelar");

        if (confirm)
        {
            try
            {
                ShowLoading(true);

                int deleted = await _analyticsService.DeleteRecommendationsAsync(filterToDelete);

                await DisplayAlert("Completado", $"Se eliminaron {deleted} recomendación(es).", "OK");

                await LoadExistingRecommendationsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo eliminar: {ex.Message}", "OK");
            }
            finally
            {
                ShowLoading(false);
            }
        }
    }

    /// <summary>
    /// Evento al presionar el botón de limpiar patrones
    /// </summary>
    private async void OnClearPatternsClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Confirmar eliminación",
            "¿Estás seguro de eliminar todos los patrones identificados?\n\nSe regenerarán al presionar 'Generar' en recomendaciones.",
            "Sí, eliminar",
            "Cancelar");

        if (confirm)
        {
            try
            {
                ShowLoading(true);

                int deleted = await _analyticsService.DeleteAllPatternsAsync();

                await DisplayAlert("Completado", $"Se eliminaron {deleted} patrón(es).", "OK");

                await LoadExistingPatternsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo eliminar: {ex.Message}", "OK");
            }
            finally
            {
                ShowLoading(false);
            }
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

        // Footer con confianza y estado
        var footerStack = new VerticalStackLayout { Spacing = 8, Margin = new Thickness(0, 5, 0, 0) };

        // Línea de confianza y fecha
        var infoLabel = new Label
        {
            Text = $"Confianza: {rec.GetConfidenceText()} • {rec.CreatedAt:dd/MM/yyyy HH:mm}",
            FontSize = 11,
            TextColor = Color.FromArgb(rec.GetConfidenceColor())
        };
        footerStack.Children.Add(infoLabel);

        // Botones de acción según el estado actual
        var buttonsStack = new HorizontalStackLayout { Spacing = 10 };

        if (_currentRecommendationFilter == "pending")
        {
            // En pendientes: Marcar leída, Marcar implementada
            var readBtn = new Button
            {
                Text = "Marcar Leída",
                FontSize = 11,
                BackgroundColor = Color.FromArgb("#2196F3"),
                TextColor = Colors.White,
                CornerRadius = 10,
                Padding = new Thickness(12, 6),
                CommandParameter = rec.Id
            };
            readBtn.Clicked += OnMarkAsReadClicked;
            buttonsStack.Children.Add(readBtn);

            var implementBtn = new Button
            {
                Text = "Marcar Implementada",
                FontSize = 11,
                BackgroundColor = Color.FromArgb("#4CAF50"),
                TextColor = Colors.White,
                CornerRadius = 10,
                Padding = new Thickness(12, 6),
                CommandParameter = rec.Id
            };
            implementBtn.Clicked += OnMarkAsImplementedClicked;
            buttonsStack.Children.Add(implementBtn);
        }
        else if (_currentRecommendationFilter == "read")
        {
            // En leídas: Desmarcar, Marcar implementada
            var unreadBtn = new Button
            {
                Text = "Desmarcar",
                FontSize = 11,
                BackgroundColor = Color.FromArgb("#FF9800"),
                TextColor = Colors.White,
                CornerRadius = 10,
                Padding = new Thickness(12, 6),
                CommandParameter = rec.Id
            };
            unreadBtn.Clicked += OnUnmarkAsReadClicked;
            buttonsStack.Children.Add(unreadBtn);

            var implementBtn = new Button
            {
                Text = "Marcar Implementada",
                FontSize = 11,
                BackgroundColor = Color.FromArgb("#4CAF50"),
                TextColor = Colors.White,
                CornerRadius = 10,
                Padding = new Thickness(12, 6),
                CommandParameter = rec.Id
            };
            implementBtn.Clicked += OnMarkAsImplementedClicked;
            buttonsStack.Children.Add(implementBtn);
        }
        else if (_currentRecommendationFilter == "implemented")
        {
            // En implementadas: Solo revertir
            var revertBtn = new Button
            {
                Text = "Revertir a Pendiente",
                FontSize = 11,
                BackgroundColor = Color.FromArgb("#F44336"),
                TextColor = Colors.White,
                CornerRadius = 10,
                Padding = new Thickness(12, 6),
                CommandParameter = rec.Id
            };
            revertBtn.Clicked += OnUnmarkAsImplementedClicked;
            buttonsStack.Children.Add(revertBtn);
        }

        footerStack.Children.Add(buttonsStack);
        mainStack.Children.Add(footerStack);

        frame.Content = mainStack;
        return frame;
    }

    /// <summary>
    /// Crea una tarjeta compacta para mostrar un patrón
    /// </summary>
    private Frame CreatePatternCard(ReadingPattern pattern)
    {
        var borderColor = pattern.PatternType switch
        {
            "content_preference" => Color.FromArgb("#2196F3"),    // Azul
            "engagement_pattern" => Color.FromArgb("#4CAF50"),    // Verde
            "abandonment_pattern" => Color.FromArgb("#F44336"),   // Rojo
            "completion_pattern" => Color.FromArgb("#9C27B0"),    // Púrpura
            _ => Color.FromArgb("#FF9800")                         // Naranja
        };

        var icon = pattern.PatternType switch
        {
            "content_preference" => "📚",
            "engagement_pattern" => "📈",
            "abandonment_pattern" => "📉",
            "completion_pattern" => "✅",
            _ => "🔍"
        };

        var frame = new Frame
        {
            BackgroundColor = Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#FFFFFF")
                : Color.FromArgb("#1E1E1E"),
            BorderColor = borderColor,
            CornerRadius = 8,
            Padding = 12,
            HasShadow = false
        };

        // Layout horizontal compacto
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };

        // Icono
        var iconLabel = new Label
        {
            Text = icon,
            FontSize = 20,
            VerticalOptions = LayoutOptions.Center
        };

        // Contenido central
        var contentStack = new VerticalStackLayout { Spacing = 2 };

        contentStack.Children.Add(new Label
        {
            Text = pattern.PatternName,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Application.Current.RequestedTheme == AppTheme.Light
                ? Color.FromArgb("#1A1A1A")
                : Color.FromArgb("#FFFFFF"),
            LineBreakMode = LineBreakMode.TailTruncation
        });

        contentStack.Children.Add(new Label
        {
            Text = pattern.PatternValue,
            FontSize = 11,
            TextColor = Color.FromArgb("#888888"),
            LineBreakMode = LineBreakMode.TailTruncation
        });

        // Badge de confianza
        var confidenceBadge = new Frame
        {
            BackgroundColor = pattern.Confidence >= 0.7m
                ? Color.FromArgb("#4CAF50")
                : pattern.Confidence >= 0.5m
                    ? Color.FromArgb("#FF9800")
                    : Color.FromArgb("#9E9E9E"),
            CornerRadius = 8,
            Padding = new Thickness(8, 4),
            HasShadow = false,
            VerticalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Text = $"{pattern.Confidence:P0}",
                FontSize = 10,
                TextColor = Colors.White
            }
        };

        grid.Children.Add(iconLabel);
        Grid.SetColumn(iconLabel, 0);

        grid.Children.Add(contentStack);
        Grid.SetColumn(contentStack, 1);

        grid.Children.Add(confidenceBadge);
        Grid.SetColumn(confidenceBadge, 2);

        frame.Content = grid;
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