using Microsoft.Data.SqlClient;
using NovelBook.Models;

namespace NovelBook.Services;

/// <summary>
/// Servicio para obtener estadísticas detalladas del usuario
/// </summary>
public class StatsService
{
    private readonly DatabaseService _database;

    public StatsService(DatabaseService database)
    {
        _database = database;
    }

    /// <summary>
    /// Obtiene todas las estadísticas extendidas del usuario
    /// </summary>
    public async Task<ExtendedStats> GetExtendedStatsAsync()
    {
        if (AuthService.CurrentUser == null)
            return new ExtendedStats();

        var stats = new ExtendedStats();
        var userId = AuthService.CurrentUser.Id;

        try
        {
            // Usar conexiones separadas para cada operación
            await LoadBasicStats(stats, userId);
            await LoadLibraryStats(stats, userId);
            await LoadGenreStats(stats, userId);
            await LoadAuthorStats(stats, userId);
            await LoadTimeStats(stats, userId);
            await LoadCategoryStats(stats, userId);
            await LoadReviewStats(stats, userId);
            await LoadAchievements(stats, userId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo estadísticas: {ex.Message}");
        }

        return stats;
    }

    /// <summary>
    /// Carga las estadísticas básicas de lectura
    /// </summary>
    private async Task LoadBasicStats(ExtendedStats stats, int userId)
    {
        using var connection = _database.GetConnection();
        await connection.OpenAsync();

        var query = @"SELECT 
                        COUNT(DISTINCT chapter_id) as total_chapters,
                        COUNT(DISTINCT novel_id) as total_novels,
                        ISNULL(SUM(reading_time), 0) as total_time,
                        MIN(read_at) as first_reading_date
                     FROM reading_history
                     WHERE user_id = @userId";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            stats.TotalChaptersRead = reader.GetInt32(reader.GetOrdinal("total_chapters"));
            stats.TotalNovelsRead = reader.GetInt32(reader.GetOrdinal("total_novels"));
            stats.TotalReadingTime = reader.GetInt32(reader.GetOrdinal("total_time"));

            if (!reader.IsDBNull(reader.GetOrdinal("first_reading_date")))
                stats.FirstReadingDate = reader.GetDateTime(reader.GetOrdinal("first_reading_date"));
        }
        reader.Close();

        // Calcular racha actual
        stats.CurrentStreak = await CalculateCurrentStreak(userId, connection);
        stats.LongestStreak = await CalculateLongestStreak(userId, connection);
    }

