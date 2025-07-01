using Microsoft.Data.SqlClient;
using NovelBook.Models;

namespace NovelBook.Services;

/// <summary>
/// Servicio para manejar el historial de lectura del usuario
/// Gestiona tanto el historial general como el progreso específico de cada capítulo
/// </summary>
public class HistoryService
{
    private readonly DatabaseService _database;

    public HistoryService(DatabaseService database)
    {
        _database = database;
    }

    /// <summary>
    /// Obtiene el historial completo de lectura del usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="limit">Límite de registros a obtener (0 = sin límite)</param>
    /// <returns>Lista de elementos del historial ordenados por fecha</returns>
    public async Task<List<ReadingHistoryItem>> GetUserHistoryAsync(int userId, int limit = 0)
    {
        var history = new List<ReadingHistoryItem>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Query que obtiene el historial con información de novelas y capítulos
            var query = @"SELECT TOP (@limit) 
                            rh.id,
                            rh.user_id,
                            rh.novel_id,
                            rh.chapter_id,
                            rh.reading_progress,
                            rh.reading_time,
                            rh.read_at,
                            rh.is_completed,
                            rh.last_position,
                            n.title as novel_title,
                            n.author as novel_author,
                            n.cover_image as novel_cover,
                            c.chapter_number,
                            c.title as chapter_title
                         FROM reading_history rh
                         INNER JOIN novels n ON rh.novel_id = n.id
                         INNER JOIN chapters c ON rh.chapter_id = c.id
                         WHERE rh.user_id = @userId
                         ORDER BY rh.read_at DESC";

            if (limit == 0)
            {
                // Si no hay límite, quitar TOP
                query = query.Replace("TOP (@limit) ", "");
            }

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);
            if (limit > 0)
            {
                command.Parameters.AddWithValue("@limit", limit);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                history.Add(new ReadingHistoryItem
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                    NovelId = reader.GetInt32(reader.GetOrdinal("novel_id")),
                    ChapterId = reader.GetInt32(reader.GetOrdinal("chapter_id")),
                    ReadingProgress = reader.GetDecimal(reader.GetOrdinal("reading_progress")),
                    ReadingTime = reader.GetInt32(reader.GetOrdinal("reading_time")),
                    ReadAt = reader.GetDateTime(reader.GetOrdinal("read_at")),
                    IsCompleted = reader.GetBoolean(reader.GetOrdinal("is_completed")),
                    LastPosition = reader.GetInt32(reader.GetOrdinal("last_position")),
                    NovelTitle = reader.GetString(reader.GetOrdinal("novel_title")),
                    NovelAuthor = reader.IsDBNull(reader.GetOrdinal("novel_author")) ?
                                  "" : reader.GetString(reader.GetOrdinal("novel_author")),
                    NovelCover = reader.IsDBNull(reader.GetOrdinal("novel_cover")) ?
                                 "" : reader.GetString(reader.GetOrdinal("novel_cover")),
                    ChapterNumber = reader.GetInt32(reader.GetOrdinal("chapter_number")),
                    ChapterTitle = reader.GetString(reader.GetOrdinal("chapter_title"))
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo historial: {ex.Message}");
        }

        return history;
    }

    /// <summary>
    /// Obtiene el historial agrupado por novela
    /// </summary>
    public async Task<List<NovelHistoryGroup>> GetHistoryGroupedByNovelAsync(int userId)
    {
        var groups = new List<NovelHistoryGroup>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Query para obtener novelas con actividad reciente
            var query = @"SELECT 
                            n.id as novel_id,
                            n.title,
                            n.author,
                            n.cover_image,
                            MAX(rh.read_at) as last_read,
                            COUNT(DISTINCT rh.chapter_id) as chapters_read,
                            SUM(rh.reading_time) as total_reading_time
                         FROM reading_history rh
                         INNER JOIN novels n ON rh.novel_id = n.id
                         WHERE rh.user_id = @userId
                         GROUP BY n.id, n.title, n.author, n.cover_image
                         ORDER BY MAX(rh.read_at) DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                groups.Add(new NovelHistoryGroup
                {
                    NovelId = reader.GetInt32(reader.GetOrdinal("novel_id")),
                    NovelTitle = reader.GetString(reader.GetOrdinal("title")),
                    NovelAuthor = reader.IsDBNull(reader.GetOrdinal("author")) ?
                                  "" : reader.GetString(reader.GetOrdinal("author")),
                    NovelCover = reader.IsDBNull(reader.GetOrdinal("cover_image")) ?
                                 "" : reader.GetString(reader.GetOrdinal("cover_image")),
                    LastRead = reader.GetDateTime(reader.GetOrdinal("last_read")),
                    ChaptersRead = reader.GetInt32(reader.GetOrdinal("chapters_read")),
                    TotalReadingTime = reader.GetInt32(reader.GetOrdinal("total_reading_time"))
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo historial agrupado: {ex.Message}");
        }

        return groups;
    }

    /// <summary>
    /// Obtiene estadísticas del historial de lectura
    /// </summary>
    public async Task<ReadingStats> GetReadingStatsAsync(int userId)
    {
        var stats = new ReadingStats();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Query para obtener estadísticas generales
            var query = @"SELECT 
                            COUNT(DISTINCT chapter_id) as total_chapters,
                            COUNT(DISTINCT novel_id) as total_novels,
                            ISNULL(SUM(reading_time), 0) as total_time,
                            COUNT(DISTINCT CAST(read_at as DATE)) as reading_days
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
                stats.ReadingDays = reader.GetInt32(reader.GetOrdinal("reading_days"));
            }

            // Calcular racha de lectura
            stats.CurrentStreak = await CalculateReadingStreakAsync(userId, connection);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo estadísticas: {ex.Message}");
        }

