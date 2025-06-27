using NovelBook.Models;
using Microsoft.Data.SqlClient;

namespace NovelBook.Services;

/// <summary>
/// Servicio para manejar operaciones relacionadas con capítulos
/// </summary>
public class ChapterService
{
    private readonly DatabaseService _database;

    public ChapterService(DatabaseService database)
    {
        _database = database;
    }

    /// <summary>
    /// Obtiene un capítulo específico por su ID
    /// </summary>
    public async Task<Chapter> GetChapterAsync(int chapterId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT c.*, n.title as novel_title 
                         FROM chapters c
                         INNER JOIN novels n ON c.novel_id = n.id
                         WHERE c.id = @id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", chapterId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Chapter
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    NovelId = reader.GetInt32(reader.GetOrdinal("novel_id")),
                    ChapterNumber = reader.GetInt32(reader.GetOrdinal("chapter_number")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Content = reader.GetString(reader.GetOrdinal("content")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo capítulo: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Obtiene todos los capítulos de una novela
    /// </summary>
    public async Task<List<Chapter>> GetChaptersForNovelAsync(int novelId)
    {
        var chapters = new List<Chapter>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT * FROM chapters 
                         WHERE novel_id = @novelId 
                         ORDER BY chapter_number";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@novelId", novelId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                chapters.Add(new Chapter
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    NovelId = reader.GetInt32(reader.GetOrdinal("novel_id")),
                    ChapterNumber = reader.GetInt32(reader.GetOrdinal("chapter_number")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Content = reader.GetString(reader.GetOrdinal("content")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo capítulos: {ex.Message}");
        }

        return chapters;
    }

    /// <summary>
    /// Guarda el progreso de lectura del usuario
    /// </summary>
    public async Task SaveReadingProgressAsync(int userId, int chapterId, decimal progress, int lastPosition, bool isCompleted)
    {
        // Verificar modo incógnito
        if (Preferences.Get("IncognitoMode", false))
        {
            return; // No guardar nada en modo incógnito
        }

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"IF EXISTS (SELECT 1 FROM reading_progress WHERE user_id = @userId AND chapter_id = @chapterId)
                         UPDATE reading_progress 
                         SET progress = @progress, 
                             last_position = @lastPosition, 
                             is_completed = @isCompleted,
                             last_read_at = GETDATE()
                         WHERE user_id = @userId AND chapter_id = @chapterId
                         ELSE
                         INSERT INTO reading_progress (user_id, chapter_id, progress, last_position, is_completed)
                         VALUES (@userId, @chapterId, @progress, @lastPosition, @isCompleted)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@chapterId", chapterId);
            command.Parameters.AddWithValue("@progress", progress);
            command.Parameters.AddWithValue("@lastPosition", lastPosition);
            command.Parameters.AddWithValue("@isCompleted", isCompleted);

            await command.ExecuteNonQueryAsync();

            // También actualizar el historial de lectura
            await UpdateReadingHistoryAsync(userId, chapterId, isCompleted);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error guardando progreso: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene el progreso de lectura guardado
    /// </summary>
    public async Task<ReadingProgress> GetReadingProgressAsync(int userId, int chapterId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT * FROM reading_progress 
                         WHERE user_id = @userId AND chapter_id = @chapterId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@chapterId", chapterId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ReadingProgress
                {
                    Progress = reader.GetDecimal(reader.GetOrdinal("progress")),
                    LastPosition = reader.GetInt32(reader.GetOrdinal("last_position")),
                    IsCompleted = reader.GetBoolean(reader.GetOrdinal("is_completed")),
                    LastReadAt = reader.GetDateTime(reader.GetOrdinal("last_read_at"))
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo progreso: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Actualiza el historial de lectura
    /// </summary>
    private async Task UpdateReadingHistoryAsync(int userId, int chapterId, bool isCompleted)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Obtener novel_id del capítulo
            var getNovelQuery = "SELECT novel_id FROM chapters WHERE id = @chapterId";
            using var getNovelCmd = new SqlCommand(getNovelQuery, connection);
            getNovelCmd.Parameters.AddWithValue("@chapterId", chapterId);
            var novelId = (int)await getNovelCmd.ExecuteScalarAsync();

            // Insertar o actualizar historial
            var query = @"IF NOT EXISTS (SELECT 1 FROM reading_history 
                                        WHERE user_id = @userId AND chapter_id = @chapterId)
                         INSERT INTO reading_history (user_id, novel_id, chapter_id, is_completed)
                         VALUES (@userId, @novelId, @chapterId, @isCompleted)
                         ELSE
                         UPDATE reading_history 
                         SET is_completed = @isCompleted, read_at = GETDATE()
                         WHERE user_id = @userId AND chapter_id = @chapterId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@novelId", novelId);
            command.Parameters.AddWithValue("@chapterId", chapterId);
            command.Parameters.AddWithValue("@isCompleted", isCompleted);

            await command.ExecuteNonQueryAsync();

            // Actualizar el último capítulo leído en user_library
            if (isCompleted)
            {
                var updateLibraryQuery = @"UPDATE user_library 
                                         SET last_read_chapter = @chapterNumber
                                         WHERE user_id = @userId AND novel_id = @novelId";

                using var updateCmd = new SqlCommand(updateLibraryQuery, connection);
                updateCmd.Parameters.AddWithValue("@userId", userId);
                updateCmd.Parameters.AddWithValue("@novelId", novelId);
                updateCmd.Parameters.AddWithValue("@chapterNumber", await GetChapterNumberAsync(chapterId));

                await updateCmd.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error actualizando historial: {ex.Message}");
        }
    }

    private async Task<int> GetChapterNumberAsync(int chapterId)
    {
        using var connection = _database.GetConnection();
        await connection.OpenAsync();

        var query = "SELECT chapter_number FROM chapters WHERE id = @id";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@id", chapterId);

        return (int)await command.ExecuteScalarAsync();
    }

    /// <summary>
    /// Obtiene el último capítulo leído de una novela
    /// </summary>
    public async Task<int?> GetLastReadChapterAsync(int userId, int novelId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT TOP 1 c.id 
                         FROM reading_history rh
                         INNER JOIN chapters c ON rh.chapter_id = c.id
                         WHERE rh.user_id = @userId AND rh.novel_id = @novelId
                         ORDER BY rh.read_at DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@novelId", novelId);

            var result = await command.ExecuteScalarAsync();
            return result as int?;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo último capítulo: {ex.Message}");
            return null;
        }
    }
}

/// <summary>
/// Clase para almacenar el progreso de lectura
/// </summary>
public class ReadingProgress
{
    public decimal Progress { get; set; }
    public int LastPosition { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime LastReadAt { get; set; }
}