    /// <summary>
    /// Carga las estadísticas de la biblioteca
    /// </summary>
    private async Task LoadLibraryStats(ExtendedStats stats, int userId)
    {
        using var connection = _database.GetConnection();
        await connection.OpenAsync();

        var query = @"SELECT 
                        COUNT(*) as total_novels,
                        SUM(CASE WHEN reading_status = 'completed' THEN 1 ELSE 0 END) as completed,
                        SUM(CASE WHEN reading_status = 'reading' THEN 1 ELSE 0 END) as reading,
                        SUM(CASE WHEN reading_status = 'plan_to_read' THEN 1 ELSE 0 END) as plan_to_read,
                        SUM(CASE WHEN reading_status = 'paused' THEN 1 ELSE 0 END) as paused,
                        SUM(CASE WHEN reading_status = 'dropped' THEN 1 ELSE 0 END) as dropped,
                        SUM(CASE WHEN is_favorite = 1 THEN 1 ELSE 0 END) as favorites
                     FROM user_library
                     WHERE user_id = @userId";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            stats.TotalNovelsInLibrary = reader.GetInt32(reader.GetOrdinal("total_novels"));
            stats.NovelsCompleted = reader.GetInt32(reader.GetOrdinal("completed"));
            stats.NovelsReading = reader.GetInt32(reader.GetOrdinal("reading"));
            stats.NovelsPlanToRead = reader.GetInt32(reader.GetOrdinal("plan_to_read"));

            // Si quieres agregar paused y dropped al modelo, puedes hacerlo
            // Por ahora, los incluiremos en "plan_to_read" o crear nuevas propiedades
            var paused = reader.GetInt32(reader.GetOrdinal("paused"));
            var dropped = reader.GetInt32(reader.GetOrdinal("dropped"));

            // Ajustar el total si hay otros estados
            // stats.NovelsPaused = paused;
            // stats.NovelsDropped = dropped;

            stats.TotalFavorites = reader.GetInt32(reader.GetOrdinal("favorites"));
        }
    }

    /// <summary>
    /// Carga las estadísticas por género
    /// </summary>
    private async Task LoadGenreStats(ExtendedStats stats, int userId)
    {
        using var connection = _database.GetConnection();
        await connection.OpenAsync();

        var query = @"SELECT TOP 5
                        g.id,
                        g.name,
                        COUNT(DISTINCT rh.chapter_id) as chapters_read,
                        COUNT(DISTINCT rh.novel_id) as novels_read,
                        ISNULL(SUM(rh.reading_time), 0) as reading_time
                     FROM genres g
                     INNER JOIN novel_genres ng ON g.id = ng.genre_id
                     INNER JOIN reading_history rh ON ng.novel_id = rh.novel_id
                     WHERE rh.user_id = @userId
                     GROUP BY g.id, g.name
                     ORDER BY chapters_read DESC";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = await command.ExecuteReaderAsync();
        var totalChapters = stats.TotalChaptersRead > 0 ? stats.TotalChaptersRead : 1;

        while (await reader.ReadAsync())
        {
            var genreStat = new GenreReadingStats
            {
                GenreId = reader.GetInt32(reader.GetOrdinal("id")),
                GenreName = reader.GetString(reader.GetOrdinal("name")),
                ChaptersRead = reader.GetInt32(reader.GetOrdinal("chapters_read")),
                NovelsRead = reader.GetInt32(reader.GetOrdinal("novels_read")),
                ReadingTime = reader.GetInt32(reader.GetOrdinal("reading_time"))
            };

            genreStat.Percentage = (double)genreStat.ChaptersRead / totalChapters * 100;
            stats.GenreStats.Add(genreStat);
        }

        // Establecer género favorito
        if (stats.GenreStats.Any())
        {
            stats.FavoriteGenre = stats.GenreStats.First().GenreName;
        }
    }

    /// <summary>
    /// Carga las estadísticas por autor
    /// </summary>
    private async Task LoadAuthorStats(ExtendedStats stats, int userId)
    {
        using var connection = _database.GetConnection();
        await connection.OpenAsync();

        var query = @"SELECT TOP 5
                        n.author,
                        COUNT(DISTINCT rh.chapter_id) as chapters_read,
                        COUNT(DISTINCT rh.novel_id) as novels_read,
                        ISNULL(SUM(rh.reading_time), 0) as reading_time,
                        ISNULL(AVG(CAST(r.rating as FLOAT)), 0) as avg_rating
                     FROM novels n
                     INNER JOIN reading_history rh ON n.id = rh.novel_id
                     LEFT JOIN reviews r ON n.id = r.novel_id AND r.user_id = @userId
                     WHERE rh.user_id = @userId AND n.author IS NOT NULL
                     GROUP BY n.author
                     ORDER BY chapters_read DESC";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var authorStat = new AuthorReadingStats
            {
                AuthorName = reader.GetString(reader.GetOrdinal("author")),
                ChaptersRead = reader.GetInt32(reader.GetOrdinal("chapters_read")),
                NovelsRead = reader.GetInt32(reader.GetOrdinal("novels_read")),
                ReadingTime = reader.GetInt32(reader.GetOrdinal("reading_time")),
                AverageRating = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("avg_rating")))
            };

            stats.AuthorStats.Add(authorStat);
        }

        // Establecer autor favorito
        if (stats.AuthorStats.Any())
        {
            stats.FavoriteAuthor = stats.AuthorStats.First().AuthorName;
        }
    }

    /// <summary>
    /// Carga las estadísticas temporales
    /// </summary>
    private async Task LoadTimeStats(ExtendedStats stats, int userId)
    {
        using var connection = _database.GetConnection();
        await connection.OpenAsync();

        // Últimos 7 días
        var last7DaysQuery = @"SELECT 
                                CAST(read_at as DATE) as date,
                                COUNT(DISTINCT chapter_id) as chapters_read,
                                ISNULL(SUM(reading_time), 0) as reading_time
                             FROM reading_history
                             WHERE user_id = @userId 
                               AND read_at >= DATEADD(day, -7, GETDATE())
                             GROUP BY CAST(read_at as DATE)
                             ORDER BY date";

        using var command = new SqlCommand(last7DaysQuery, connection);
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            stats.Last7DaysStats.Add(new DailyReadingStats
            {
                Date = reader.GetDateTime(reader.GetOrdinal("date")),
                ChaptersRead = reader.GetInt32(reader.GetOrdinal("chapters_read")),
                ReadingTime = reader.GetInt32(reader.GetOrdinal("reading_time"))
            });
        }
        reader.Close();

        // Distribución por hora del día
        await LoadHourlyDistribution(stats, userId, connection);
    }

    /// <summary>
    /// Carga la distribución de lectura por hora del día
    /// </summary>
    private async Task LoadHourlyDistribution(ExtendedStats stats, int userId, SqlConnection connection)
    {
        var query = @"SELECT 
                        DATEPART(hour, read_at) as hour,
                        COUNT(*) as chapters
                     FROM reading_history
                     WHERE user_id = @userId
                     GROUP BY DATEPART(hour, read_at)";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var hour = reader.GetInt32(reader.GetOrdinal("hour"));
            var chapters = reader.GetInt32(reader.GetOrdinal("chapters"));
            stats.HourlyDistribution[hour] = chapters;
        }
    }

    /// <summary>
    /// Carga las estadísticas de categorías
    /// </summary>
    private async Task LoadCategoryStats(ExtendedStats stats, int userId)
    {
        using var connection = _database.GetConnection();
        await connection.OpenAsync();

        var query = @"SELECT 
                        COUNT(DISTINCT uc.id) as total_categories,
                        COUNT(DISTINCT cn.novel_id) as novels_in_categories
                     FROM user_categories uc
                     LEFT JOIN category_novels cn ON uc.id = cn.category_id
                     WHERE uc.user_id = @userId";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            stats.TotalCategories = reader.GetInt32(reader.GetOrdinal("total_categories"));
            stats.NovelsInCategories = reader.GetInt32(reader.GetOrdinal("novels_in_categories"));
        }
    }

    /// <summary>
    /// Carga las estadísticas de reseñas
    /// </summary>
    private async Task LoadReviewStats(ExtendedStats stats, int userId)
    {
        using var connection = _database.GetConnection();
        await connection.OpenAsync();

        var query = @"SELECT 
                        COUNT(*) as total_reviews,
                        ISNULL(AVG(CAST(rating as FLOAT)), 0) as avg_rating
                     FROM reviews
                     WHERE user_id = @userId";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            stats.TotalReviewsWritten = reader.GetInt32(reader.GetOrdinal("total_reviews"));
            stats.AverageRating = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("avg_rating")));
        }
    }

    /// <summary>
    /// Calcula la racha actual de lectura
    /// </summary>
    private async Task<int> CalculateCurrentStreak(int userId, SqlConnection connection)
    {
        try
        {
            // Consulta simplificada para calcular la racha
            var query = @"WITH DatesRead AS (
                            SELECT DISTINCT CAST(read_at as DATE) as read_date
                            FROM reading_history
                            WHERE user_id = @userId
                        ),
                        DatesWithGaps AS (
                            SELECT 
                                read_date,
                                DATEDIFF(day, read_date, GETDATE()) as days_ago,
                                ROW_NUMBER() OVER (ORDER BY read_date DESC) - 1 as expected_days_ago
                            FROM DatesRead
                        )
                        SELECT COUNT(*) as streak
                        FROM DatesWithGaps
                        WHERE days_ago = expected_days_ago";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);

            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error calculando racha actual: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Calcula la racha más larga
    /// </summary>
    private async Task<int> CalculateLongestStreak(int userId, SqlConnection connection)
    {
        try
        {
            // Consulta simplificada para la racha más larga
            var query = @"WITH DatesRead AS (
                            SELECT DISTINCT CAST(read_at as DATE) as read_date
                            FROM reading_history
                            WHERE user_id = @userId
                        ),
                        Streaks AS (
                            SELECT 
                                read_date,
                                DATEADD(day, -ROW_NUMBER() OVER (ORDER BY read_date), read_date) as streak_group
                            FROM DatesRead
                        )
                        SELECT ISNULL(MAX(streak_length), 0) as max_streak
                        FROM (
                            SELECT COUNT(*) as streak_length
                            FROM Streaks
                            GROUP BY streak_group
                        ) as StreakCounts";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);

            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error calculando racha más larga: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Carga los logros del usuario
    /// </summary>
    private async Task LoadAchievements(ExtendedStats stats, int userId)
    {
        // Logros predefinidos - no necesita conexión a BD
        var achievements = new List<Achievement>
        {
            new Achievement
            {
                Id = "first_chapter",
                Name = "Primera Página",
                Description = "Lee tu primer capítulo",
                Icon = "📖",
                Type = AchievementType.ChaptersRead,
                Target = 1,
                Progress = stats.TotalChaptersRead
            },
            new Achievement
            {
                Id = "bookworm",
                Name = "Ratón de Biblioteca",
                Description = "Lee 100 capítulos",
                Icon = "📚",
                Type = AchievementType.ChaptersRead,
                Target = 100,
                Progress = stats.TotalChaptersRead
            },
            new Achievement
            {
                Id = "dedicated_reader",
                Name = "Lector Dedicado",
                Description = "Lee 500 capítulos",
                Icon = "🎓",
                Type = AchievementType.ChaptersRead,
                Target = 500,
                Progress = stats.TotalChaptersRead
            },
            new Achievement
            {
                Id = "first_complete",
                Name = "Primera Victoria",
                Description = "Completa tu primera novela",
                Icon = "🏆",
                Type = AchievementType.NovelsCompleted,
                Target = 1,
                Progress = stats.NovelsCompleted
            },
            new Achievement
            {
                Id = "completionist",
                Name = "Completista",
                Description = "Completa 10 novelas",
                Icon = "👑",
                Type = AchievementType.NovelsCompleted,
                Target = 10,
                Progress = stats.NovelsCompleted
            },
            new Achievement
            {
                Id = "week_streak",
                Name = "Semana Perfecta",
                Description = "Lee durante 7 días seguidos",
                Icon = "🔥",
                Type = AchievementType.ReadingStreak,
                Target = 7,
                Progress = stats.CurrentStreak
            },
            new Achievement
            {
                Id = "month_streak",
                Name = "Mes Imparable",
                Description = "Lee durante 30 días seguidos",
                Icon = "💎",
                Type = AchievementType.ReadingStreak,
                Target = 30,
                Progress = stats.CurrentStreak
            },
            new Achievement
            {
                Id = "genre_explorer",
                Name = "Explorador de Géneros",
                Description = "Lee novelas de 5 géneros diferentes",
                Icon = "🗺️",
                Type = AchievementType.GenreExplorer,
                Target = 5,
                Progress = stats.GenreStats.Count
            },
            new Achievement
            {
                Id = "organizer",
                Name = "Organizador",
                Description = "Crea 5 categorías personalizadas",
                Icon = "📁",
                Type = AchievementType.Categories,
                Target = 5,
                Progress = stats.TotalCategories
            },
            new Achievement
            {
                Id = "critic",
                Name = "Crítico Literario",
                Description = "Escribe 10 reseñas",
                Icon = "✍️",
                Type = AchievementType.Reviews,
                Target = 10,
                Progress = stats.TotalReviewsWritten
            }
        };

        // Filtrar solo los desbloqueados
        stats.UnlockedAchievements = achievements.Where(a => a.IsUnlocked).ToList();
    }
}