        return stats;
    }

    /// <summary>
    /// Calcula la racha de lectura actual del usuario
    /// </summary>
    private async Task<int> CalculateReadingStreakAsync(int userId, SqlConnection connection)
    {
        try
        {
            // Query para calcular días consecutivos de lectura
            var query = @"WITH ReadingDates AS (
                            SELECT DISTINCT CAST(read_at as DATE) as read_date
                            FROM reading_history
                            WHERE user_id = @userId
                        ),
                        DateGroups AS (
                            SELECT read_date,
                                   DATEADD(day, -ROW_NUMBER() OVER (ORDER BY read_date), read_date) as grp
                            FROM ReadingDates
                        ),
                        Streaks AS (
                            SELECT MIN(read_date) as start_date,
                                   MAX(read_date) as end_date,
                                   COUNT(*) as streak_length
                            FROM DateGroups
                            GROUP BY grp
                        )
                        SELECT TOP 1 streak_length
                        FROM Streaks
                        WHERE end_date >= DATEADD(day, -1, CAST(GETDATE() as DATE))
                        ORDER BY end_date DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);

            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Elimina un elemento del historial
    /// </summary>
    public async Task<bool> DeleteHistoryItemAsync(int historyId, int userId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"DELETE FROM reading_history 
                         WHERE id = @id AND user_id = @userId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", historyId);
            command.Parameters.AddWithValue("@userId", userId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error eliminando del historial: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Limpia todo el historial del usuario
    /// </summary>
    public async Task<bool> ClearAllHistoryAsync(int userId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = "DELETE FROM reading_history WHERE user_id = @userId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error limpiando historial: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtiene el historial de un período específico
    /// </summary>
    public async Task<List<ReadingHistoryItem>> GetHistoryByDateRangeAsync(
        int userId, DateTime startDate, DateTime endDate)
    {
        var history = new List<ReadingHistoryItem>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT 
                            rh.*,
                            n.title as novel_title,
                            n.author as novel_author,
                            n.cover_image as novel_cover,
                            c.chapter_number,
                            c.title as chapter_title
                         FROM reading_history rh
                         INNER JOIN novels n ON rh.novel_id = n.id
                         INNER JOIN chapters c ON rh.chapter_id = c.id
                         WHERE rh.user_id = @userId
                           AND rh.read_at >= @startDate
                           AND rh.read_at <= @endDate
                         ORDER BY rh.read_at DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@startDate", startDate);
            command.Parameters.AddWithValue("@endDate", endDate);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                // Misma lógica de mapeo que GetUserHistoryAsync
                history.Add(ReadHistoryItemFromReader(reader));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo historial por fechas: {ex.Message}");
        }

        return history;
    }

    /// <summary>
    /// Método auxiliar para mapear un reader a ReadingHistoryItem
    /// </summary>
    private ReadingHistoryItem ReadHistoryItemFromReader(SqlDataReader reader)
    {
        return new ReadingHistoryItem
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
            NovelId = reader.GetInt32(reader.GetOrdinal("novel_id")),
            ChapterId = reader.GetInt32(reader.GetOrdinal("chapter_id")),
            ReadingProgress = reader.GetDecimal(reader.GetOrdinal("reading_progress")),
            ReadingTime = reader.GetInt32(reader.GetOrdinal("reading_time")),
            ReadAt = reader.GetDateTime(reader.GetOrdinal("read_at")),
            IsCompleted = reader.GetBoolean(reader.GetOrdinal("is_completed")),
            LastPosition = reader.GetInt32(reader.GetOrdinal("last_position")),
            NovelTitle = reader.GetString(reader.GetOrdinal("novel_title")),
            NovelAuthor = reader.IsDBNull(reader.GetOrdinal("novel_author")) ?
                          "" : reader.GetString(reader.GetOrdinal("novel_author")),
            NovelCover = reader.IsDBNull(reader.GetOrdinal("novel_cover")) ?
                         "" : reader.GetString(reader.GetOrdinal("novel_cover")),
            ChapterNumber = reader.GetInt32(reader.GetOrdinal("chapter_number")),
            ChapterTitle = reader.GetString(reader.GetOrdinal("chapter_title"))
        };
    }
}