namespace NovelBook.Models;

/// <summary>
/// Estadísticas extendidas del usuario
/// </summary>
public class ExtendedStats
{
    // Estadísticas básicas de lectura
    public int TotalChaptersRead { get; set; }
    public int TotalNovelsRead { get; set; }
    public int TotalReadingTime { get; set; } // en segundos
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime FirstReadingDate { get; set; }

    // Estadísticas de biblioteca
    public int TotalNovelsInLibrary { get; set; }
    public int NovelsCompleted { get; set; }
    public int NovelsReading { get; set; }
    public int NovelsPlanToRead { get; set; }
    public int TotalFavorites { get; set; }

    // Estadísticas de géneros
    public List<GenreReadingStats> GenreStats { get; set; }
    public string FavoriteGenre { get; set; }

    // Estadísticas de autores
    public List<AuthorReadingStats> AuthorStats { get; set; }
    public string FavoriteAuthor { get; set; }

    // Estadísticas por tiempo
    public List<DailyReadingStats> Last7DaysStats { get; set; }
    public List<MonthlyReadingStats> Last12MonthsStats { get; set; }
    public Dictionary<int, int> HourlyDistribution { get; set; } // Hora del día -> capítulos leídos

    // Estadísticas de categorías personalizadas
    public int TotalCategories { get; set; }
    public int NovelsInCategories { get; set; }

    // Estadísticas de reseñas
    public int TotalReviewsWritten { get; set; }
    public decimal AverageRating { get; set; }

    // Logros y metas
    public List<Achievement> UnlockedAchievements { get; set; }
    public ReadingGoal CurrentGoal { get; set; }

    // Propiedades calculadas
    public string FormattedTotalTime => FormatTime(TotalReadingTime);
    public double AverageChaptersPerDay => GetAverageChaptersPerDay();
    public double AverageReadingTimePerDay => GetAverageTimePerDay();
    public double CompletionRate => TotalNovelsInLibrary > 0 ?
        (double)NovelsCompleted / TotalNovelsInLibrary * 100 : 0;

    /// <summary>
    /// Constructor
    /// </summary>
    public ExtendedStats()
    {
        GenreStats = new List<GenreReadingStats>();
        AuthorStats = new List<AuthorReadingStats>();
        Last7DaysStats = new List<DailyReadingStats>();
        Last12MonthsStats = new List<MonthlyReadingStats>();
        HourlyDistribution = new Dictionary<int, int>();
        UnlockedAchievements = new List<Achievement>();
    }

    /// <summary>
    /// Formatea el tiempo en un formato legible
    /// </summary>
    private string FormatTime(int seconds)
    {
        if (seconds < 3600)
            return $"{seconds / 60} min";
        else if (seconds < 86400)
            return $"{seconds / 3600}h {(seconds % 3600) / 60}m";
        else
        {
            var days = seconds / 86400;
            var hours = (seconds % 86400) / 3600;
            return $"{days}d {hours}h";
        }
    }

    /// <summary>
    /// Calcula el promedio de capítulos por día
    /// </summary>
    private double GetAverageChaptersPerDay()
    {
        if (FirstReadingDate == default) return 0;
        var daysSinceStart = (DateTime.Now - FirstReadingDate).TotalDays;
        return daysSinceStart > 0 ? TotalChaptersRead / daysSinceStart : 0;
    }

    /// <summary>
    /// Calcula el tiempo promedio por día
    /// </summary>
    private double GetAverageTimePerDay()
    {
        if (FirstReadingDate == default) return 0;
        var daysSinceStart = (DateTime.Now - FirstReadingDate).TotalDays;
        return daysSinceStart > 0 ? TotalReadingTime / daysSinceStart : 0;
    }
}

/// <summary>
/// Estadísticas de lectura por género
/// </summary>
public class GenreReadingStats
{
    public int GenreId { get; set; }
    public string GenreName { get; set; }
    public int ChaptersRead { get; set; }
    public int NovelsRead { get; set; }
    public int ReadingTime { get; set; }
    public double Percentage { get; set; } // Porcentaje del total de lectura
}

/// <summary>
/// Estadísticas de lectura por autor
/// </summary>
public class AuthorReadingStats
{
    public string AuthorName { get; set; }
    public int ChaptersRead { get; set; }
    public int NovelsRead { get; set; }
    public int ReadingTime { get; set; }
    public decimal AverageRating { get; set; }
}

/// <summary>
/// Estadísticas diarias de lectura
/// </summary>
public class DailyReadingStats
{
    public DateTime Date { get; set; }
    public int ChaptersRead { get; set; }
    public int ReadingTime { get; set; }
    public string DayName => Date.ToString("ddd");
    public string ShortDate => Date.ToString("dd/MM");
}

/// <summary>
/// Estadísticas mensuales de lectura
/// </summary>
public class MonthlyReadingStats
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int ChaptersRead { get; set; }
    public int NovelsStarted { get; set; }
    public int NovelsCompleted { get; set; }
    public int ReadingTime { get; set; }
    public string MonthName => new DateTime(Year, Month, 1).ToString("MMM");
}

/// <summary>
/// Logro desbloqueado
/// </summary>
public class Achievement
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public DateTime UnlockedAt { get; set; }
    public AchievementType Type { get; set; }
    public int Progress { get; set; }
    public int Target { get; set; }
    public bool IsUnlocked => Progress >= Target;
}

/// <summary>
/// Tipo de logro
/// </summary>
public enum AchievementType
{
    ChaptersRead,
    NovelsCompleted,
    ReadingStreak,
    GenreExplorer,
    TimeSpent,
    Reviews,
    Categories
}

/// <summary>
/// Meta de lectura
/// </summary>
public class ReadingGoal
{
    public int Id { get; set; }
    public GoalType Type { get; set; }
    public int Target { get; set; }
    public int Current { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public double Progress => Target > 0 ? (double)Current / Target * 100 : 0;
    public int DaysRemaining => (EndDate - DateTime.Now).Days;
    public bool IsCompleted => Current >= Target;
    public bool IsActive => DateTime.Now >= StartDate && DateTime.Now <= EndDate;
}

/// <summary>
/// Tipo de meta
/// </summary>
public enum GoalType
{
    ChaptersPerDay,
    ChaptersPerWeek,
    ChaptersPerMonth,
    NovelsPerMonth,
    ReadingTimePerDay
}