using Microsoft.Data.SqlClient;
using NovelBook.Models;
using System.Text.Json;

namespace NovelBook.Services;

/// <summary>
/// Servicio para gestionar descargas de capítulos para lectura offline
/// Este servicio maneja:
/// - Descarga de capítulos individuales o novelas completas
/// - Almacenamiento local de contenido
/// - Gestión de espacio y archivos descargados
/// - Seguimiento del estado de descargas
/// </summary>
public class DownloadService
{
    private readonly DatabaseService _database;
    private readonly string _downloadsDirectory;

    // Constantes para límites
    private const long MAX_STORAGE_BYTES = 1073741824; // 1GB máximo
    private const string DOWNLOADS_FOLDER = "NovelDownloads";

    public DownloadService(DatabaseService database)
    {
        _database = database;

        // Crear directorio de descargas si no existe
        _downloadsDirectory = Path.Combine(FileSystem.AppDataDirectory, DOWNLOADS_FOLDER);
        if (!Directory.Exists(_downloadsDirectory))
        {
            Directory.CreateDirectory(_downloadsDirectory);
        }
    }

    #region Métodos de Descarga

    /// <summary>
    /// Descarga un capítulo específico
    /// </summary>
    public async Task<bool> DownloadChapterAsync(int chapterId)
    {
        try
        {
            // Verificar si ya está descargado
            if (await IsChapterDownloadedAsync(chapterId))
            {
                System.Diagnostics.Debug.WriteLine($"Capítulo {chapterId} ya está descargado");
                return true;
            }

            // Obtener datos del capítulo
            var chapterData = await GetChapterDataAsync(chapterId);
            if (chapterData == null) return false;

            // Verificar espacio disponible
            var estimatedSize = EstimateChapterSize(chapterData.Content);
            if (!await HasEnoughSpaceAsync(estimatedSize))
            {
                throw new Exception("No hay suficiente espacio de almacenamiento");
            }

            // Guardar archivo local
            var fileName = GetChapterFileName(chapterId);
            var filePath = Path.Combine(_downloadsDirectory, fileName);

            // Serializar y guardar
            var json = JsonSerializer.Serialize(chapterData);
            await File.WriteAllTextAsync(filePath, json);

            // Registrar en base de datos
            await RegisterDownloadAsync(chapterId, chapterData.NovelId, new FileInfo(filePath).Length);

            System.Diagnostics.Debug.WriteLine($"Capítulo {chapterId} descargado exitosamente");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error descargando capítulo: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Descarga todos los capítulos de una novela
    /// </summary>
    public async Task<(int downloaded, int failed)> DownloadNovelAsync(int novelId)
    {
        var downloaded = 0;
        var failed = 0;

        try
        {
            // Obtener todos los capítulos de la novela
            var chapters = await GetNovelChaptersAsync(novelId);

            foreach (var chapter in chapters)
            {
                if (await DownloadChapterAsync(chapter.Id))
                {
                    downloaded++;
                }
                else
                {
                    failed++;
                }

                // Pequeña pausa entre descargas para no saturar
                await Task.Delay(100);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error descargando novela: {ex.Message}");
        }

        return (downloaded, failed);
    }

    #endregion

    #region Métodos de Lectura

    /// <summary>
    /// Lee un capítulo descargado
    /// </summary>
    public async Task<Chapter> ReadDownloadedChapterAsync(int chapterId)
    {
        try
        {
            var fileName = GetChapterFileName(chapterId);
            var filePath = Path.Combine(_downloadsDirectory, fileName);

            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<Chapter>(json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error leyendo capítulo descargado: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Verifica si un capítulo está descargado
    /// </summary>
    public async Task<bool> IsChapterDownloadedAsync(int chapterId)
    {
        // Verificar archivo local
        var fileName = GetChapterFileName(chapterId);
        var filePath = Path.Combine(_downloadsDirectory, fileName);

        if (!File.Exists(filePath))
            return false;

        // Verificar registro en BD
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT COUNT(*) FROM downloads 
                         WHERE chapter_id = @chapterId 
                         AND user_id = @userId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@chapterId", chapterId);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser?.Id ?? 0);

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Gestión de Descargas

    /// <summary>
    /// Obtiene todas las descargas del usuario
    /// </summary>
    public async Task<List<DownloadedNovel>> GetDownloadedNovelsAsync()
    {
        var novels = new List<DownloadedNovel>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"
                SELECT 
                    n.id,
                    n.title,
                    n.author,
                    n.cover_image,
                    COUNT(DISTINCT d.chapter_id) as downloaded_chapters,
                    n.chapter_count as total_chapters,
                    SUM(d.file_size) as total_size,
                    MAX(d.downloaded_at) as last_download
                FROM downloads d
                INNER JOIN novels n ON d.novel_id = n.id
                WHERE d.user_id = @userId
                GROUP BY n.id, n.title, n.author, n.cover_image, n.chapter_count
                ORDER BY last_download DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser?.Id ?? 0);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                novels.Add(new DownloadedNovel
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Author = reader.GetString(2),
                    CoverImage = reader.IsDBNull(3) ? null : reader.GetString(3),
                    DownloadedChapters = reader.GetInt32(4),
                    TotalChapters = reader.GetInt32(5),
                    TotalSize = reader.GetInt64(6),
                    LastDownload = reader.GetDateTime(7)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo descargas: {ex.Message}");
        }

        return novels;
    }

    /// <summary>
    /// Elimina un capítulo descargado
    /// </summary>
    public async Task<bool> DeleteDownloadedChapterAsync(int chapterId)
    {
        try
        {
            // Eliminar archivo
            var fileName = GetChapterFileName(chapterId);
            var filePath = Path.Combine(_downloadsDirectory, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // Eliminar registro de BD
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"DELETE FROM downloads 
                         WHERE chapter_id = @chapterId 
                         AND user_id = @userId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@chapterId", chapterId);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser?.Id ?? 0);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error eliminando descarga: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Elimina todas las descargas de una novela
    /// </summary>
    public async Task<bool> DeleteNovelDownloadsAsync(int novelId)
    {
        try
        {
            // Obtener capítulos descargados
            var chapters = await GetDownloadedChaptersAsync(novelId);

            // Eliminar archivos
            foreach (var chapter in chapters)
            {
                var fileName = GetChapterFileName(chapter.Id);
                var filePath = Path.Combine(_downloadsDirectory, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            // Eliminar registros de BD
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"DELETE FROM downloads 
                         WHERE novel_id = @novelId 
                         AND user_id = @userId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@novelId", novelId);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser?.Id ?? 0);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error eliminando descargas de novela: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Gestión de Espacio

    /// <summary>
    /// Obtiene información del espacio usado
    /// </summary>
    public async Task<StorageInfo> GetStorageInfoAsync()
    {
        try
        {
            // Calcular espacio usado
            var files = Directory.GetFiles(_downloadsDirectory);
            long totalSize = 0;

            foreach (var file in files)
            {
                var info = new FileInfo(file);
                totalSize += info.Length;
            }

            // Obtener conteo de descargas
            var downloadCount = await GetDownloadCountAsync();

            return new StorageInfo
            {
                UsedSpace = totalSize,
                MaxSpace = MAX_STORAGE_BYTES,
                UsedPercentage = (double)totalSize / MAX_STORAGE_BYTES,
                FileCount = files.Length,
                NovelCount = downloadCount
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo info de almacenamiento: {ex.Message}");
            return new StorageInfo { MaxSpace = MAX_STORAGE_BYTES };
        }
    }

    /// <summary>
    /// Limpia descargas antiguas para liberar espacio
    /// </summary>
    public async Task<long> CleanOldDownloadsAsync(int daysOld = 30)
    {
        long freedSpace = 0;

        try
        {
            var cutoffDate = DateTime.Now.AddDays(-daysOld);

            // Obtener descargas antiguas
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT chapter_id FROM downloads 
                         WHERE user_id = @userId 
                         AND downloaded_at < @cutoffDate";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser?.Id ?? 0);
            command.Parameters.AddWithValue("@cutoffDate", cutoffDate);

            var oldChapters = new List<int>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                oldChapters.Add(reader.GetInt32(0));
            }

            // Eliminar archivos antiguos
            foreach (var chapterId in oldChapters)
            {
                var fileName = GetChapterFileName(chapterId);
                var filePath = Path.Combine(_downloadsDirectory, fileName);

                if (File.Exists(filePath))
                {
                    var info = new FileInfo(filePath);
                    freedSpace += info.Length;
                    File.Delete(filePath);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error limpiando descargas antiguas: {ex.Message}");
        }

        return freedSpace;
    }

    #endregion

    #region Métodos Privados de Ayuda

    private async Task<Chapter> GetChapterDataAsync(int chapterId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT * FROM chapters WHERE id = @id";
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
            System.Diagnostics.Debug.WriteLine($"Error obteniendo datos del capítulo: {ex.Message}");
        }

        return null;
    }

    private async Task<List<Chapter>> GetNovelChaptersAsync(int novelId)
    {
        var chapters = new List<Chapter>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT id FROM chapters 
                         WHERE novel_id = @novelId 
                         ORDER BY chapter_number";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@novelId", novelId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                chapters.Add(new Chapter { Id = reader.GetInt32(0) });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo capítulos: {ex.Message}");
        }

        return chapters;
    }

    private async Task<List<Chapter>> GetDownloadedChaptersAsync(int novelId)
    {
        var chapters = new List<Chapter>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT chapter_id as Id FROM downloads 
                         WHERE novel_id = @novelId 
                         AND user_id = @userId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@novelId", novelId);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser?.Id ?? 0);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                chapters.Add(new Chapter { Id = reader.GetInt32(0) });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo capítulos descargados: {ex.Message}");
        }

        return chapters;
    }

    private async Task RegisterDownloadAsync(int chapterId, int novelId, long fileSize)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"INSERT INTO downloads (user_id, novel_id, chapter_id, downloaded_at, file_size)
                         VALUES (@userId, @novelId, @chapterId, GETDATE(), @fileSize)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser?.Id ?? 0);
            command.Parameters.AddWithValue("@novelId", novelId);
            command.Parameters.AddWithValue("@chapterId", chapterId);
            command.Parameters.AddWithValue("@fileSize", fileSize);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error registrando descarga: {ex.Message}");
        }
    }

    private async Task<bool> HasEnoughSpaceAsync(long requiredBytes)
    {
        var storageInfo = await GetStorageInfoAsync();
        return (storageInfo.UsedSpace + requiredBytes) <= MAX_STORAGE_BYTES;
    }

    private async Task<int> GetDownloadCountAsync()
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT COUNT(DISTINCT novel_id) FROM downloads 
                         WHERE user_id = @userId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser?.Id ?? 0);

            return (int)await command.ExecuteScalarAsync();
        }
        catch
        {
            return 0;
        }
    }

    private string GetChapterFileName(int chapterId)
    {
        return $"chapter_{chapterId}.json";
    }

    private long EstimateChapterSize(string content)
    {
        // Estimar tamaño basado en longitud del contenido
        // Aproximadamente 1 byte por carácter + overhead JSON
        return content.Length * 2;
    }

    #endregion
}

#region Modelos de Apoyo

/// <summary>
/// Información de una novela descargada
/// </summary>
public class DownloadedNovel
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string CoverImage { get; set; }
    public int DownloadedChapters { get; set; }
    public int TotalChapters { get; set; }
    public long TotalSize { get; set; }
    public DateTime LastDownload { get; set; }

    // Propiedades calculadas
    public bool IsComplete => DownloadedChapters == TotalChapters;
    public double ProgressPercentage => TotalChapters > 0 ? (double)DownloadedChapters / TotalChapters : 0;
    public string FormattedSize => FormatFileSize(TotalSize);

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Información del almacenamiento
/// </summary>
public class StorageInfo
{
    public long UsedSpace { get; set; }
    public long MaxSpace { get; set; }
    public double UsedPercentage { get; set; }
    public int FileCount { get; set; }
    public int NovelCount { get; set; }

    public string FormattedUsedSpace => FormatFileSize(UsedSpace);
    public string FormattedMaxSpace => FormatFileSize(MaxSpace);

    private string FormatFileSize(long bytes)
    {
        if (bytes >= 1073741824)
            return $"{bytes / 1073741824.0:0.##} GB";
        else if (bytes >= 1048576)
            return $"{bytes / 1048576.0:0.##} MB";
        else if (bytes >= 1024)
            return $"{bytes / 1024.0:0.##} KB";
        else
            return $"{bytes} B";
    }
}

#endregion