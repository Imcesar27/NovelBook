using Microsoft.Data.SqlClient;
using NovelBook.Models;
using System.Text.Json;

namespace NovelBook.Services;

/// <summary>
/// Servicio principal del módulo inteligente de analytics
/// Se encarga de:
/// - Calcular métricas de uso y engagement
/// - Analizar patrones de lectura
/// - Generar recomendaciones para el administrador
/// - Gestionar datos de las tablas de analytics
/// </summary>
public class AnalyticsService
{
    private readonly DatabaseService _database;
    private readonly TagService _tagService;

    /// <summary>
    /// Constructor que recibe el servicio de base de datos
    /// </summary>
    public AnalyticsService(DatabaseService database)
    {
        _database = database;
        _tagService = new TagService(database);
    }

    #region ========== MÉTRICAS DE ENGAGEMENT ==========

    /// <summary>
    /// Calcula el tiempo promedio de lectura por sesión (en minutos)
    /// Analiza la tabla reading_history para determinar cuánto tiempo 
    /// pasan los usuarios leyendo en promedio
    /// </summary>
    public async Task<decimal> CalculateAverageReadingTimeAsync()
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Query para calcular el tiempo promedio de lectura
            // Usamos la diferencia entre read_at de registros consecutivos del mismo usuario
            var query = @"
                SELECT AVG(CAST(reading_time AS DECIMAL(10,2))) as avg_time
                FROM reading_history
                WHERE reading_time IS NOT NULL AND reading_time > 0";

            using var command = new SqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync();

            if (result != null && result != DBNull.Value)
            {
                return Convert.ToDecimal(result);
            }

            return 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error calculando tiempo promedio: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Calcula la tasa de abandono de novelas (porcentaje)
    /// Una novela se considera abandonada si el usuario dejó de leerla
    /// antes de completar el 50% de los capítulos
    /// </summary>
    public async Task<decimal> CalculateAbandonmentRateAsync()
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Query para calcular tasa de abandono
            var query = @"
                SELECT 
                    CASE 
                        WHEN COUNT(*) = 0 THEN 0
                        ELSE (COUNT(CASE WHEN reading_status = 'dropped' THEN 1 END) * 100.0 / COUNT(*))
                    END as abandonment_rate
                FROM user_library";

            using var command = new SqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync();

            if (result != null && result != DBNull.Value)
            {
                return Convert.ToDecimal(result);
            }

            return 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error calculando tasa de abandono: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Calcula el promedio de capítulos leídos por novela
    /// </summary>
    public async Task<decimal> CalculateAverageChaptersReadAsync()
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"
                SELECT AVG(CAST(last_read_chapter AS DECIMAL(10,2))) as avg_chapters
                FROM user_library
                WHERE last_read_chapter > 0";

            using var command = new SqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync();

            if (result != null && result != DBNull.Value)
            {
                return Convert.ToDecimal(result);
            }

            return 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error calculando capítulos promedio: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Obtiene el número de usuarios activos (que han leído en los últimos 30 días)
    /// </summary>
    public async Task<int> GetActiveUsersCountAsync()
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"
                SELECT COUNT(DISTINCT user_id) 
                FROM reading_history 
                WHERE read_at >= DATEADD(day, -30, GETDATE())";

