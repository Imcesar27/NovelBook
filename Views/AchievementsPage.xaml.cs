using NovelBook.Models;

namespace NovelBook.Views;

public partial class AchievementsPage : ContentPage
{
    private readonly ExtendedStats _stats;
    private readonly List<Achievement> _allAchievements;
    private string _currentFilter = "All";

    public AchievementsPage(ExtendedStats stats)
    {
        InitializeComponent();
        _stats = stats;

        // Crear lista completa de logros
        _allAchievements = GetAllAchievements();

        // Actualizar progreso general
        UpdateOverallProgress();

        // Mostrar todos los logros
        DisplayAchievements("All");
    }

    /// <summary>
    /// Obtiene todos los logros posibles
    /// </summary>
    private List<Achievement> GetAllAchievements()
    {
        return new List<Achievement>
        {
            new Achievement
            {
                Id = "first_chapter",
                Name = "Primera Página",
                Description = "Lee tu primer capítulo",
                Icon = "📖",
                Type = AchievementType.ChaptersRead,
                Target = 1,
                Progress = _stats.TotalChaptersRead
            },
            new Achievement
            {
                Id = "bookworm",
                Name = "Ratón de Biblioteca",
                Description = "Lee 100 capítulos",
                Icon = "📚",
                Type = AchievementType.ChaptersRead,
                Target = 100,
                Progress = _stats.TotalChaptersRead
            },
            new Achievement
            {
                Id = "dedicated_reader",
                Name = "Lector Dedicado",
                Description = "Lee 500 capítulos",
                Icon = "🎓",
                Type = AchievementType.ChaptersRead,
                Target = 500,
                Progress = _stats.TotalChaptersRead
            },
            new Achievement
            {
                Id = "chapter_master",
                Name = "Maestro de Capítulos",
                Description = "Lee 1000 capítulos",
                Icon = "👨‍🎓",
                Type = AchievementType.ChaptersRead,
                Target = 1000,
                Progress = _stats.TotalChaptersRead
            },
            new Achievement
            {
                Id = "first_complete",
                Name = "Primera Victoria",
                Description = "Completa tu primera novela",
                Icon = "🏆",
                Type = AchievementType.NovelsCompleted,
                Target = 1,
                Progress = _stats.NovelsCompleted
            },
            new Achievement
            {
                Id = "completionist",
                Name = "Completista",
                Description = "Completa 10 novelas",
                Icon = "👑",
                Type = AchievementType.NovelsCompleted,
                Target = 10,
                Progress = _stats.NovelsCompleted
            },
            new Achievement
            {
                Id = "week_streak",
                Name = "Semana Perfecta",
                Description = "Lee durante 7 días seguidos",
                Icon = "🔥",
                Type = AchievementType.ReadingStreak,
                Target = 7,
                Progress = _stats.CurrentStreak
            },
            new Achievement
            {
                Id = "month_streak",
                Name = "Mes Imparable",
                Description = "Lee durante 30 días seguidos",
                Icon = "💎",
                Type = AchievementType.ReadingStreak,
                Target = 30,
                Progress = _stats.CurrentStreak
            },
            new Achievement
            {
                Id = "genre_explorer",
                Name = "Explorador de Géneros",
                Description = "Lee novelas de 5 géneros diferentes",
                Icon = "🗺️",
                Type = AchievementType.GenreExplorer,
                Target = 5,
                Progress = _stats.GenreStats.Count
            },
            new Achievement
            {
                Id = "organizer",
                Name = "Organizador",
                Description = "Crea 5 categorías personalizadas",
                Icon = "📁",
                Type = AchievementType.Categories,
                Target = 5,
                Progress = _stats.TotalCategories
            }
        };
    }

    /// <summary>
    /// Actualiza el progreso general
    /// </summary>
    private void UpdateOverallProgress()
    {
        var unlockedCount = _allAchievements.Count(a => a.IsUnlocked);
        var totalCount = _allAchievements.Count;

        OverallProgressBar.Progress = (double)unlockedCount / totalCount;
        ProgressLabel.Text = $"{unlockedCount}/{totalCount} logros desbloqueados";
    }

