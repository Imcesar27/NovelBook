using Microsoft.Data.SqlClient;
using NovelBook.Models;

namespace NovelBook.Services;

/// <summary>
/// Servicio para manejar géneros y categorías populares
/// Gestiona estadísticas de popularidad basadas en lecturas y biblioteca
/// </summary>
public class GenreService
{
    private readonly DatabaseService _database;

    public GenreService(DatabaseService database)
    {
        _database = database;
    }

    /// <summary>
    /// Obtiene todos los géneros disponibles
    /// </summary>
    public async Task<List<Genre>> GetAllGenresAsync()
    {
        var genres = new List<Genre>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = "SELECT * FROM genres ORDER BY name";
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                genres.Add(new Genre
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Description = "" // La tabla genres no tiene columna description
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo géneros: {ex.Message}");
        }

        return genres;
    }

    /// <summary>
    /// Obtiene los géneros más populares basándose en diferentes métricas
    /// </summary>
    public async Task<List<PopularGenre>> GetPopularGenresAsync(int limit = 10)
    {
        var popularGenres = new List<PopularGenre>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Query compleja que considera múltiples factores de popularidad
            var query = @"WITH GenreStats AS (
                            SELECT 
                                g.id,
                                g.name,
                                -- Número de novelas en el género
                                COUNT(DISTINCT ng.novel_id) as novel_count,
                                -- Promedio de rating de las novelas del género
                                AVG(n.rating) as avg_rating,
                                -- Total de capítulos leídos de novelas de este género
                                ISNULL(SUM(rh.chapters_read), 0) as total_chapters_read,
                                -- Número de usuarios que tienen novelas de este género en su biblioteca
                                COUNT(DISTINCT ul.user_id) as users_count,
                                -- Número de reseñas totales
                                ISNULL(SUM(rv.review_count), 0) as total_reviews
                            FROM genres g
                            LEFT JOIN novel_genres ng ON g.id = ng.genre_id
                            LEFT JOIN novels n ON ng.novel_id = n.id
                            LEFT JOIN user_library ul ON n.id = ul.novel_id
                            LEFT JOIN (
                                SELECT novel_id, COUNT(DISTINCT chapter_id) as chapters_read
                                FROM reading_history
                                GROUP BY novel_id
                            ) rh ON n.id = rh.novel_id
                            LEFT JOIN (
                                SELECT novel_id, COUNT(*) as review_count
                                FROM reviews
                                GROUP BY novel_id
                            ) rv ON n.id = rv.novel_id
                            GROUP BY g.id, g.name
                        )
                        SELECT TOP (@limit)
                            id,
                            name,
                            novel_count,
                            avg_rating,
                            total_chapters_read,
                            users_count,
                            total_reviews,
                            -- Calcular un puntaje de popularidad ponderado
                            CAST((
                                (novel_count * 10) +                    -- Peso por cantidad de novelas
                                (ISNULL(avg_rating, 0) * 20) +         -- Peso por rating promedio
                                (total_chapters_read * 0.1) +          -- Peso por capítulos leídos
                                (users_count * 5) +                     -- Peso por usuarios únicos
                                (total_reviews * 2)                     -- Peso por reseñas
                            ) AS FLOAT) as popularity_score
                        FROM GenreStats
                        WHERE novel_count > 0
                        ORDER BY popularity_score DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@limit", limit);

            using var reader = await command.ExecuteReaderAsync();
            int rank = 1;
            while (await reader.ReadAsync())
            {
                popularGenres.Add(new PopularGenre
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Description = "", // La tabla genres no tiene columna description
                    Rank = rank++,
                    NovelCount = reader.GetInt32(reader.GetOrdinal("novel_count")),
                    AverageRating = reader.IsDBNull(reader.GetOrdinal("avg_rating")) ?
                                    0 : reader.GetDecimal(reader.GetOrdinal("avg_rating")),
                    TotalChaptersRead = reader.GetInt32(reader.GetOrdinal("total_chapters_read")),
                    ActiveUsers = reader.GetInt32(reader.GetOrdinal("users_count")),
                    TotalReviews = reader.GetInt32(reader.GetOrdinal("total_reviews")),
                    PopularityScore = reader.GetDouble(reader.GetOrdinal("popularity_score"))
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo géneros populares: {ex.Message}");
        }

        return popularGenres;
    }

