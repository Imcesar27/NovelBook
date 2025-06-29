using NovelBook.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace NovelBook.Services;

public class NovelService
{
    private readonly DatabaseService _database;

    public NovelService(DatabaseService database)
    {
        _database = database;
    }

    // Agregar este método para obtener novelas con sus imágenes
    public async Task<List<Novel>> GetAllNovelsAsync()
    {
        var novels = new List<Novel>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Query mejorada que incluye géneros
            var query = @"SELECT n.*, 
                     STRING_AGG(g.name, ', ') WITHIN GROUP (ORDER BY g.name) as genres
                     FROM novels n
                     LEFT JOIN novel_genres ng ON n.id = ng.novel_id
                     LEFT JOIN genres g ON ng.genre_id = g.id
                     GROUP BY n.id, n.title, n.author, n.cover_image, n.synopsis, 
                              n.status, n.rating, n.chapter_count, n.created_at, n.updated_at";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var novel = new Novel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Author = reader.IsDBNull(reader.GetOrdinal("author")) ? "" : reader.GetString(reader.GetOrdinal("author")),
                    CoverImage = reader.IsDBNull(reader.GetOrdinal("cover_image")) ? "" : reader.GetString(reader.GetOrdinal("cover_image")),
                    Synopsis = reader.IsDBNull(reader.GetOrdinal("synopsis")) ? "" : reader.GetString(reader.GetOrdinal("synopsis")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    Rating = reader.GetDecimal(reader.GetOrdinal("rating")),
                    ChapterCount = reader.GetInt32(reader.GetOrdinal("chapter_count")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                };

                // Agregar géneros si existen
                if (!reader.IsDBNull(reader.GetOrdinal("genres")))
                {
                    var genresString = reader.GetString(reader.GetOrdinal("genres"));
                    novel.Genres = genresString.Split(',').Select(g => new Genre { Name = g.Trim() }).ToList();
                }

                novels.Add(novel);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al obtener novelas: {ex.Message}");
        }

        return novels;
    }
    public async Task<List<Novel>> SearchNovelsAsync(string searchTerm)
    {
        var novels = new List<Novel>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT * FROM novels 
                         WHERE title LIKE @search OR author LIKE @search 
                         ORDER BY rating DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@search", $"%{searchTerm}%");

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                novels.Add(ReadNovelFromReader(reader));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en búsqueda: {ex.Message}");
        }

