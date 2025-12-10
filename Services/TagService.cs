using NovelBook.Models;
using Microsoft.Data.SqlClient;

namespace NovelBook.Services;

/// <summary>
/// Servicio para gestionar etiquetas de novelas
/// </summary>
public class TagService
{
    private readonly DatabaseService _database;
    private const int MAX_TAG_LENGTH = 25;
    private const int MAX_TAGS_PER_USER_PER_NOVEL = 3;
    private const int DELETE_WINDOW_HOURS = 24;

    public TagService(DatabaseService database)
    {
        _database = database;
    }

    #region ========== OBTENER ETIQUETAS ==========

    /// <summary>
    /// Obtiene todas las etiquetas de una novela ordenadas por votos
    /// </summary>
    public async Task<List<NovelTag>> GetTagsByNovelAsync(int novelId, int? currentUserId = null)
    {
        var tags = new List<NovelTag>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"
                SELECT 
                    t.id,
                    t.novel_id,
                    t.user_id,
                    t.tag_name,
                    t.created_at,
                    t.is_active,
                    u.name as creator_name,
                    (SELECT COUNT(*) FROM tag_votes WHERE tag_id = t.id) as vote_count,
                    CASE WHEN EXISTS(
                        SELECT 1 FROM tag_votes 
                        WHERE tag_id = t.id AND user_id = @currentUserId
                    ) THEN 1 ELSE 0 END as user_has_voted
                FROM novel_tags t
                INNER JOIN users u ON t.user_id = u.id
                WHERE t.novel_id = @novelId AND t.is_active = 1
                ORDER BY vote_count DESC, t.created_at ASC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@novelId", novelId);
            command.Parameters.AddWithValue("@currentUserId", currentUserId ?? 0);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tags.Add(new NovelTag
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    NovelId = reader.GetInt32(reader.GetOrdinal("novel_id")),
                    UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                    TagName = reader.GetString(reader.GetOrdinal("tag_name")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatorName = reader.GetString(reader.GetOrdinal("creator_name")),
                    VoteCount = reader.GetInt32(reader.GetOrdinal("vote_count")),
                    UserHasVoted = reader.GetInt32(reader.GetOrdinal("user_has_voted")) == 1
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo etiquetas: {ex.Message}");
        }

        return tags;
    }

    /// <summary>
    /// Obtiene las etiquetas más populares del sistema
    /// </summary>
    public async Task<List<(string TagName, int Count)>> GetPopularTagsAsync(int limit = 20)
    {
        var tags = new List<(string, int)>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"
                SELECT TOP (@limit) 
                    tag_name, 
                    COUNT(*) as usage_count
                FROM novel_tags
                WHERE is_active = 1
                GROUP BY tag_name
                ORDER BY usage_count DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@limit", limit);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tags.Add((
                    reader.GetString(0),
                    reader.GetInt32(1)
                ));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo etiquetas populares: {ex.Message}");
        }

        return tags;
    }

    /// <summary>
    /// Busca novelas por etiqueta
    /// </summary>
    public async Task<List<int>> GetNovelIdsByTagAsync(string tagName)
    {
        var novelIds = new List<int>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"
                SELECT DISTINCT novel_id 
                FROM novel_tags 
                WHERE tag_name = @tagName AND is_active = 1";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@tagName", tagName);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                novelIds.Add(reader.GetInt32(0));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error buscando por etiqueta: {ex.Message}");
        }

        return novelIds;
    }

    #endregion

    #region ========== CREAR/ELIMINAR ETIQUETAS ==========

    /// <summary>
    /// Agrega una nueva etiqueta a una novela
    /// </summary>
    public async Task<(bool Success, string Message)> AddTagAsync(int novelId, int userId, string tagName)
    {
        // Validaciones
        tagName = tagName?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(tagName))
            return (false, "La etiqueta no puede estar vacía");

        if (tagName.Length > MAX_TAG_LENGTH)
            return (false, $"La etiqueta no puede tener más de {MAX_TAG_LENGTH} caracteres");

        // Normalizar: primera letra mayúscula, resto minúscula
        tagName = char.ToUpper(tagName[0]) + tagName.Substring(1).ToLower();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Verificar si el usuario ya agregó el máximo de etiquetas a esta novela
            var countQuery = @"
                SELECT COUNT(*) FROM novel_tags 
                WHERE novel_id = @novelId AND user_id = @userId AND is_active = 1";

            using var countCmd = new SqlCommand(countQuery, connection);
            countCmd.Parameters.AddWithValue("@novelId", novelId);
            countCmd.Parameters.AddWithValue("@userId", userId);

            var userTagCount = (int)await countCmd.ExecuteScalarAsync();
            if (userTagCount >= MAX_TAGS_PER_USER_PER_NOVEL)
                return (false, $"Solo puedes agregar {MAX_TAGS_PER_USER_PER_NOVEL} etiquetas por novela");

            // Verificar si la etiqueta ya existe para esta novela
            var existsQuery = @"
                SELECT id FROM novel_tags 
                WHERE novel_id = @novelId AND tag_name = @tagName AND is_active = 1";

            using var existsCmd = new SqlCommand(existsQuery, connection);
            existsCmd.Parameters.AddWithValue("@novelId", novelId);
            existsCmd.Parameters.AddWithValue("@tagName", tagName);

            var existingTagId = await existsCmd.ExecuteScalarAsync();
            if (existingTagId != null)
            {
                // La etiqueta ya existe, agregar voto en su lugar
                var voteResult = await VoteTagAsync((int)existingTagId, userId);
                if (voteResult)
                    return (true, "Etiqueta ya existe. ¡Tu voto fue agregado!");
                else
                    return (false, "Ya votaste por esta etiqueta");
            }

            // Insertar nueva etiqueta
            var insertQuery = @"
                INSERT INTO novel_tags (novel_id, user_id, tag_name, created_at, is_active)
                VALUES (@novelId, @userId, @tagName, GETDATE(), 1)";

            using var insertCmd = new SqlCommand(insertQuery, connection);
            insertCmd.Parameters.AddWithValue("@novelId", novelId);
            insertCmd.Parameters.AddWithValue("@userId", userId);
            insertCmd.Parameters.AddWithValue("@tagName", tagName);

            await insertCmd.ExecuteNonQueryAsync();
            return (true, "Etiqueta agregada exitosamente");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error agregando etiqueta: {ex.Message}");
            return (false, "Error al agregar etiqueta");
        }
    }

    /// <summary>
    /// Elimina una etiqueta (solo creador dentro de 24h o admin)
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteTagAsync(int tagId, int userId, bool isAdmin)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Obtener información de la etiqueta
            var getQuery = "SELECT user_id, created_at FROM novel_tags WHERE id = @tagId";
            using var getCmd = new SqlCommand(getQuery, connection);
            getCmd.Parameters.AddWithValue("@tagId", tagId);

            using var reader = await getCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return (false, "Etiqueta no encontrada");

            var creatorId = reader.GetInt32(0);
            var createdAt = reader.GetDateTime(1);
            reader.Close();

            // Verificar permisos
            if (!isAdmin)
            {
                if (creatorId != userId)
                    return (false, "Solo el creador o un administrador puede eliminar esta etiqueta");

                var hoursSinceCreation = (DateTime.Now - createdAt).TotalHours;
                if (hoursSinceCreation > DELETE_WINDOW_HOURS)
                    return (false, $"Solo puedes eliminar etiquetas dentro de las primeras {DELETE_WINDOW_HOURS} horas");
            }

            // Eliminar (soft delete)
            var deleteQuery = "UPDATE novel_tags SET is_active = 0 WHERE id = @tagId";
            using var deleteCmd = new SqlCommand(deleteQuery, connection);
            deleteCmd.Parameters.AddWithValue("@tagId", tagId);

            await deleteCmd.ExecuteNonQueryAsync();
            return (true, "Etiqueta eliminada");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error eliminando etiqueta: {ex.Message}");
            return (false, "Error al eliminar etiqueta");
        }
    }

    #endregion

    #region ========== SISTEMA DE VOTOS ==========

    /// <summary>
    /// Agrega o quita voto de una etiqueta
    /// </summary>
    public async Task<bool> VoteTagAsync(int tagId, int userId)
    {
        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Verificar si ya votó
            var checkQuery = "SELECT id FROM tag_votes WHERE tag_id = @tagId AND user_id = @userId";
            using var checkCmd = new SqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("@tagId", tagId);
            checkCmd.Parameters.AddWithValue("@userId", userId);

            var existingVote = await checkCmd.ExecuteScalarAsync();

            if (existingVote != null)
            {
                // Ya votó, quitar voto
                var removeQuery = "DELETE FROM tag_votes WHERE tag_id = @tagId AND user_id = @userId";
                using var removeCmd = new SqlCommand(removeQuery, connection);
                removeCmd.Parameters.AddWithValue("@tagId", tagId);
                removeCmd.Parameters.AddWithValue("@userId", userId);
                await removeCmd.ExecuteNonQueryAsync();
                return false; // Voto removido
            }
            else
            {
                // No ha votado, agregar voto
                var addQuery = "INSERT INTO tag_votes (tag_id, user_id, voted_at) VALUES (@tagId, @userId, GETDATE())";
                using var addCmd = new SqlCommand(addQuery, connection);
                addCmd.Parameters.AddWithValue("@tagId", tagId);
                addCmd.Parameters.AddWithValue("@userId", userId);
                await addCmd.ExecuteNonQueryAsync();
                return true; // Voto agregado
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error votando etiqueta: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region ========== ANALYTICS (para módulo inteligente) ==========

    /// <summary>
    /// Obtiene estadísticas de etiquetas para analytics
    /// </summary>
    public async Task<List<(string TagName, int NovelCount, int TotalVotes)>> GetTagStatsAsync(int limit = 10)
    {
        var stats = new List<(string, int, int)>();

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"
                SELECT TOP (@limit)
                    t.tag_name,
                    COUNT(DISTINCT t.novel_id) as novel_count,
                    (SELECT COUNT(*) FROM tag_votes tv 
                     INNER JOIN novel_tags nt ON tv.tag_id = nt.id 
                     WHERE nt.tag_name = t.tag_name) as total_votes
                FROM novel_tags t
                WHERE t.is_active = 1
                GROUP BY t.tag_name
                ORDER BY novel_count DESC, total_votes DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@limit", limit);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                stats.Add((
                    reader.GetString(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2)
                ));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo stats de etiquetas: {ex.Message}");
        }

        return stats;
    }

    #endregion
}