    /// <summary>
    /// Maneja el filtrado de logros
    /// </summary>
    private void OnFilterClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string filter)
        {
            _currentFilter = filter;

            // Actualizar visual de botones
            UpdateFilterButtons(filter);

            // Mostrar logros filtrados
            DisplayAchievements(filter);
        }
    }

    /// <summary>
    /// Actualiza el estado visual de los botones de filtro
    /// </summary>
    private void UpdateFilterButtons(string selectedFilter)
    {
        // Resetear todos los botones
        AllButton.BackgroundColor = Color.FromArgb("#3D3D3D");
        AllButton.TextColor = Color.FromArgb("#B0B0B0");
        UnlockedButton.BackgroundColor = Color.FromArgb("#3D3D3D");
        UnlockedButton.TextColor = Color.FromArgb("#B0B0B0");
        LockedButton.BackgroundColor = Color.FromArgb("#3D3D3D");
        LockedButton.TextColor = Color.FromArgb("#B0B0B0");

        // Resaltar el seleccionado
        var selectedButton = selectedFilter switch
        {
            "All" => AllButton,
            "Unlocked" => UnlockedButton,
            "Locked" => LockedButton,
            _ => AllButton
        };

        selectedButton.BackgroundColor = Color.FromArgb("#8B5CF6");
        selectedButton.TextColor = Colors.White;
    }

    /// <summary>
    /// Muestra los logros según el filtro
    /// </summary>
    private void DisplayAchievements(string filter)
    {
        AchievementsContainer.Children.Clear();

        var achievementsToShow = filter switch
        {
            "Unlocked" => _allAchievements.Where(a => a.IsUnlocked),
            "Locked" => _allAchievements.Where(a => !a.IsUnlocked),
            _ => _allAchievements
        };

        foreach (var achievement in achievementsToShow)
        {
            var achievementFrame = CreateAchievementFrame(achievement);
            AchievementsContainer.Children.Add(achievementFrame);
        }

        if (!achievementsToShow.Any())
        {
            var emptyLabel = new Label
            {
                Text = filter == "Unlocked" ?
                       "No has desbloqueado ningún logro aún" :
                       "¡Has desbloqueado todos los logros!",
                TextColor = Color.FromArgb("#B0B0B0"),
                FontSize = 16,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 20)
            };
            AchievementsContainer.Children.Add(emptyLabel);
        }
    }

    /// <summary>
    /// Crea el frame visual para un logro
    /// </summary>
    private Frame CreateAchievementFrame(Achievement achievement)
    {
        var frame = new Frame
        {
            BackgroundColor = achievement.IsUnlocked ?
                             Color.FromArgb("#3D3D3D") :
                             Color.FromArgb("#2D2D2D"),
            CornerRadius = 15,
            Padding = 15,
            HasShadow = false,
            Opacity = achievement.IsUnlocked ? 1.0 : 0.6
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        // Ícono
        var iconFrame = new Frame
        {
            BackgroundColor = achievement.IsUnlocked ?
                             GetAchievementColor(achievement.Type) :
                             Color.FromArgb("#404040"),
            CornerRadius = 30,
            WidthRequest = 60,
            HeightRequest = 60,
            Padding = 0,
            HasShadow = false
        };

        var iconLabel = new Label
        {
            Text = achievement.Icon,
            FontSize = 28,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        iconFrame.Content = iconLabel;
        grid.Children.Add(iconFrame);
        Grid.SetColumn(iconFrame, 0);

        // Información
        var infoStack = new StackLayout
        {
            Spacing = 5,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(15, 0)
        };

        var nameLabel = new Label
        {
            Text = achievement.Name,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = achievement.IsUnlocked ? Colors.White : Color.FromArgb("#808080")
        };
        infoStack.Children.Add(nameLabel);

        var descriptionLabel = new Label
        {
            Text = achievement.Description,
            FontSize = 14,
            TextColor = Color.FromArgb("#B0B0B0")
        };
        infoStack.Children.Add(descriptionLabel);

        // Barra de progreso
        var progressBar = new ProgressBar
        {
            Progress = Math.Min(1.0, (double)achievement.Progress / achievement.Target),
            ProgressColor = achievement.IsUnlocked ?
                           GetAchievementColor(achievement.Type) :
                           Color.FromArgb("#606060"),
            HeightRequest = 4
        };
        infoStack.Children.Add(progressBar);

        var progressLabel = new Label
        {
            Text = $"{achievement.Progress}/{achievement.Target}",
            FontSize = 12,
            TextColor = Color.FromArgb("#808080")
        };
        infoStack.Children.Add(progressLabel);

        grid.Children.Add(infoStack);
        Grid.SetColumn(infoStack, 1);

        // Estado
        if (achievement.IsUnlocked)
        {
            var checkIcon = new Label
            {
                Text = "✓",
                FontSize = 24,
                TextColor = GetAchievementColor(achievement.Type),
                VerticalOptions = LayoutOptions.Center
            };
            grid.Children.Add(checkIcon);
            Grid.SetColumn(checkIcon, 2);
        }
        else
        {
            var lockIcon = new Label
            {
                Text = "🔒",
                FontSize = 20,
                VerticalOptions = LayoutOptions.Center
            };
            grid.Children.Add(lockIcon);
            Grid.SetColumn(lockIcon, 2);
        }

        frame.Content = grid;
        return frame;
    }

    /// <summary>
    /// Obtiene el color según el tipo de logro
    /// </summary>
    private Color GetAchievementColor(AchievementType type)
    {
        return type switch
        {
            AchievementType.ChaptersRead => Color.FromArgb("#4CAF50"),
            AchievementType.NovelsCompleted => Color.FromArgb("#2196F3"),
            AchievementType.ReadingStreak => Color.FromArgb("#FF9800"),
            AchievementType.GenreExplorer => Color.FromArgb("#9C27B0"),
            AchievementType.Reviews => Color.FromArgb("#E91E63"),
            AchievementType.Categories => Color.FromArgb("#00BCD4"),
            _ => Color.FromArgb("#607D8B")
        };
    }
}