        return novels;
    }

    // Método para obtener una novela con sus géneros
    public async Task<Novel> GetNovelByIdAsync(int novelId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Obtener novela con géneros
            var query = @"SELECT n.*, 
                     STRING_AGG(g.name, ', ') WITHIN GROUP (ORDER BY g.name) as genres
                     FROM novels n
                     LEFT JOIN novel_genres ng ON n.id = ng.novel_id
                     LEFT JOIN genres g ON ng.genre_id = g.id
                     WHERE n.id = @id
                     GROUP BY n.id, n.title, n.author, n.cover_image, n.synopsis, 
                              n.status, n.rating, n.chapter_count, n.created_at, n.updated_at";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", novelId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var novel = ReadNovelFromReader(reader);

                // Agregar géneros
                if (!reader.IsDBNull(reader.GetOrdinal("genres")))
                {
                    var genresString = reader.GetString(reader.GetOrdinal("genres"));
                    novel.Genres = genresString.Split(',').Select(g => new Genre { Name = g.Trim() }).ToList();
                }

                return novel;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al obtener novela: {ex.Message}");
        }

        return null;
    }

    private Novel ReadNovelFromReader(SqlDataReader reader)
    {
        return new Novel
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Title = reader.GetString(reader.GetOrdinal("title")),
            Author = reader.IsDBNull(reader.GetOrdinal("author")) ? "" : reader.GetString(reader.GetOrdinal("author")),
            CoverImage = reader.IsDBNull(reader.GetOrdinal("cover_image")) ? "" : reader.GetString(reader.GetOrdinal("cover_image")),
            Synopsis = reader.IsDBNull(reader.GetOrdinal("synopsis")) ? "" : reader.GetString(reader.GetOrdinal("synopsis")),
            Status = reader.GetString(reader.GetOrdinal("status")),
            Rating = reader.GetDecimal(reader.GetOrdinal("rating")),
            ChapterCount = reader.GetInt32(reader.GetOrdinal("chapter_count")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }

    public async Task<bool> CreateNovelAsync(string title, string author, string synopsis,
    string status, List<string> genres, byte[] coverImage, string imageType,
    string firstChapterTitle, string firstChapterContent)
    {
        using var connection = _database.GetConnection();
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        try
        {
            // Insertar novela
            var novelQuery = @"INSERT INTO novels (title, author, synopsis, status, chapter_count) 
                          OUTPUT INSERTED.id
                          VALUES (@title, @author, @synopsis, @status, @chapterCount)";

            using var novelCommand = new SqlCommand(novelQuery, connection, transaction);
            novelCommand.Parameters.AddWithValue("@title", title);
            novelCommand.Parameters.AddWithValue("@author", author);
            novelCommand.Parameters.AddWithValue("@synopsis", synopsis);
            novelCommand.Parameters.AddWithValue("@status", status);
            novelCommand.Parameters.AddWithValue("@chapterCount", string.IsNullOrEmpty(firstChapterContent) ? 0 : 1);

            var novelId = (int)await novelCommand.ExecuteScalarAsync();

            // Insertar imagen si existe
            if (coverImage != null && coverImage.Length > 0)
            {
                var imageQuery = @"INSERT INTO novel_covers (novel_id, image_data, image_type) 
                             VALUES (@novelId, @imageData, @imageType)";

                using var imageCommand = new SqlCommand(imageQuery, connection, transaction);
                imageCommand.Parameters.AddWithValue("@novelId", novelId);
                imageCommand.Parameters.AddWithValue("@imageData", coverImage);
                imageCommand.Parameters.AddWithValue("@imageType", imageType ?? "image/jpeg");

                await imageCommand.ExecuteNonQueryAsync();

                // Actualizar URL de la imagen en la novela
                var updateQuery = "UPDATE novels SET cover_image = @url WHERE id = @id";
                using var updateCommand = new SqlCommand(updateQuery, connection, transaction);
                updateCommand.Parameters.AddWithValue("@url", $"db://covers/{novelId}");
                updateCommand.Parameters.AddWithValue("@id", novelId);
                await updateCommand.ExecuteNonQueryAsync();
            }

            // Insertar géneros
            foreach (var genre in genres)
            {
                var genreQuery = @"INSERT INTO novel_genres (novel_id, genre_id)
                             SELECT @novelId, id FROM genres WHERE name = @genreName";

                using var genreCommand = new SqlCommand(genreQuery, connection, transaction);
                genreCommand.Parameters.AddWithValue("@novelId", novelId);
                genreCommand.Parameters.AddWithValue("@genreName", genre);

                await genreCommand.ExecuteNonQueryAsync();
            }

            // Insertar primer capítulo si existe
            if (!string.IsNullOrWhiteSpace(firstChapterContent))
            {
                var chapterQuery = @"INSERT INTO chapters (novel_id, chapter_number, title, content) 
                               VALUES (@novelId, 1, @title, @content)";

                using var chapterCommand = new SqlCommand(chapterQuery, connection, transaction);
                chapterCommand.Parameters.AddWithValue("@novelId", novelId);
                chapterCommand.Parameters.AddWithValue("@title", string.IsNullOrWhiteSpace(firstChapterTitle) ? "Capítulo 1" : firstChapterTitle);
                chapterCommand.Parameters.AddWithValue("@content", firstChapterContent);

                await chapterCommand.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            System.Diagnostics.Debug.WriteLine($"Error creando novela: {ex.Message}");
            return false;
        }
    }

    // Método para obtener imagen de la base de datos
    public async Task<(byte[] data, string type)> GetNovelCoverAsync(int novelId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = "SELECT image_data, image_type FROM novel_covers WHERE novel_id = @novelId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@novelId", novelId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var data = (byte[])reader["image_data"];
                var type = reader.GetString(reader.GetOrdinal("image_type"));
                return (data, type);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo imagen: {ex.Message}");
        }

        return (null, null);
    }

    public async Task<bool> AddChapterAsync(int novelId, int chapterNumber, string title, string content)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            // Verificar si el número de capítulo ya existe
            var checkQuery = "SELECT COUNT(*) FROM chapters WHERE novel_id = @novelId AND chapter_number = @chapterNumber";
            using var checkCommand = new SqlCommand(checkQuery, connection, transaction);
            checkCommand.Parameters.AddWithValue("@novelId", novelId);
            checkCommand.Parameters.AddWithValue("@chapterNumber", chapterNumber);

            var exists = (int)await checkCommand.ExecuteScalarAsync() > 0;
            if (exists)
            {
                await transaction.RollbackAsync();
                return false;
            }

            // Insertar el capítulo
            var insertQuery = @"INSERT INTO chapters (novel_id, chapter_number, title, content) 
                           VALUES (@novelId, @chapterNumber, @title, @content)";

            using var insertCommand = new SqlCommand(insertQuery, connection, transaction);
            insertCommand.Parameters.AddWithValue("@novelId", novelId);
            insertCommand.Parameters.AddWithValue("@chapterNumber", chapterNumber);
            insertCommand.Parameters.AddWithValue("@title", title);
            insertCommand.Parameters.AddWithValue("@content", content);

            await insertCommand.ExecuteNonQueryAsync();

            // Actualizar el conteo de capítulos
            var updateQuery = @"UPDATE novels 
                           SET chapter_count = (SELECT COUNT(*) FROM chapters WHERE novel_id = @novelId),
                               updated_at = GETDATE()
                           WHERE id = @novelId";

            using var updateCommand = new SqlCommand(updateQuery, connection, transaction);
            updateCommand.Parameters.AddWithValue("@novelId", novelId);
            await updateCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error agregando capítulo: {ex.Message}");
            return false;
        }
    }

    public async Task<List<Chapter>> GetChaptersAsync(int novelId)
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
    /// Elimina una novela y todos sus datos relacionados
    /// </summary>
    /// <param name="novelId">ID de la novela a eliminar</param>
    /// <returns>true si se eliminó correctamente, false en caso contrario</returns>
    public async Task<bool> DeleteNovelAsync(int novelId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // La eliminación en cascada debería encargarse de todo
            // (capítulos, géneros, reseñas, etc.)
            var query = "DELETE FROM novels WHERE id = @id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", novelId);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error eliminando novela: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Actualiza una novela existente
    /// </summary>
    public async Task<bool> UpdateNovelAsync(int novelId, string title, string author,
        string synopsis, string status, List<string> genres, byte[] coverImage, string imageType)
    {
        using var connection = _database.GetConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 1. Actualizar información básica de la novela
            var updateQuery = @"UPDATE novels 
                              SET title = @title, 
                                  author = @author, 
                                  synopsis = @synopsis, 
                                  status = @status,
                                  updated_at = GETDATE()
                              WHERE id = @id";

            using var updateCommand = new SqlCommand(updateQuery, connection, transaction);
            updateCommand.Parameters.AddWithValue("@id", novelId);
            updateCommand.Parameters.AddWithValue("@title", title);
            updateCommand.Parameters.AddWithValue("@author", author);
            updateCommand.Parameters.AddWithValue("@synopsis", synopsis);
            updateCommand.Parameters.AddWithValue("@status", status);

            await updateCommand.ExecuteNonQueryAsync();

            // 2. Actualizar imagen si se proporcionó una nueva
            if (coverImage != null && coverImage.Length > 0)
            {
                // Primero, eliminar imagen anterior si existe
                var deleteImageQuery = "DELETE FROM novel_covers WHERE novel_id = @novelId";
                using var deleteImageCommand = new SqlCommand(deleteImageQuery, connection, transaction);
                deleteImageCommand.Parameters.AddWithValue("@novelId", novelId);
                await deleteImageCommand.ExecuteNonQueryAsync();

                // Insertar nueva imagen
                var imageQuery = @"INSERT INTO novel_covers (novel_id, image_data, image_type) 
                                 VALUES (@novelId, @imageData, @imageType)";

                using var imageCommand = new SqlCommand(imageQuery, connection, transaction);
                imageCommand.Parameters.AddWithValue("@novelId", novelId);
                imageCommand.Parameters.AddWithValue("@imageData", coverImage);
                imageCommand.Parameters.AddWithValue("@imageType", imageType ?? "image/jpeg");

                await imageCommand.ExecuteNonQueryAsync();

                // Actualizar URL de la imagen en la novela
                var updateImageUrlQuery = "UPDATE novels SET cover_image = @url WHERE id = @id";
                using var updateImageUrlCommand = new SqlCommand(updateImageUrlQuery, connection, transaction);
                updateImageUrlCommand.Parameters.AddWithValue("@url", $"db://covers/{novelId}");
                updateImageUrlCommand.Parameters.AddWithValue("@id", novelId);
                await updateImageUrlCommand.ExecuteNonQueryAsync();
            }

            // 3. Actualizar géneros
            // Primero eliminar todos los géneros actuales
            var deleteGenresQuery = "DELETE FROM novel_genres WHERE novel_id = @novelId";
            using var deleteGenresCommand = new SqlCommand(deleteGenresQuery, connection, transaction);
            deleteGenresCommand.Parameters.AddWithValue("@novelId", novelId);
            await deleteGenresCommand.ExecuteNonQueryAsync();

            // Insertar nuevos géneros
            foreach (var genre in genres)
            {
                var genreQuery = @"INSERT INTO novel_genres (novel_id, genre_id)
                                 SELECT @novelId, id FROM genres WHERE name = @genreName";

                using var genreCommand = new SqlCommand(genreQuery, connection, transaction);
                genreCommand.Parameters.AddWithValue("@novelId", novelId);
                genreCommand.Parameters.AddWithValue("@genreName", genre);

                await genreCommand.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            System.Diagnostics.Debug.WriteLine($"Error actualizando novela: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Elimina un capítulo específico
    /// </summary>
    public async Task<bool> DeleteChapterAsync(int chapterId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            // Obtener información del capítulo antes de eliminarlo
            var getInfoQuery = "SELECT novel_id FROM chapters WHERE id = @id";
            using var getInfoCommand = new SqlCommand(getInfoQuery, connection, transaction);
            getInfoCommand.Parameters.AddWithValue("@id", chapterId);

            var novelId = await getInfoCommand.ExecuteScalarAsync();
            if (novelId == null)
            {
                await transaction.RollbackAsync();
                return false;
            }

            // Primero eliminar registros relacionados en reading_history
            var deleteHistoryQuery = "DELETE FROM reading_history WHERE chapter_id = @chapterId";
            using var deleteHistoryCommand = new SqlCommand(deleteHistoryQuery, connection, transaction);
            deleteHistoryCommand.Parameters.AddWithValue("@chapterId", chapterId);
            await deleteHistoryCommand.ExecuteNonQueryAsync();

            // Eliminar registros en reading_progress
            var deleteProgressQuery = "DELETE FROM reading_progress WHERE chapter_id = @chapterId";
            using var deleteProgressCommand = new SqlCommand(deleteProgressQuery, connection, transaction);
            deleteProgressCommand.Parameters.AddWithValue("@chapterId", chapterId);
            await deleteProgressCommand.ExecuteNonQueryAsync();

            // Eliminar registros en downloads si existen
            var deleteDownloadsQuery = "DELETE FROM downloads WHERE chapter_id = @chapterId";
            using var deleteDownloadsCommand = new SqlCommand(deleteDownloadsQuery, connection, transaction);
            deleteDownloadsCommand.Parameters.AddWithValue("@chapterId", chapterId);
            await deleteDownloadsCommand.ExecuteNonQueryAsync();

            // Ahora sí eliminar el capítulo
            var deleteQuery = "DELETE FROM chapters WHERE id = @id";
            using var deleteCommand = new SqlCommand(deleteQuery, connection, transaction);
            deleteCommand.Parameters.AddWithValue("@id", chapterId);
            await deleteCommand.ExecuteNonQueryAsync();

            // Actualizar el conteo de capítulos en la novela
            var updateCountQuery = @"UPDATE novels 
                                   SET chapter_count = (SELECT COUNT(*) FROM chapters WHERE novel_id = @novelId),
                                       updated_at = GETDATE()
                                   WHERE id = @novelId";

            using var updateCommand = new SqlCommand(updateCountQuery, connection, transaction);
            updateCommand.Parameters.AddWithValue("@novelId", novelId);
            await updateCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error eliminando capítulo: {ex.Message}");
            return false;
        }
    }

    // Agregar estos métodos al NovelService.cs

    /// <summary>
    /// Obtiene todos los géneros disponibles
    /// </summary>
    public async Task<List<string>> GetAllGenresAsync()
    {
        var genres = new List<string>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = "SELECT name FROM genres ORDER BY name";
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                genres.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo géneros: {ex.Message}");
        }

        return genres;
    }

    /// <summary>
    /// Obtiene los géneros de una novela específica
    /// </summary>
    public async Task<List<string>> GetNovelGenresAsync(int novelId)
    {
        var genres = new List<string>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT g.name 
                     FROM genres g
                     INNER JOIN novel_genres ng ON g.id = ng.genre_id
                     WHERE ng.novel_id = @novelId
                     ORDER BY g.name";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@novelId", novelId);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                genres.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo géneros de la novela: {ex.Message}");
        }

        return genres;
    }
}
