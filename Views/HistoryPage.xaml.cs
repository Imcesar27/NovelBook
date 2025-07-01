using NovelBook.Models;
using NovelBook.Services;

namespace NovelBook.Views;

public partial class HistoryPage : ContentPage
{
    // Servicios
    private readonly HistoryService _historyService;
    private readonly DatabaseService _databaseService;
    private readonly ImageService _imageService;

    // Datos
    private List<ReadingHistoryItem> _fullHistory;
    private List<NovelHistoryGroup> _novelGroups;
    private string _currentView = "📅 Historial completo";

    public HistoryPage()
    {
        InitializeComponent();

        // Inicializar servicios
        _databaseService = new DatabaseService();
        _historyService = new HistoryService(_databaseService);
        _imageService = new ImageService(_databaseService);

        // Configurar picker
        ViewPicker.SelectedIndex = 0;
    }

    /// <summary>
    /// Se ejecuta cada vez que la página aparece
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Solo cargar si hay usuario logueado
        if (AuthService.CurrentUser != null)
        {
            await LoadHistoryData();
        }
        else
        {
            ShowNoUserMessage();
        }
    }

    /// <summary>
    /// Carga los datos del historial
    /// </summary>
    private async Task LoadHistoryData()
    {
        try
        {
            // Mostrar indicador de carga
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            HistoryContainer.Children.Clear();

            // Obtener estadísticas
            var stats = await _historyService.GetReadingStatsAsync(AuthService.CurrentUser.Id);
            UpdateStatistics(stats);

            // Cargar datos según la vista seleccionada
            await LoadViewData();

            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
        catch (Exception ex)
        {
            LoadingIndicator.IsVisible = false;
            await DisplayAlert("Error", "Error al cargar historial: " + ex.Message, "OK");
        }
    }

    /// <summary>
    /// Actualiza las estadísticas en el header
    /// </summary>
    private void UpdateStatistics(ReadingStats stats)
    {
        ChaptersReadLabel.Text = stats.TotalChaptersRead.ToString();
        TotalTimeLabel.Text = stats.FormattedTotalTime;
        StreakLabel.Text = stats.CurrentStreak.ToString();
    }

    /// <summary>
    /// Carga los datos según la vista seleccionada
    /// </summary>
    private async Task LoadViewData()
    {
        HistoryContainer.Children.Clear();

        switch (_currentView)
        {
            case "📅 Historial completo":
                await LoadCompleteHistory();
                break;

            case "📚 Por novela":
                await LoadNovelGroupedHistory();
                break;

            case "📆 Hoy":
                await LoadTodayHistory();
                break;

            case "📅 Esta semana":
                await LoadWeekHistory();
                break;

            case "📅 Este mes":
                await LoadMonthHistory();
                break;
        }
    }

    /// <summary>
    /// Carga el historial completo
    /// </summary>
    private async Task LoadCompleteHistory()
    {
        _fullHistory = await _historyService.GetUserHistoryAsync(AuthService.CurrentUser.Id);

        if (_fullHistory.Count == 0)
        {
            NoHistoryLabel.IsVisible = true;
            return;
        }

        NoHistoryLabel.IsVisible = false;

        // Agrupar por fecha
        var groupedByDate = _fullHistory.GroupBy(h => h.ReadAt.Date)
                                       .OrderByDescending(g => g.Key);

        foreach (var dateGroup in groupedByDate)
        {
            // Crear header de fecha
            var dateHeader = CreateDateHeader(dateGroup.Key);
            HistoryContainer.Children.Add(dateHeader);

            // Agregar elementos de esa fecha
            foreach (var item in dateGroup.OrderByDescending(h => h.ReadAt))
            {
                var historyCard = await CreateHistoryCard(item);
                HistoryContainer.Children.Add(historyCard);
            }
        }
    }

    /// <summary>
    /// Carga el historial agrupado por novela
    /// </summary>
    private async Task LoadNovelGroupedHistory()
    {
        _novelGroups = await _historyService.GetHistoryGroupedByNovelAsync(AuthService.CurrentUser.Id);

        if (_novelGroups.Count == 0)
        {
            NoHistoryLabel.IsVisible = true;
            return;
        }

        NoHistoryLabel.IsVisible = false;

        foreach (var group in _novelGroups)
        {
            var novelCard = await CreateNovelGroupCard(group);
            HistoryContainer.Children.Add(novelCard);
        }
    }

    /// <summary>
    /// Carga el historial de hoy
    /// </summary>
    private async Task LoadTodayHistory()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var todayHistory = await _historyService.GetHistoryByDateRangeAsync(
            AuthService.CurrentUser.Id, today, tomorrow);

        DisplayFilteredHistory(todayHistory, "No has leído nada hoy");
    }

    /// <summary>
    /// Carga el historial de la semana
    /// </summary>
    private async Task LoadWeekHistory()
    {
        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);

        var weekHistory = await _historyService.GetHistoryByDateRangeAsync(
            AuthService.CurrentUser.Id, weekStart, today.AddDays(1));

        DisplayFilteredHistory(weekHistory, "No has leído nada esta semana");
    }

    /// <summary>
    /// Carga el historial del mes
    /// </summary>
    private async Task LoadMonthHistory()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var monthHistory = await _historyService.GetHistoryByDateRangeAsync(
            AuthService.CurrentUser.Id, monthStart, today.AddDays(1));

        DisplayFilteredHistory(monthHistory, "No has leído nada este mes");
    }

    /// <summary>
    /// Muestra un historial filtrado
    /// </summary>
    private async void DisplayFilteredHistory(List<ReadingHistoryItem> items, string emptyMessage)
    {
        if (items.Count == 0)
        {
            NoHistoryLabel.Text = emptyMessage;
            NoHistoryLabel.IsVisible = true;
            return;
        }

        NoHistoryLabel.IsVisible = false;

        foreach (var item in items.OrderByDescending(h => h.ReadAt))
        {
            var historyCard = await CreateHistoryCard(item);
            HistoryContainer.Children.Add(historyCard);
        }
    }

    /// <summary>
    /// Crea un header de fecha
    /// </summary>
    private Label CreateDateHeader(DateTime date)
    {
        string dateText;
        if (date.Date == DateTime.Today)
            dateText = "Hoy";
        else if (date.Date == DateTime.Today.AddDays(-1))
            dateText = "Ayer";
        else
            dateText = date.ToString("dddd, dd 'de' MMMM",
                new System.Globalization.CultureInfo("es-ES"));

        return new Label
        {
            Text = dateText,
            TextColor = Colors.White,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 15, 0, 5)
        };
    }

    /// <summary>
    /// Crea una tarjeta de historial
    /// </summary>
    private async Task<Frame> CreateHistoryCard(ReadingHistoryItem item)
    {
        var frame = new Frame
        {
            BackgroundColor = Color.FromArgb("#1E1E1E"),
            CornerRadius = 10,
            Padding = 15,
            HasShadow = false
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = 60 },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 15
        };

        // Imagen de portada
        var coverImage = new Image
        {
            Source = await _imageService.GetCoverImageAsync(item.NovelCover),
            WidthRequest = 60,
            HeightRequest = 80,
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

        var novelLabel = new Label
        {
            Text = item.NovelTitle,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 16,
            LineBreakMode = LineBreakMode.TailTruncation
        };
        infoStack.Children.Add(novelLabel);

        var chapterLabel = new Label
        {
            Text = item.ChapterDescription,
            TextColor = Color.FromArgb("#CCCCCC"),
            FontSize = 14,
            LineBreakMode = LineBreakMode.TailTruncation
        };
        infoStack.Children.Add(chapterLabel);

        var progressStack = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            Spacing = 10
        };

        if (item.IsCompleted)
        {
            var completedLabel = new Label
            {
                Text = "✓ Completado",
                TextColor = Color.FromArgb("#4CAF50"),
                FontSize = 12
            };
            progressStack.Children.Add(completedLabel);
        }
        else if (item.ReadingProgress > 0)
        {
            var progressLabel = new Label
            {
                Text = $"📖 {item.ReadingProgress:F0}%",
                TextColor = Color.FromArgb("#FFA726"),
                FontSize = 12
            };
            progressStack.Children.Add(progressLabel);
        }

        infoStack.Children.Add(progressStack);

        grid.Children.Add(infoStack);
        Grid.SetColumn(infoStack, 1);

        // Tiempo y hora
        var timeStack = new StackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End,
            Spacing = 2
        };

        var timeLabel = new Label
        {
            Text = item.ReadAt.ToString("HH:mm"),
            TextColor = Color.FromArgb("#808080"),
            FontSize = 12,
            HorizontalTextAlignment = TextAlignment.End
        };
        timeStack.Children.Add(timeLabel);

        if (item.ReadingTime > 0)
        {
            var durationLabel = new Label
            {
                Text = item.FormattedReadingTime,
                TextColor = Color.FromArgb("#808080"),
                FontSize = 11,
                HorizontalTextAlignment = TextAlignment.End
            };
            timeStack.Children.Add(durationLabel);
        }

        grid.Children.Add(timeStack);
        Grid.SetColumn(timeStack, 2);

        frame.Content = grid;

        // Agregar tap para navegar a la novela
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) =>
        {
            await Navigation.PushAsync(new NovelDetailPage(item.NovelId));
        };
        frame.GestureRecognizers.Add(tapGesture);

        return frame;
    }

    /// <summary>
    /// Crea una tarjeta de novela agrupada
    /// </summary>
    private async Task<Frame> CreateNovelGroupCard(NovelHistoryGroup group)
    {
        var frame = new Frame
        {
            BackgroundColor = Color.FromArgb("#1E1E1E"),
            CornerRadius = 10,
            Padding = 15,
            HasShadow = false
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = 80 },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 15
        };

        // Imagen de portada
        var coverImage = new Image
        {
            Source = await _imageService.GetCoverImageAsync(group.NovelCover),
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
        var infoStack = new StackLayout { Spacing = 8 };

        var titleLabel = new Label
        {
            Text = group.NovelTitle,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 18,
            LineBreakMode = LineBreakMode.TailTruncation
        };
        infoStack.Children.Add(titleLabel);

        var authorLabel = new Label
        {
            Text = group.NovelAuthor,
            TextColor = Color.FromArgb("#CCCCCC"),
            FontSize = 14
        };
        infoStack.Children.Add(authorLabel);

        var statsGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            }
        };

        // Capítulos leídos
        var chaptersLabel = new Label
        {
            Text = $"📖 {group.ChaptersRead} capítulos",
            TextColor = Color.FromArgb("#E91E63"),
            FontSize = 12
        };
        statsGrid.Children.Add(chaptersLabel);
        Grid.SetRow(chaptersLabel, 0);
        Grid.SetColumn(chaptersLabel, 0);

        // Tiempo total
        var timeLabel = new Label
        {
            Text = $"⏱️ {group.FormattedTotalTime}",
            TextColor = Color.FromArgb("#E91E63"),
            FontSize = 12
        };
        statsGrid.Children.Add(timeLabel);
        Grid.SetRow(timeLabel, 0);
        Grid.SetColumn(timeLabel, 1);

        // Última lectura
        var lastReadLabel = new Label
        {
            Text = $"📅 {group.LastReadFormatted}",
            TextColor = Color.FromArgb("#808080"),
            FontSize = 12,
            Margin = new Thickness(0, 5, 0, 0)
        };
        statsGrid.Children.Add(lastReadLabel);
        Grid.SetRow(lastReadLabel, 1);
        Grid.SetColumnSpan(lastReadLabel, 2);

        infoStack.Children.Add(statsGrid);

        grid.Children.Add(infoStack);
        Grid.SetColumn(infoStack, 1);

        frame.Content = grid;

        // Agregar tap para navegar
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) =>
        {
            await Navigation.PushAsync(new NovelDetailPage(group.NovelId));
        };
        frame.GestureRecognizers.Add(tapGesture);

        return frame;
    }

    /// <summary>
    /// Muestra mensaje cuando no hay usuario
    /// </summary>
    private void ShowNoUserMessage()
    {
        HistoryContainer.Children.Clear();
        NoHistoryLabel.Text = "Inicia sesión para ver tu historial de lectura";
        NoHistoryLabel.IsVisible = true;

        // Ocultar estadísticas
        ChaptersReadLabel.Text = "0";
        TotalTimeLabel.Text = "0h";
        StreakLabel.Text = "0";
    }

    /// <summary>
    /// Maneja el cambio de vista
    /// </summary>
    private async void OnViewPickerChanged(object sender, EventArgs e)
    {
        if (ViewPicker.SelectedItem != null)
        {
            _currentView = ViewPicker.SelectedItem.ToString();
            await LoadViewData();
        }
    }

    /// <summary>
    /// Maneja el clic en limpiar historial
    /// </summary>
    private async void OnClearHistoryClicked(object sender, EventArgs e)
    {
        if (AuthService.CurrentUser == null) return;

        bool confirm = await DisplayAlert("Confirmar",
            "¿Estás seguro de que quieres borrar todo tu historial de lectura?",
            "Sí", "No");

        if (confirm)
        {
            var success = await _historyService.ClearAllHistoryAsync(AuthService.CurrentUser.Id);

            if (success)
            {
                await DisplayAlert("Éxito", "Tu historial ha sido borrado", "OK");
                await LoadHistoryData();
            }
            else
            {
                await DisplayAlert("Error", "No se pudo borrar el historial", "OK");
            }
        }
    }
}