    /// <summary>
    /// Obtiene las novelas más populares de un género específico
    /// </summary>
    public async Task<List<Novel>> GetTopNovelsByGenreAsync(int genreId, int limit = 20)
    {
        var novels = new List<Novel>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT TOP (@limit) n.*, 
                         STRING_AGG(g.name, ', ') WITHIN GROUP (ORDER BY g.name) as genres
                         FROM novels n
                         INNER JOIN novel_genres ng ON n.id = ng.novel_id
                         LEFT JOIN genres g ON ng.genre_id = g.id
                         WHERE n.id IN (
                             SELECT novel_id FROM novel_genres WHERE genre_id = @genreId
                         )
                         GROUP BY n.id, n.title, n.author, n.cover_image, n.synopsis, 
                                  n.status, n.rating, n.chapter_count, n.created_at, n.updated_at
                         ORDER BY n.rating DESC, n.chapter_count DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@genreId", genreId);
            command.Parameters.AddWithValue("@limit", limit);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var novel = new Novel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Author = reader.IsDBNull(reader.GetOrdinal("author")) ?
                             "Autor desconocido" : reader.GetString(reader.GetOrdinal("author")),
                    CoverImage = reader.IsDBNull(reader.GetOrdinal("cover_image")) ?
                                 "" : reader.GetString(reader.GetOrdinal("cover_image")),
                    Synopsis = reader.IsDBNull(reader.GetOrdinal("synopsis")) ?
                               "" : reader.GetString(reader.GetOrdinal("synopsis")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    Rating = reader.GetDecimal(reader.GetOrdinal("rating")),
                    ChapterCount = reader.GetInt32(reader.GetOrdinal("chapter_count")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                };

                // Agregar géneros
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
            System.Diagnostics.Debug.WriteLine($"Error obteniendo novelas por género: {ex.Message}");
        }

        return novels;
    }

    /// <summary>
    /// Obtiene estadísticas de un género específico
    /// </summary>
    public async Task<GenreStats> GetGenreStatsAsync(int genreId)
    {
        var stats = new GenreStats();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT 
                            g.id,
                            g.name,
                            COUNT(DISTINCT ng.novel_id) as total_novels,
                            COUNT(DISTINCT CASE WHEN n.status = 'completed' THEN n.id END) as completed_novels,
                            COUNT(DISTINCT CASE WHEN n.status = 'ongoing' THEN n.id END) as ongoing_novels,
                            AVG(n.rating) as average_rating,
                            MIN(n.rating) as min_rating,
                            MAX(n.rating) as max_rating,
                            COUNT(DISTINCT ul.user_id) as unique_readers,
                            COUNT(DISTINCT r.id) as total_reviews,
                            SUM(n.chapter_count) as total_chapters
                         FROM genres g
                         LEFT JOIN novel_genres ng ON g.id = ng.genre_id
                         LEFT JOIN novels n ON ng.novel_id = n.id
                         LEFT JOIN user_library ul ON n.id = ul.novel_id
                         LEFT JOIN reviews r ON n.id = r.novel_id
                         WHERE g.id = @genreId
                         GROUP BY g.id, g.name";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@genreId", genreId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                stats = new GenreStats
                {
                    GenreId = reader.GetInt32(reader.GetOrdinal("id")),
                    GenreName = reader.GetString(reader.GetOrdinal("name")),
                    Description = "", // La tabla genres no tiene columna description
                    TotalNovels = reader.GetInt32(reader.GetOrdinal("total_novels")),
                    CompletedNovels = reader.GetInt32(reader.GetOrdinal("completed_novels")),
                    OngoingNovels = reader.GetInt32(reader.GetOrdinal("ongoing_novels")),
                    AverageRating = reader.IsDBNull(reader.GetOrdinal("average_rating")) ?
                                    0 : reader.GetDecimal(reader.GetOrdinal("average_rating")),
                    MinRating = reader.IsDBNull(reader.GetOrdinal("min_rating")) ?
                                0 : reader.GetDecimal(reader.GetOrdinal("min_rating")),
                    MaxRating = reader.IsDBNull(reader.GetOrdinal("max_rating")) ?
                                0 : reader.GetDecimal(reader.GetOrdinal("max_rating")),
                    UniqueReaders = reader.GetInt32(reader.GetOrdinal("unique_readers")),
                    TotalReviews = reader.GetInt32(reader.GetOrdinal("total_reviews")),
                    TotalChapters = reader.GetInt32(reader.GetOrdinal("total_chapters"))
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo estadísticas del género: {ex.Message}");
        }

        return stats;
    }

    /// <summary>
    /// Crea un nuevo género (sin descripción ya que la tabla no tiene esa columna)
    /// </summary>
    public async Task<bool> CreateGenreAsync(string name)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Verificar si ya existe
            var checkQuery = "SELECT COUNT(*) FROM genres WHERE name = @name";
            using var checkCommand = new SqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@name", name);

            var exists = (int)await checkCommand.ExecuteScalarAsync() > 0;
            if (exists) return false;

            // Crear el género (solo con name ya que no hay columna description)
            var query = "INSERT INTO genres (name) VALUES (@name)";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@name", name);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creando género: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Actualiza un género existente (solo el nombre)
    /// </summary>
    public async Task<bool> UpdateGenreAsync(int genreId, string name)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = "UPDATE genres SET name = @name WHERE id = @id";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", genreId);
            command.Parameters.AddWithValue("@name", name);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error actualizando género: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Elimina un género (solo si no tiene novelas asociadas)
    /// </summary>
    public async Task<bool> DeleteGenreAsync(int genreId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Verificar si tiene novelas asociadas
            var checkQuery = "SELECT COUNT(*) FROM novel_genres WHERE genre_id = @id";
            using var checkCommand = new SqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@id", genreId);

            var hasNovels = (int)await checkCommand.ExecuteScalarAsync() > 0;
            if (hasNovels) return false;

            // Eliminar el género
            var query = "DELETE FROM genres WHERE id = @id";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", genreId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error eliminando género: {ex.Message}");
            return false;
        }
    }
}