using Microsoft.Data.SqlClient;
using NovelBook.Models;

namespace NovelBook.Services;

/// <summary>
/// Servicio para manejar todas las operaciones relacionadas con reseñas y calificaciones
/// Este servicio se encarga de crear, leer, actualizar y eliminar reseñas de novelas
/// </summary>
public class ReviewService
{
    private readonly DatabaseService _database;

    /// <summary>
    /// Constructor que recibe el servicio de base de datos
    /// </summary>
    /// <param name="database">Instancia del servicio de base de datos</param>
    public ReviewService(DatabaseService database)
    {
        _database = database;
    }

    /// <summary>
    /// Obtiene todas las reseñas de una novela específica
    /// </summary>
    /// <param name="novelId">ID de la novela</param>
    /// <returns>Lista de reseñas con información del usuario</returns>
    public async Task<List<Review>> GetNovelReviewsAsync(int novelId)
    {
        var reviews = new List<Review>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Query que obtiene las reseñas junto con el nombre del usuario
            var query = @"SELECT r.*, u.name as user_name 
                         FROM reviews r
                         INNER JOIN users u ON r.user_id = u.id
                         WHERE r.novel_id = @novelId
                         ORDER BY r.created_at DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@novelId", novelId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                reviews.Add(new Review
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                    NovelId = reader.GetInt32(reader.GetOrdinal("novel_id")),
                    Rating = reader.GetInt32(reader.GetOrdinal("rating")),
                    Comment = reader.IsDBNull(reader.GetOrdinal("comment")) ?
                              null : reader.GetString(reader.GetOrdinal("comment")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                    UserName = reader.GetString(reader.GetOrdinal("user_name"))
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo reseñas: {ex.Message}");
        }

        return reviews;
    }

    /// <summary>
    /// Obtiene la reseña de un usuario específico para una novela
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="novelId">ID de la novela</param>
    /// <returns>La reseña del usuario o null si no existe</returns>
    public async Task<Review> GetUserReviewAsync(int userId, int novelId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT r.*, u.name as user_name 
                         FROM reviews r
                         INNER JOIN users u ON r.user_id = u.id
                         WHERE r.user_id = @userId AND r.novel_id = @novelId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@novelId", novelId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Review
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                    NovelId = reader.GetInt32(reader.GetOrdinal("novel_id")),
                    Rating = reader.GetInt32(reader.GetOrdinal("rating")),
                    Comment = reader.IsDBNull(reader.GetOrdinal("comment")) ?
                              null : reader.GetString(reader.GetOrdinal("comment")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                    UserName = reader.GetString(reader.GetOrdinal("user_name"))
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo reseña del usuario: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Crea o actualiza una reseña
    /// Si el usuario ya tiene una reseña para esta novela, la actualiza
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="novelId">ID de la novela</param>
    /// <param name="rating">Calificación (1-5)</param>
    /// <param name="comment">Comentario opcional</param>
    /// <returns>true si se guardó correctamente</returns>
    public async Task<bool> SaveReviewAsync(int userId, int novelId, int rating, string comment)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Verificar si ya existe una reseña de este usuario para esta novela
            var checkQuery = @"SELECT COUNT(*) FROM reviews 
                              WHERE user_id = @userId AND novel_id = @novelId";

            using var checkCommand = new SqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@userId", userId);
            checkCommand.Parameters.AddWithValue("@novelId", novelId);

            var exists = (int)await checkCommand.ExecuteScalarAsync() > 0;

            string query;
            if (exists)
            {
                // Si existe, actualizar
                query = @"UPDATE reviews 
                         SET rating = @rating, 
                             comment = @comment, 
                             updated_at = GETDATE()
                         WHERE user_id = @userId AND novel_id = @novelId";
            }
            else
            {
                // Si no existe, crear nueva
                query = @"INSERT INTO reviews (user_id, novel_id, rating, comment, created_at, updated_at)
                         VALUES (@userId, @novelId, @rating, @comment, GETDATE(), GETDATE())";
            }

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@novelId", novelId);
            command.Parameters.AddWithValue("@rating", rating);
            command.Parameters.AddWithValue("@comment", string.IsNullOrEmpty(comment) ? DBNull.Value : comment);

            await command.ExecuteNonQueryAsync();

            // Actualizar el rating promedio de la novela usando el stored procedure
            await UpdateNovelRatingAsync(novelId, connection);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error guardando reseña: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Elimina una reseña
    /// </summary>
    /// <param name="userId">ID del usuario dueño de la reseña</param>
    /// <param name="reviewId">ID de la reseña</param>
    /// <returns>true si se eliminó correctamente</returns>
    public async Task<bool> DeleteReviewAsync(int userId, int reviewId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Primero obtener el novel_id antes de eliminar
            var getNovelQuery = "SELECT novel_id FROM reviews WHERE id = @reviewId AND user_id = @userId";
            using var getNovelCommand = new SqlCommand(getNovelQuery, connection);
            getNovelCommand.Parameters.AddWithValue("@reviewId", reviewId);
            getNovelCommand.Parameters.AddWithValue("@userId", userId);

            var novelId = await getNovelCommand.ExecuteScalarAsync();
            if (novelId == null) return false;

            // Eliminar la reseña (solo si pertenece al usuario)
            var query = "DELETE FROM reviews WHERE id = @reviewId AND user_id = @userId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@reviewId", reviewId);
            command.Parameters.AddWithValue("@userId", userId);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                // Actualizar el rating promedio de la novela
                await UpdateNovelRatingAsync((int)novelId, connection);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error eliminando reseña: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtiene estadísticas de las reseñas de una novela
    /// </summary>
    /// <param name="novelId">ID de la novela</param>
    /// <returns>Estadísticas con total de reseñas y distribución de ratings</returns>
    public async Task<ReviewStats> GetReviewStatsAsync(int novelId)
    {
        var stats = new ReviewStats();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Query para obtener la distribución de ratings
            var query = @"SELECT 
                            rating,
                            COUNT(*) as count
                         FROM reviews 
                         WHERE novel_id = @novelId
                         GROUP BY rating";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@novelId", novelId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var rating = reader.GetInt32(reader.GetOrdinal("rating"));
                var count = reader.GetInt32(reader.GetOrdinal("count"));

                stats.RatingDistribution[rating] = count;
                stats.TotalReviews += count;
            }

            // Calcular el promedio ponderado
            if (stats.TotalReviews > 0)
            {
                decimal sum = 0;
                foreach (var kvp in stats.RatingDistribution)
                {
                    sum += kvp.Key * kvp.Value;
                }
                stats.AverageRating = sum / stats.TotalReviews;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo estadísticas: {ex.Message}");
        }

        return stats;
    }

    /// <summary>
    /// Actualiza el rating promedio de una novela
    /// Utiliza el stored procedure UpdateNovelRating
    /// </summary>
    /// <param name="novelId">ID de la novela</param>
    /// <param name="connection">Conexión activa a la base de datos</param>
    private async Task UpdateNovelRatingAsync(int novelId, SqlConnection connection)
    {
        try
        {
            using var command = new SqlCommand("UpdateNovelRating", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@novelId", novelId);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error actualizando rating de novela: {ex.Message}");
        }
    }
}