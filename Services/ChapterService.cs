using Microsoft.Data.SqlClient;
using NovelBook.Models;
using System.Data;

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
    /// Guarda el progreso de lectura con actualización mejorada
    /// </summary>
    public async Task SaveReadingProgressAsync(int userId, int chapterId, decimal progress, int lastPosition, bool isCompleted)
    {
        System.Diagnostics.Debug.WriteLine($"=== GUARDANDO PROGRESO ===");
        System.Diagnostics.Debug.WriteLine($"UserId: {userId}, ChapterId: {chapterId}");
        System.Diagnostics.Debug.WriteLine($"Progress: {progress}%, IsCompleted: {isCompleted}");
        System.Diagnostics.Debug.WriteLine($"=========================");

        // Verificar si está en modo incógnito
        if (Preferences.Get("IncognitoMode", false)) return;

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Obtener info del capítulo
                var chapterInfo = await GetChapterInfoAsync(chapterId, connection, transaction);
                if (chapterInfo == null)
                {
                    await transaction.RollbackAsync();
                    return;
                }

                // 2. Guardar/actualizar progreso
                await SaveProgressRecordAsync(userId, chapterId, progress, lastPosition, isCompleted, connection, transaction);

                // 3. Si está completado, actualizar user_library
                if (isCompleted)
                {
                    await UpdateLibraryProgressAsync(userId, chapterInfo.Value.NovelId, chapterInfo.Value.ChapterNumber, connection, transaction);
                }

                // 4. Actualizar historial
                await UpdateHistoryAsync(userId, chapterId, chapterInfo.Value.NovelId, isCompleted, connection, transaction);

                await transaction.CommitAsync();

                System.Diagnostics.Debug.WriteLine($"Progreso guardado: Cap {chapterInfo.Value.ChapterNumber}, Completado: {isCompleted}");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error guardando progreso: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene información del capítulo
    /// </summary>
    private async Task<(int NovelId, int ChapterNumber)?> GetChapterInfoAsync(int chapterId, SqlConnection connection, SqlTransaction transaction)
    {
        var query = "SELECT novel_id, chapter_number FROM chapters WHERE id = @id";
        using var command = new SqlCommand(query, connection, transaction);
        command.Parameters.AddWithValue("@id", chapterId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return (reader.GetInt32(0), reader.GetInt32(1));
        }
        return null;
    }

    /// <summary>
    /// Guarda el registro de progreso
    /// </summary>
    private async Task SaveProgressRecordAsync(int userId, int chapterId, decimal progress, int lastPosition, bool isCompleted, SqlConnection connection, SqlTransaction transaction)
    {
        var query = @"MERGE reading_progress AS target
                      USING (SELECT @userId AS user_id, @chapterId AS chapter_id) AS source
                      ON target.user_id = source.user_id AND target.chapter_id = source.chapter_id
                      WHEN MATCHED THEN
                          UPDATE SET progress = @progress, 
                                     last_position = @lastPosition, 
                                     is_completed = @isCompleted,
                                     last_read_at = GETDATE()
                      WHEN NOT MATCHED THEN
                          INSERT (user_id, chapter_id, progress, last_position, is_completed)
                          VALUES (@userId, @chapterId, @progress, @lastPosition, @isCompleted);";

        using var command = new SqlCommand(query, connection, transaction);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@chapterId", chapterId);
        command.Parameters.AddWithValue("@progress", progress);
        command.Parameters.AddWithValue("@lastPosition", lastPosition);
        command.Parameters.AddWithValue("@isCompleted", isCompleted);

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Actualiza el progreso en la biblioteca del usuario
    /// </summary>
    private async Task UpdateLibraryProgressAsync(int userId, int novelId, int chapterNumber, SqlConnection connection, SqlTransaction transaction)
    {
        var query = @"UPDATE user_library 
                      SET last_read_chapter = CASE 
                          WHEN last_read_chapter < @chapterNumber THEN @chapterNumber 
                          ELSE last_read_chapter 
                      END
                      WHERE user_id = @userId AND novel_id = @novelId";

        using var command = new SqlCommand(query, connection, transaction);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@novelId", novelId);
        command.Parameters.AddWithValue("@chapterNumber", chapterNumber);

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Actualiza el historial de lectura
    /// </summary>
    private async Task UpdateHistoryAsync(int userId, int chapterId, int novelId, bool isCompleted, SqlConnection connection, SqlTransaction transaction)
    {
        var query = @"MERGE reading_history AS target
                      USING (SELECT @userId AS user_id, @chapterId AS chapter_id) AS source
                      ON target.user_id = source.user_id AND target.chapter_id = source.chapter_id
                      WHEN MATCHED THEN
                          UPDATE SET is_completed = @isCompleted, 
                                     read_at = GETDATE()
                      WHEN NOT MATCHED THEN
                          INSERT (user_id, novel_id, chapter_id, is_completed)
                          VALUES (@userId, @novelId, @chapterId, @isCompleted);";

        using var command = new SqlCommand(query, connection, transaction);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@novelId", novelId);
        command.Parameters.AddWithValue("@chapterId", chapterId);
        command.Parameters.AddWithValue("@isCompleted", isCompleted);

        await command.ExecuteNonQueryAsync();
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

    /// <summary>
    /// Obtiene el número real de capítulos leídos por el usuario
    /// </summary>
    public async Task<int> GetCompletedChaptersCountAsync(int userId, int novelId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT COUNT(DISTINCT c.chapter_number) 
                         FROM reading_progress rp
                         INNER JOIN chapters c ON rp.chapter_id = c.id
                         WHERE rp.user_id = @userId 
                         AND c.novel_id = @novelId 
                         AND rp.is_completed = 1";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@novelId", novelId);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo capítulos completados: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Sincroniza el contador de last_read_chapter con los capítulos realmente leídos
    /// </summary>
    public async Task SyncUserLibraryProgressAsync(int userId, int novelId)
    {
        try
        {
            var completedCount = await GetCompletedChaptersCountAsync(userId, novelId);

            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var updateQuery = @"UPDATE user_library 
                              SET last_read_chapter = @completedCount
                              WHERE user_id = @userId AND novel_id = @novelId";

            using var command = new SqlCommand(updateQuery, connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@novelId", novelId);
            command.Parameters.AddWithValue("@completedCount", completedCount);

            await command.ExecuteNonQueryAsync();

            System.Diagnostics.Debug.WriteLine($"Sincronizado: Usuario {userId}, Novela {novelId}, Capítulos completados: {completedCount}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sincronizando progreso: {ex.Message}");
        }
    }

    // Agregar este método en la clase ChapterService

    /// <summary>
    /// Actualiza el contenido y título de un capítulo existente
    /// </summary>
    /// <param name="chapterId">ID del capítulo a actualizar</param>
    /// <param name="title">Nuevo título del capítulo</param>
    /// <param name="content">Nuevo contenido del capítulo</param>
    /// <returns>true si se actualizó correctamente, false en caso contrario</returns>
    public async Task<bool> UpdateChapterAsync(int chapterId, string title, string content)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Actualizar el capítulo
            var updateQuery = @"UPDATE chapters 
                           SET title = @title, 
                               content = @content
                           WHERE id = @id";

            using var command = new SqlCommand(updateQuery, connection);
            command.Parameters.AddWithValue("@id", chapterId);
            command.Parameters.AddWithValue("@title", title);
            command.Parameters.AddWithValue("@content", content ?? "");

            var rowsAffected = await command.ExecuteNonQueryAsync();

            // También actualizar la fecha de modificación de la novela
            if (rowsAffected > 0)
            {
                var updateNovelQuery = @"UPDATE novels 
                                   SET updated_at = GETDATE() 
                                   WHERE id = (SELECT novel_id FROM chapters WHERE id = @chapterId)";

                using var novelCommand = new SqlCommand(updateNovelQuery, connection);
                novelCommand.Parameters.AddWithValue("@chapterId", chapterId);
                await novelCommand.ExecuteNonQueryAsync();
            }

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error actualizando capítulo: {ex.Message}");
            return false;
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