            using var command = new SqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync();

            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }

            return 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo usuarios activos: {ex.Message}");
            return 0;
        }
    }

    #endregion

    #region ========== MÉTRICAS DE POPULARIDAD ==========

    /// <summary>
    /// Obtiene las etiquetas más populares para el dashboard
    /// </summary>
    public async Task<List<(string TagName, int NovelCount, int TotalVotes)>> GetPopularTagsStatsAsync(int limit = 5)
    {
        return await _tagService.GetTagStatsAsync(limit);
    }

    /// <summary>
    /// Obtiene las etiquetas más recientes para el dashboard
    /// </summary>
    public async Task<List<(string TagName, int Count, DateTime CreatedAt)>> GetRecentTagsAsync(int limit = 10)
    {
        return await _tagService.GetRecentTagsAsync(limit);
    }

    /// <summary>
    /// Obtiene las novelas más leídas (Top N)
    /// </summary>
    public async Task<List<(Novel Novel, int ReadCount)>> GetMostReadNovelsAsync(int topN = 10)
    {
        var result = new List<(Novel, int)>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = $@"
                SELECT TOP {topN}
                    n.id, n.title, n.author, n.cover_image, n.synopsis, 
                    n.status, n.rating, n.chapter_count,
                    COUNT(rh.id) as read_count
                FROM novels n
                LEFT JOIN reading_history rh ON n.id = rh.novel_id
                GROUP BY n.id, n.title, n.author, n.cover_image, n.synopsis, 
                         n.status, n.rating, n.chapter_count
                ORDER BY read_count DESC";

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
                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "" : reader.GetString(reader.GetOrdinal("status")),
                    Rating = reader.IsDBNull(reader.GetOrdinal("rating")) ? 0 : reader.GetDecimal(reader.GetOrdinal("rating")),
                    ChapterCount = reader.IsDBNull(reader.GetOrdinal("chapter_count")) ? 0 : reader.GetInt32(reader.GetOrdinal("chapter_count"))
                };

                var readCount = reader.GetInt32(reader.GetOrdinal("read_count"));
                result.Add((novel, readCount));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo novelas más leídas: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Obtiene los géneros más populares con estadísticas
    /// </summary>
    public async Task<List<(string Genre, int NovelCount, int ReadCount, decimal AvgRating)>> GetPopularGenresStatsAsync(int topN = 10)
    {
        var result = new List<(string, int, int, decimal)>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = $@"
                SELECT TOP {topN}
                    g.name as genre_name,
                    COUNT(DISTINCT n.id) as novel_count,
                    COUNT(rh.id) as read_count,
                    AVG(CAST(n.rating AS DECIMAL(3,2))) as avg_rating
                FROM genres g
                INNER JOIN novel_genres ng ON g.id = ng.genre_id
                INNER JOIN novels n ON ng.novel_id = n.id
                LEFT JOIN reading_history rh ON n.id = rh.novel_id
                GROUP BY g.id, g.name
                ORDER BY read_count DESC";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var genreName = reader.GetString(reader.GetOrdinal("genre_name"));
                var novelCount = reader.GetInt32(reader.GetOrdinal("novel_count"));
                var readCount = reader.GetInt32(reader.GetOrdinal("read_count"));
                var avgRating = reader.IsDBNull(reader.GetOrdinal("avg_rating")) ? 0 : reader.GetDecimal(reader.GetOrdinal("avg_rating"));

                result.Add((genreName, novelCount, readCount, avgRating));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo géneros populares: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Obtiene los autores con mejor engagement (más lecturas y mejores ratings)
    /// </summary>
    public async Task<List<(string Author, int NovelCount, int ReadCount, decimal AvgRating)>> GetTopAuthorsAsync(int topN = 10)
    {
        var result = new List<(string, int, int, decimal)>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = $@"
                SELECT TOP {topN}
                    n.author,
                    COUNT(DISTINCT n.id) as novel_count,
                    COUNT(rh.id) as read_count,
                    AVG(CAST(n.rating AS DECIMAL(3,2))) as avg_rating
                FROM novels n
                LEFT JOIN reading_history rh ON n.id = rh.novel_id
                WHERE n.author IS NOT NULL AND n.author != ''
                GROUP BY n.author
                ORDER BY read_count DESC";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var author = reader.GetString(reader.GetOrdinal("author"));
                var novelCount = reader.GetInt32(reader.GetOrdinal("novel_count"));
                var readCount = reader.GetInt32(reader.GetOrdinal("read_count"));
                var avgRating = reader.IsDBNull(reader.GetOrdinal("avg_rating")) ? 0 : reader.GetDecimal(reader.GetOrdinal("avg_rating"));

                result.Add((author, novelCount, readCount, avgRating));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo top autores: {ex.Message}");
        }

        return result;
    }

    #endregion

    #region ========== ESTADÍSTICAS GENERALES ==========

    /// <summary>
    /// Obtiene estadísticas generales del sistema
    /// </summary>
    public async Task<Dictionary<string, object>> GetGeneralStatsAsync()
    {
        var stats = new Dictionary<string, object>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Total de usuarios
            var queryUsers = "SELECT COUNT(*) FROM users WHERE role = 'user'";
            using (var cmd = new SqlCommand(queryUsers, connection))
            {
                stats["TotalUsers"] = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Total de novelas
            var queryNovels = "SELECT COUNT(*) FROM novels";
            using (var cmd = new SqlCommand(queryNovels, connection))
            {
                stats["TotalNovels"] = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Total de capítulos
            var queryChapters = "SELECT COUNT(*) FROM chapters";
            using (var cmd = new SqlCommand(queryChapters, connection))
            {
                stats["TotalChapters"] = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Total de reseñas
            var queryReviews = "SELECT COUNT(*) FROM reviews";
            using (var cmd = new SqlCommand(queryReviews, connection))
            {
                stats["TotalReviews"] = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Rating promedio global
            var queryAvgRating = "SELECT AVG(CAST(rating AS DECIMAL(3,2))) FROM novels WHERE rating > 0";
            using (var cmd = new SqlCommand(queryAvgRating, connection))
            {
                var result = await cmd.ExecuteScalarAsync();
                stats["AverageRating"] = result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
            }

            // Novelas en biblioteca de usuarios
            var queryLibrary = "SELECT COUNT(*) FROM user_library";
            using (var cmd = new SqlCommand(queryLibrary, connection))
            {
                stats["TotalInLibraries"] = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Total de géneros
            var queryGenres = "SELECT COUNT(*) FROM genres";
            using (var cmd = new SqlCommand(queryGenres, connection))
            {
                stats["TotalGenres"] = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo estadísticas generales: {ex.Message}");
        }

        return stats;
    }

    #endregion

    #region ========== GESTIÓN DE MÉTRICAS (CRUD) ==========

    /// <summary>
    /// Guarda una métrica calculada en la base de datos
    /// </summary>
    public async Task<bool> SaveMetricAsync(AnalyticsMetric metric)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"
                INSERT INTO analytics_metrics (metric_type, metric_name, metric_value, calculated_at, metadata)
                VALUES (@type, @name, @value, @calculatedAt, @metadata)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@type", metric.MetricType);
            command.Parameters.AddWithValue("@name", metric.MetricName);
            command.Parameters.AddWithValue("@value", metric.MetricValue);
            command.Parameters.AddWithValue("@calculatedAt", metric.CalculatedAt);
            command.Parameters.AddWithValue("@metadata", (object)metric.Metadata ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error guardando métrica: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtiene las métricas más recientes por tipo
    /// </summary>
    public async Task<List<AnalyticsMetric>> GetRecentMetricsAsync(string metricType = null, int limit = 50)
    {
        var metrics = new List<AnalyticsMetric>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = $@"
                SELECT TOP {limit} *
                FROM analytics_metrics
                {(metricType != null ? "WHERE metric_type = @type" : "")}
                ORDER BY calculated_at DESC";

            using var command = new SqlCommand(query, connection);
            if (metricType != null)
            {
                command.Parameters.AddWithValue("@type", metricType);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                metrics.Add(new AnalyticsMetric
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    MetricType = reader.GetString(reader.GetOrdinal("metric_type")),
                    MetricName = reader.GetString(reader.GetOrdinal("metric_name")),
                    MetricValue = reader.GetDecimal(reader.GetOrdinal("metric_value")),
                    CalculatedAt = reader.GetDateTime(reader.GetOrdinal("calculated_at")),
                    Metadata = reader.IsDBNull(reader.GetOrdinal("metadata")) ? null : reader.GetString(reader.GetOrdinal("metadata"))
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo métricas: {ex.Message}");
        }

        return metrics;
    }

    #endregion

    #region ========== GESTIÓN DE RECOMENDACIONES (CRUD) ==========

    /// <summary>
    /// Guarda una recomendación en la base de datos
    /// </summary>
    /// <summary>
    /// Guarda una recomendación en la base de datos (evita duplicados)
    /// </summary>
    public async Task<bool> SaveRecommendationAsync(AdminRecommendation recommendation)
    {
        try
        {
            // Verificar si ya existe una recomendación similar no implementada
            if (await RecommendationExistsAsync(recommendation.RecommendationType, recommendation.Title))
            {
                System.Diagnostics.Debug.WriteLine($"Recomendación duplicada ignorada: {recommendation.Title}");
                return false; // No guardar duplicado
            }

            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"
                INSERT INTO admin_recommendations 
                    (recommendation_type, title, description, priority, confidence_score, created_at, metadata)
                VALUES 
                    (@type, @title, @description, @priority, @confidence, @createdAt, @metadata)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@type", recommendation.RecommendationType);
            command.Parameters.AddWithValue("@title", recommendation.Title);
            command.Parameters.AddWithValue("@description", recommendation.Description);
            command.Parameters.AddWithValue("@priority", recommendation.Priority);
            command.Parameters.AddWithValue("@confidence", recommendation.ConfidenceScore);
            command.Parameters.AddWithValue("@createdAt", recommendation.CreatedAt);
            command.Parameters.AddWithValue("@metadata", (object)recommendation.Metadata ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error guardando recomendación: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtiene las recomendaciones no leídas
    /// </summary>
    public async Task<List<AdminRecommendation>> GetUnreadRecommendationsAsync()
    {
        var recommendations = new List<AdminRecommendation>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"
                SELECT * FROM admin_recommendations
                WHERE is_read = 0
                ORDER BY priority DESC, created_at DESC";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                recommendations.Add(MapRecommendation(reader));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo recomendaciones: {ex.Message}");
        }

        return recommendations;
    }

    /// <summary>
    /// Obtiene todas las recomendaciones
    /// </summary>
    public async Task<List<AdminRecommendation>> GetAllRecommendationsAsync(int limit = 100)
    {
        var recommendations = new List<AdminRecommendation>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = $@"
                SELECT TOP {limit} * FROM admin_recommendations
                ORDER BY created_at DESC";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                recommendations.Add(MapRecommendation(reader));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo recomendaciones: {ex.Message}");
        }

        return recommendations;
    }

    /// <summary>
    /// Marca una recomendación como leída
    /// </summary>
    public async Task<bool> MarkRecommendationAsReadAsync(int recommendationId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = "UPDATE admin_recommendations SET is_read = 1 WHERE id = @id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", recommendationId);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error marcando como leída: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Marca una recomendación como implementada
    /// </summary>
    public async Task<bool> MarkRecommendationAsImplementedAsync(int recommendationId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = "UPDATE admin_recommendations SET is_implemented = 1, is_read = 1 WHERE id = @id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", recommendationId);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error marcando como implementada: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Desmarca una recomendación como leída (vuelve a no leída)
    /// </summary>
    public async Task<bool> UnmarkRecommendationAsReadAsync(int recommendationId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = "UPDATE admin_recommendations SET is_read = 0 WHERE id = @id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", recommendationId);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error desmarcando como leída: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Desmarca una recomendación como implementada (vuelve a leída o pendiente)
    /// </summary>
    public async Task<bool> UnmarkRecommendationAsImplementedAsync(int recommendationId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = "UPDATE admin_recommendations SET is_implemented = 0 WHERE id = @id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", recommendationId);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error desmarcando como implementada: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtiene recomendaciones filtradas por estado
    /// </summary>
    /// <param name="filter">pending = no leídas, read = leídas no implementadas, implemented = implementadas</param>
    public async Task<List<AdminRecommendation>> GetRecommendationsByStatusAsync(string filter, int limit = 100)
    {
        var recommendations = new List<AdminRecommendation>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            string whereClause = filter switch
            {
                "pending" => "WHERE is_read = 0 AND is_implemented = 0",
                "read" => "WHERE is_read = 1 AND is_implemented = 0",
                "implemented" => "WHERE is_implemented = 1",
                _ => "" // all
            };

            var query = $@"
                SELECT TOP {limit} * FROM admin_recommendations
                {whereClause}
                ORDER BY priority DESC, created_at DESC";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                recommendations.Add(MapRecommendation(reader));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo recomendaciones por estado: {ex.Message}");
        }

        return recommendations;
    }

    /// <summary>
    /// Obtiene el conteo de recomendaciones por estado
    /// </summary>
    public async Task<(int Pending, int Read, int Implemented)> GetRecommendationCountsAsync()
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"
                SELECT 
                    SUM(CASE WHEN is_read = 0 AND is_implemented = 0 THEN 1 ELSE 0 END) as pending,
                    SUM(CASE WHEN is_read = 1 AND is_implemented = 0 THEN 1 ELSE 0 END) as read_count,
                    SUM(CASE WHEN is_implemented = 1 THEN 1 ELSE 0 END) as implemented
                FROM admin_recommendations";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return (
                    reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                    reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                    reader.IsDBNull(2) ? 0 : reader.GetInt32(2)
                );
            }

            return (0, 0, 0);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo conteos: {ex.Message}");
            return (0, 0, 0);
        }
    }

    /// <summary>
    /// Mapea un SqlDataReader a un objeto AdminRecommendation
    /// </summary>
    private AdminRecommendation MapRecommendation(SqlDataReader reader)
    {
        return new AdminRecommendation
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            RecommendationType = reader.GetString(reader.GetOrdinal("recommendation_type")),
            Title = reader.GetString(reader.GetOrdinal("title")),
            Description = reader.GetString(reader.GetOrdinal("description")),
            Priority = reader.GetInt32(reader.GetOrdinal("priority")),
            ConfidenceScore = reader.GetDecimal(reader.GetOrdinal("confidence_score")),
            IsRead = reader.GetBoolean(reader.GetOrdinal("is_read")),
            IsImplemented = reader.GetBoolean(reader.GetOrdinal("is_implemented")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            Metadata = reader.IsDBNull(reader.GetOrdinal("metadata")) ? null : reader.GetString(reader.GetOrdinal("metadata"))
        };
    }

    /// <summary>
    /// Verifica si ya existe una recomendación similar (mismo tipo y título)
    /// </summary>
    public async Task<bool> RecommendationExistsAsync(string type, string title)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"
                SELECT COUNT(*) FROM admin_recommendations 
                WHERE recommendation_type = @type 
                AND title = @title 
                AND is_implemented = 0";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@type", type);
            command.Parameters.AddWithValue("@title", title);

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error verificando duplicado: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Elimina todas las recomendaciones según el filtro
    /// </summary>
    /// <param name="filter">pending, read, implemented, o all</param>
    public async Task<int> DeleteRecommendationsAsync(string filter)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            string whereClause = filter switch
            {
                "pending" => "WHERE is_read = 0 AND is_implemented = 0",
                "read" => "WHERE is_read = 1 AND is_implemented = 0",
                "implemented" => "WHERE is_implemented = 1",
                "all" => "",
                _ => "WHERE 1=0" // No eliminar nada si filtro inválido
            };

            var query = $"DELETE FROM admin_recommendations {whereClause}";

            using var command = new SqlCommand(query, connection);
            var rowsAffected = await command.ExecuteNonQueryAsync();

            return rowsAffected;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error eliminando recomendaciones: {ex.Message}");
            return 0;
        }
    }

    #endregion

    #region ========== GESTIÓN DE PATRONES (CRUD) ==========

    /// <summary>
    /// Verifica si ya existe un patrón similar
    /// </summary>
    public async Task<bool> PatternExistsAsync(string patternType, string patternName)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"
                SELECT COUNT(*) FROM reading_patterns 
                WHERE pattern_type = @type 
                AND pattern_name = @name";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@type", patternType);
            command.Parameters.AddWithValue("@name", patternName);

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error verificando patrón duplicado: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Elimina todos los patrones
    /// </summary>
    public async Task<int> DeleteAllPatternsAsync()
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = "DELETE FROM reading_patterns";

            using var command = new SqlCommand(query, connection);
            var rowsAffected = await command.ExecuteNonQueryAsync();

            return rowsAffected;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error eliminando patrones: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Actualiza un patrón existente en lugar de crear duplicado
    /// </summary>
    public async Task<bool> UpdatePatternAsync(string patternType, string patternName, string patternValue, decimal confidence, int frequency)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"
                UPDATE reading_patterns 
                SET pattern_value = @value, 
                    confidence = @confidence, 
                    frequency = @frequency,
                    identified_at = @identifiedAt
                WHERE pattern_type = @type AND pattern_name = @name";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@type", patternType);
            command.Parameters.AddWithValue("@name", patternName);
            command.Parameters.AddWithValue("@value", patternValue);
            command.Parameters.AddWithValue("@confidence", confidence);
            command.Parameters.AddWithValue("@frequency", frequency);
            command.Parameters.AddWithValue("@identifiedAt", DateTime.Now);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error actualizando patrón: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Guarda un patrón en la base de datos (actualiza si existe, crea si no)
    /// </summary>
    public async Task<bool> SavePatternAsync(ReadingPattern pattern)
    {
        try
        {
            // Verificar si ya existe el patrón
            if (await PatternExistsAsync(pattern.PatternType, pattern.PatternName))
            {
                // Actualizar el existente en lugar de crear duplicado
                return await UpdatePatternAsync(
                    pattern.PatternType,
                    pattern.PatternName,
                    pattern.PatternValue,
                    pattern.Confidence,
                    pattern.Frequency
                );
            }

            // Crear nuevo patrón
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"
                INSERT INTO reading_patterns 
                    (pattern_type, pattern_name, pattern_value, frequency, confidence, identified_at)
                VALUES 
                    (@type, @name, @value, @frequency, @confidence, @identifiedAt)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@type", pattern.PatternType);
            command.Parameters.AddWithValue("@name", pattern.PatternName);
            command.Parameters.AddWithValue("@value", pattern.PatternValue);
            command.Parameters.AddWithValue("@frequency", pattern.Frequency);
            command.Parameters.AddWithValue("@confidence", pattern.Confidence);
            command.Parameters.AddWithValue("@identifiedAt", pattern.IdentifiedAt);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error guardando patrón: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtiene los patrones identificados
    /// </summary>
    public async Task<List<ReadingPattern>> GetPatternsAsync(string patternType = null, int limit = 50)
    {
        var patterns = new List<ReadingPattern>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = $@"
                SELECT TOP {limit} *
                FROM reading_patterns
                {(patternType != null ? "WHERE pattern_type = @type" : "")}
                ORDER BY confidence DESC, identified_at DESC";

            using var command = new SqlCommand(query, connection);
            if (patternType != null)
            {
                command.Parameters.AddWithValue("@type", patternType);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                patterns.Add(new ReadingPattern
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    PatternType = reader.GetString(reader.GetOrdinal("pattern_type")),
                    PatternName = reader.GetString(reader.GetOrdinal("pattern_name")),
                    PatternValue = reader.GetString(reader.GetOrdinal("pattern_value")),
                    Frequency = reader.GetInt32(reader.GetOrdinal("frequency")),
                    Confidence = reader.GetDecimal(reader.GetOrdinal("confidence")),
                    IdentifiedAt = reader.GetDateTime(reader.GetOrdinal("identified_at")),
                    Metadata = reader.IsDBNull(reader.GetOrdinal("metadata")) ? null : reader.GetString(reader.GetOrdinal("metadata"))
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo patrones: {ex.Message}");
        }

        return patterns;
    }

    #endregion

    #region ========== MOTOR DE RECOMENDACIONES ==========

    /// <summary>
    /// Ejecuta el análisis completo y genera todas las recomendaciones
    /// Este es el método principal que debe llamarse periódicamente
    /// </summary>
    public async Task<List<AdminRecommendation>> GenerateAllRecommendationsAsync()
    {
        var recommendations = new List<AdminRecommendation>();

        try
        {
            // 1. Analizar géneros y generar recomendaciones
            var genreRecommendations = await AnalyzeGenresAndRecommendAsync();
            recommendations.AddRange(genreRecommendations);

            // 2. Analizar autores y generar recomendaciones
            var authorRecommendations = await AnalyzeAuthorsAndRecommendAsync();
            recommendations.AddRange(authorRecommendations);

            // 3. Analizar calidad/ratings y generar recomendaciones
            var qualityRecommendations = await AnalyzeQualityAndRecommendAsync();
            recommendations.AddRange(qualityRecommendations);

            // 4. Analizar longitud de novelas y generar recomendaciones
            var lengthRecommendations = await AnalyzeLengthPreferencesAsync();
            recommendations.AddRange(lengthRecommendations);

            // 5. Analizar tasa de abandono y generar recomendaciones
            var abandonmentRecommendations = await AnalyzeAbandonmentAndRecommendAsync();
            recommendations.AddRange(abandonmentRecommendations);

            // Guardar todas las recomendaciones en la base de datos
            foreach (var rec in recommendations)
            {
                await SaveRecommendationAsync(rec);
            }

            System.Diagnostics.Debug.WriteLine($"Se generaron {recommendations.Count} recomendaciones");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generando recomendaciones: {ex.Message}");
        }

        // 6. Recomendaciones por etiquetas populares
        await GenerateTagRecommendationsAsync();

        return recommendations;
    }

    /// <summary>
    /// Genera recomendaciones basadas en etiquetas populares
    /// </summary>
    private async Task GenerateTagRecommendationsAsync()
    {
        try
        {
            var tagStats = await _tagService.GetTagStatsAsync(10);

            foreach (var tag in tagStats)
            {
                // Si una etiqueta tiene muchos votos pero pocas novelas, recomendar agregar más
                if (tag.TotalVotes >= 5 && tag.NovelCount <= 2)
                {
                    var ratio = (decimal)tag.TotalVotes / tag.NovelCount;

                    var recommendation = new AdminRecommendation
                    {
                        RecommendationType = "tag_demand",
                        Title = $"Alta demanda: etiqueta '{tag.TagName}'",
                        Description = $"La etiqueta '{tag.TagName}' tiene {tag.TotalVotes} votos pero solo {tag.NovelCount} novela(s). " +
                                      $"Los usuarios buscan más contenido con esta característica.",
                        Priority = ratio > 5 ? 3 : (ratio > 2 ? 2 : 1),
                        ConfidenceScore = Math.Min(0.5m + (tag.TotalVotes * 0.05m), 0.95m),
                        CreatedAt = DateTime.Now,
                        Metadata = $"{{\"tag\":\"{tag.TagName}\",\"votes\":{tag.TotalVotes},\"novels\":{tag.NovelCount}}}"
                    };

                    await SaveRecommendationAsync(recommendation);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generando recomendaciones de etiquetas: {ex.Message}");
        }
    }

    /// <summary>
    /// Analiza los géneros y genera recomendaciones basadas en popularidad
    /// </summary>
    private async Task<List<AdminRecommendation>> AnalyzeGenresAndRecommendAsync()
    {
        var recommendations = new List<AdminRecommendation>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Query para encontrar géneros con alta demanda pero pocas novelas
            var query = @"
                SELECT 
                    g.name as genre_name,
                    COUNT(DISTINCT ng.novel_id) as novel_count,
                    COUNT(DISTINCT ul.user_id) as user_interest,
                    COUNT(rh.id) as read_count
                FROM genres g
                LEFT JOIN novel_genres ng ON g.id = ng.genre_id
                LEFT JOIN novels n ON ng.novel_id = n.id
                LEFT JOIN user_library ul ON n.id = ul.novel_id
                LEFT JOIN reading_history rh ON n.id = rh.novel_id
                GROUP BY g.id, g.name
                HAVING COUNT(rh.id) > 0
                ORDER BY (COUNT(rh.id) * 1.0 / NULLIF(COUNT(DISTINCT ng.novel_id), 0)) DESC";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            var genreData = new List<(string Name, int NovelCount, int UserInterest, int ReadCount)>();
            while (await reader.ReadAsync())
            {
                genreData.Add((
                    reader.GetString(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3)
                ));
            }
            reader.Close();

            // Analizar y generar recomendaciones
            foreach (var genre in genreData.Take(5))
            {
                // Si hay alta demanda (muchas lecturas) pero pocas novelas
                if (genre.ReadCount > 0 && genre.NovelCount < 10)
                {
                    var ratio = genre.NovelCount > 0 ? (decimal)genre.ReadCount / genre.NovelCount : genre.ReadCount;
                    var confidence = Math.Min(0.95m, 0.5m + (ratio / 100));

                    recommendations.Add(new AdminRecommendation
                    {
                        RecommendationType = RecommendationTypes.Genre,
                        Title = $"Agregar más novelas de {genre.Name}",
                        Description = $"El género '{genre.Name}' tiene alta demanda con {genre.ReadCount} lecturas " +
                                     $"pero solo {genre.NovelCount} novelas disponibles. " +
                                     $"Ratio de lectura por novela: {ratio:F1}. " +
                                     $"Se recomienda expandir el catálogo de este género.",
                        Priority = ratio > 10 ? PriorityLevels.High : (ratio > 5 ? PriorityLevels.Medium : PriorityLevels.Low),
                        ConfidenceScore = confidence,
                        Metadata = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            genre = genre.Name,
                            novelCount = genre.NovelCount,
                            readCount = genre.ReadCount,
                            ratio = ratio
                        })
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error analizando géneros: {ex.Message}");
        }

        return recommendations;
    }

    /// <summary>
    /// Analiza los autores y genera recomendaciones basadas en engagement
    /// </summary>
    private async Task<List<AdminRecommendation>> AnalyzeAuthorsAndRecommendAsync()
    {
        var recommendations = new List<AdminRecommendation>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Query para encontrar autores con alto engagement
            var query = @"
                SELECT 
                    n.author,
                    COUNT(DISTINCT n.id) as novel_count,
                    COUNT(rh.id) as read_count,
                    AVG(CAST(n.rating AS DECIMAL(3,2))) as avg_rating,
                    COUNT(DISTINCT rh.user_id) as unique_readers
                FROM novels n
                LEFT JOIN reading_history rh ON n.id = rh.novel_id
                WHERE n.author IS NOT NULL AND n.author != ''
                GROUP BY n.author
                HAVING COUNT(rh.id) > 0
                ORDER BY COUNT(rh.id) DESC";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            var authorData = new List<(string Author, int NovelCount, int ReadCount, decimal AvgRating, int UniqueReaders)>();
            while (await reader.ReadAsync())
            {
                authorData.Add((
                    reader.GetString(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                    reader.GetInt32(4)
                ));
            }
            reader.Close();

            // Generar recomendaciones para autores con alto engagement
            foreach (var author in authorData.Take(3))
            {
                if (author.ReadCount > 5 && author.AvgRating >= 3.5m)
                {
                    var confidence = Math.Min(0.95m, 0.6m + (author.AvgRating / 10));

                    recommendations.Add(new AdminRecommendation
                    {
                        RecommendationType = RecommendationTypes.Author,
                        Title = $"El autor '{author.Author}' tiene alto engagement",
                        Description = $"El autor '{author.Author}' tiene excelente recepción: " +
                                     $"{author.ReadCount} lecturas totales, rating promedio de {author.AvgRating:F1}/5, " +
                                     $"y {author.UniqueReaders} lectores únicos. " +
                                     $"Actualmente tiene {author.NovelCount} novela(s) en el catálogo. " +
                                     $"Se recomienda agregar más obras de este autor.",
                        Priority = author.AvgRating >= 4.0m ? PriorityLevels.High : PriorityLevels.Medium,
                        ConfidenceScore = confidence,
                        Metadata = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            author = author.Author,
                            novelCount = author.NovelCount,
                            readCount = author.ReadCount,
                            avgRating = author.AvgRating,
                            uniqueReaders = author.UniqueReaders
                        })
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error analizando autores: {ex.Message}");
        }

        return recommendations;
    }

    /// <summary>
    /// Analiza la calidad/ratings y genera recomendaciones
    /// </summary>
    private async Task<List<AdminRecommendation>> AnalyzeQualityAndRecommendAsync()
    {
        var recommendations = new List<AdminRecommendation>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Query para encontrar novelas con bajo rating y alto abandono
            var query = @"
                SELECT 
                    n.id,
                    n.title,
                    n.rating,
                    COUNT(CASE WHEN ul.reading_status = 'dropped' THEN 1 END) as dropped_count,
                    COUNT(ul.id) as total_in_library
                FROM novels n
                LEFT JOIN user_library ul ON n.id = ul.novel_id
                WHERE n.rating < 3.5 AND n.rating > 0
                GROUP BY n.id, n.title, n.rating
                HAVING COUNT(ul.id) > 0
                ORDER BY n.rating ASC";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            var lowRatedNovels = new List<(int Id, string Title, decimal Rating, int DroppedCount, int TotalInLibrary)>();
            while (await reader.ReadAsync())
            {
                lowRatedNovels.Add((
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetDecimal(2),
                    reader.GetInt32(3),
                    reader.GetInt32(4)
                ));
            }
            reader.Close();

            // Si hay novelas con bajo rating
            if (lowRatedNovels.Count > 0)
            {
                var avgLowRating = lowRatedNovels.Average(n => n.Rating);
                var totalDropped = lowRatedNovels.Sum(n => n.DroppedCount);
                var dropRate = lowRatedNovels.Sum(n => n.TotalInLibrary) > 0
                    ? (decimal)totalDropped / lowRatedNovels.Sum(n => n.TotalInLibrary) * 100
                    : 0;

                recommendations.Add(new AdminRecommendation
                {
                    RecommendationType = RecommendationTypes.Quality,
                    Title = "Novelas con bajo rating tienen alto abandono",
                    Description = $"Se identificaron {lowRatedNovels.Count} novelas con rating menor a 3.5. " +
                                 $"Rating promedio: {avgLowRating:F1}/5. " +
                                 $"Tasa de abandono en estas novelas: {dropRate:F1}%. " +
                                 $"Se recomienda revisar la calidad del contenido o considerar remover " +
                                 $"las novelas con peor desempeño.",
                    Priority = dropRate > 50 ? PriorityLevels.High : PriorityLevels.Medium,
                    ConfidenceScore = 0.85m,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        lowRatedCount = lowRatedNovels.Count,
                        avgRating = avgLowRating,
                        dropRate = dropRate,
                        novels = lowRatedNovels.Take(5).Select(n => new { n.Title, n.Rating })
                    })
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error analizando calidad: {ex.Message}");
        }

        return recommendations;
    }

    /// <summary>
    /// Analiza las preferencias de longitud de novelas
    /// </summary>
    private async Task<List<AdminRecommendation>> AnalyzeLengthPreferencesAsync()
    {
        var recommendations = new List<AdminRecommendation>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Query para analizar preferencias de longitud
            var query = @"
                SELECT 
                    CASE 
                        WHEN n.chapter_count <= 50 THEN 'Corta (1-50)'
                        WHEN n.chapter_count <= 100 THEN 'Media (51-100)'
                        WHEN n.chapter_count <= 200 THEN 'Larga (101-200)'
                        ELSE 'Muy Larga (200+)'
                    END as length_category,
                    COUNT(DISTINCT n.id) as novel_count,
                    COUNT(rh.id) as read_count,
                    COUNT(CASE WHEN ul.reading_status = 'completed' THEN 1 END) as completed_count
                FROM novels n
                LEFT JOIN reading_history rh ON n.id = rh.novel_id
                LEFT JOIN user_library ul ON n.id = ul.novel_id
                GROUP BY 
                    CASE 
                        WHEN n.chapter_count <= 50 THEN 'Corta (1-50)'
                        WHEN n.chapter_count <= 100 THEN 'Media (51-100)'
                        WHEN n.chapter_count <= 200 THEN 'Larga (101-200)'
                        ELSE 'Muy Larga (200+)'
                    END
                ORDER BY read_count DESC";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            var lengthData = new List<(string Category, int NovelCount, int ReadCount, int CompletedCount)>();
            while (await reader.ReadAsync())
            {
                lengthData.Add((
                    reader.GetString(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3)
                ));
            }
            reader.Close();

            // Encontrar la categoría más popular
            if (lengthData.Count > 0)
            {
                var mostPopular = lengthData.OrderByDescending(l => l.ReadCount).First();
                var completionRate = mostPopular.ReadCount > 0
                    ? (decimal)mostPopular.CompletedCount / mostPopular.ReadCount * 100
                    : 0;

                recommendations.Add(new AdminRecommendation
                {
                    RecommendationType = RecommendationTypes.Content,
                    Title = $"Los usuarios prefieren novelas de longitud '{mostPopular.Category}'",
                    Description = $"Análisis de preferencias de longitud: " +
                                 $"Las novelas '{mostPopular.Category}' capítulos son las más leídas " +
                                 $"con {mostPopular.ReadCount} lecturas. " +
                                 $"Tasa de finalización: {completionRate:F1}%. " +
                                 $"Se recomienda priorizar novelas de esta longitud al expandir el catálogo.",
                    Priority = PriorityLevels.Medium,
                    ConfidenceScore = 0.75m,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        preferredLength = mostPopular.Category,
                        readCount = mostPopular.ReadCount,
                        completionRate = completionRate,
                        allCategories = lengthData
                    })
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error analizando longitud: {ex.Message}");
        }

        return recommendations;
    }

    /// <summary>
    /// Analiza la tasa de abandono y genera recomendaciones
    /// </summary>
    private async Task<List<AdminRecommendation>> AnalyzeAbandonmentAndRecommendAsync()
    {
        var recommendations = new List<AdminRecommendation>();

        try
        {
            var abandonmentRate = await CalculateAbandonmentRateAsync();

            if (abandonmentRate > 30)
            {
                recommendations.Add(new AdminRecommendation
                {
                    RecommendationType = RecommendationTypes.Quality,
                    Title = "Tasa de abandono elevada detectada",
                    Description = $"La tasa de abandono actual es del {abandonmentRate:F1}%, " +
                                 $"lo cual está por encima del umbral recomendado (30%). " +
                                 $"Esto puede indicar problemas con la calidad del contenido, " +
                                 $"la experiencia de lectura, o que las novelas no cumplen " +
                                 $"las expectativas de los usuarios. " +
                                 $"Se recomienda investigar las causas y tomar medidas correctivas.",
                    Priority = abandonmentRate > 50 ? PriorityLevels.High : PriorityLevels.Medium,
                    ConfidenceScore = 0.90m,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        abandonmentRate = abandonmentRate,
                        threshold = 30,
                        severity = abandonmentRate > 50 ? "Alta" : "Media"
                    })
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error analizando abandono: {ex.Message}");
        }

        return recommendations;
    }

    #endregion

    #region ========== IDENTIFICACIÓN DE PATRONES ==========

    /// <summary>
    /// Ejecuta el análisis completo y genera todos los patrones
    /// </summary>
    public async Task<List<ReadingPattern>> IdentifyAllPatternsAsync()
    {
        var patterns = new List<ReadingPattern>();

        try
        {
            // 1. Identificar patrones de preferencia de contenido
            var contentPatterns = await IdentifyContentPreferencePatternsAsync();
            patterns.AddRange(contentPatterns);

            // 2. Identificar patrones de finalización
            var completionPatterns = await IdentifyCompletionPatternsAsync();
            patterns.AddRange(completionPatterns);

            // Guardar todos los patrones en la base de datos
            foreach (var pattern in patterns)
            {
                await SavePatternAsync(pattern);
            }

            System.Diagnostics.Debug.WriteLine($"Se identificaron {patterns.Count} patrones");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error identificando patrones: {ex.Message}");
        }

        return patterns;
    }

    /// <summary>
    /// Identifica patrones de preferencia de contenido
    /// </summary>
    private async Task<List<ReadingPattern>> IdentifyContentPreferencePatternsAsync()
    {
        var patterns = new List<ReadingPattern>();

        try
        {
            // Obtener géneros populares
            var popularGenres = await GetPopularGenresStatsAsync(5);

            if (popularGenres.Count > 0)
            {
                var topGenre = popularGenres.First();
                patterns.Add(new ReadingPattern
                {
                    PatternType = PatternTypes.ContentPreference,
                    PatternName = "Género más popular",
                    PatternValue = $"El género '{topGenre.Genre}' es el más popular con " +
                                  $"{topGenre.ReadCount} lecturas y rating promedio de {topGenre.AvgRating:F1}",
                    Frequency = topGenre.ReadCount,
                    Confidence = 0.85m,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        genre = topGenre.Genre,
                        readCount = topGenre.ReadCount,
                        avgRating = topGenre.AvgRating
                    })
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error identificando patrones de contenido: {ex.Message}");
        }

        return patterns;
    }

    /// <summary>
    /// Identifica patrones de finalización de novelas
    /// </summary>
    private async Task<List<ReadingPattern>> IdentifyCompletionPatternsAsync()
    {
        var patterns = new List<ReadingPattern>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Query para analizar tasa de finalización
            var query = @"
                SELECT 
                    COUNT(*) as total,
                    COUNT(CASE WHEN reading_status = 'completed' THEN 1 END) as completed,
                    COUNT(CASE WHEN reading_status = 'reading' THEN 1 END) as reading,
                    COUNT(CASE WHEN reading_status = 'dropped' THEN 1 END) as dropped,
                    COUNT(CASE WHEN reading_status = 'plan_to_read' THEN 1 END) as plan_to_read
                FROM user_library";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var total = reader.GetInt32(0);
                var completed = reader.GetInt32(1);
                var reading = reader.GetInt32(2);
                var dropped = reader.GetInt32(3);
                var planToRead = reader.GetInt32(4);

                if (total > 0)
                {
                    var completionRate = (decimal)completed / total * 100;
                    var dropRate = (decimal)dropped / total * 100;

                    patterns.Add(new ReadingPattern
                    {
                        PatternType = PatternTypes.CompletionPattern,
                        PatternName = "Distribución de estados de lectura",
                        PatternValue = $"De {total} novelas en bibliotecas: " +
                                      $"{completionRate:F1}% completadas, " +
                                      $"{(decimal)reading / total * 100:F1}% en lectura, " +
                                      $"{dropRate:F1}% abandonadas, " +
                                      $"{(decimal)planToRead / total * 100:F1}% planeadas",
                        Frequency = total,
                        Confidence = 0.95m,
                        Metadata = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            total,
                            completed,
                            reading,
                            dropped,
                            planToRead,
                            completionRate,
                            dropRate
                        })
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error identificando patrones de finalización: {ex.Message}");
        }

        return patterns;
    }

    #endregion
}