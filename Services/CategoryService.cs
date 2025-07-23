using Microsoft.Data.SqlClient;
using NovelBook.Models;

namespace NovelBook.Services;

/// <summary>
/// Servicio para manejar categorías personalizadas de usuarios
/// </summary>
public class CategoryService
{
    private readonly DatabaseService _database;

    public CategoryService(DatabaseService database)
    {
        _database = database;
    }

    /// <summary>
    /// Obtiene todas las categorías del usuario actual
    /// </summary>
    public async Task<List<UserCategory>> GetUserCategoriesAsync()
    {
        var categories = new List<UserCategory>();

        // Verificar que hay usuario logueado
        if (AuthService.CurrentUser == null) return categories;

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Query para obtener categorías con conteo de novelas
            var query = @"SELECT 
                            c.*,
                            COUNT(cn.novel_id) as novel_count
                         FROM user_categories c
                         LEFT JOIN category_novels cn ON c.id = cn.category_id
                         WHERE c.user_id = @userId
                         GROUP BY c.id, c.user_id, c.name, c.description, 
                                  c.color, c.icon, c.created_at, c.updated_at
                         ORDER BY c.name";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser.Id);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new UserCategory
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ?
                                 "" : reader.GetString(reader.GetOrdinal("description")),
                    Color = reader.IsDBNull(reader.GetOrdinal("color")) ?
                            "#2196F3" : reader.GetString(reader.GetOrdinal("color")),
                    Icon = reader.IsDBNull(reader.GetOrdinal("icon")) ?
                           "📁" : reader.GetString(reader.GetOrdinal("icon")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                    NovelCount = reader.GetInt32(reader.GetOrdinal("novel_count"))
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo categorías: {ex.Message}");
        }

        return categories;
    }

    /// <summary>
    /// Crea una nueva categoría para el usuario
    /// </summary>
    public async Task<(bool success, string message, int categoryId)> CreateCategoryAsync(UserCategory category)
    {
        if (AuthService.CurrentUser == null)
            return (false, LocalizationService.GetString("MustBeLoggedIn"), 0);

        if (!category.IsValidName())
            return (false, LocalizationService.GetString("NameLengthError"), 0);

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Verificar si ya existe una categoría con ese nombre
            var checkQuery = @"SELECT COUNT(*) FROM user_categories 
                              WHERE user_id = @userId AND name = @name";
            using var checkCommand = new SqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@userId", AuthService.CurrentUser.Id);
            checkCommand.Parameters.AddWithValue("@name", category.Name);

            var exists = (int)await checkCommand.ExecuteScalarAsync() > 0;
            if (exists)
                return (false, LocalizationService.GetString("CategoryNameExists"), 0);

            // Insertar la nueva categoría
            var insertQuery = @"INSERT INTO user_categories 
                               (user_id, name, description, color, icon) 
                               OUTPUT INSERTED.id
                               VALUES (@userId, @name, @description, @color, @icon)";

            using var insertCommand = new SqlCommand(insertQuery, connection);
            insertCommand.Parameters.AddWithValue("@userId", AuthService.CurrentUser.Id);
            insertCommand.Parameters.AddWithValue("@name", category.Name);
            insertCommand.Parameters.AddWithValue("@description",
                string.IsNullOrEmpty(category.Description) ? DBNull.Value : category.Description);
            insertCommand.Parameters.AddWithValue("@color",
                string.IsNullOrEmpty(category.Color) ? "#2196F3" : category.Color);
            insertCommand.Parameters.AddWithValue("@icon",
                string.IsNullOrEmpty(category.Icon) ? "📁" : category.Icon);

            var categoryId = (int)await insertCommand.ExecuteScalarAsync();
            return (true, LocalizationService.GetString("CategoryCreated"), categoryId);
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}", 0);
        }
    }

    /// <summary>
    /// Actualiza una categoría existente
    /// </summary>
    public async Task<(bool success, string message)> UpdateCategoryAsync(UserCategory category)
    {
        if (AuthService.CurrentUser == null)
            return (false, LocalizationService.GetString("MustBeLoggedIn"));

        if (!category.IsValidName())
            return (false, LocalizationService.GetString("NameLengthError"));

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Verificar que la categoría pertenece al usuario
            var checkQuery = @"SELECT COUNT(*) FROM user_categories 
                              WHERE id = @id AND user_id = @userId";
            using var checkCommand = new SqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@id", category.Id);
            checkCommand.Parameters.AddWithValue("@userId", AuthService.CurrentUser.Id);

            var isOwner = (int)await checkCommand.ExecuteScalarAsync() > 0;
            if (!isOwner)
                return (false, LocalizationService.GetString("NoPermissionEdit"));

            // Actualizar la categoría
            var updateQuery = @"UPDATE user_categories 
                               SET name = @name, 
                                   description = @description, 
                                   color = @color, 
                                   icon = @icon,
                                   updated_at = GETDATE()
                               WHERE id = @id";

            using var updateCommand = new SqlCommand(updateQuery, connection);
            updateCommand.Parameters.AddWithValue("@id", category.Id);
            updateCommand.Parameters.AddWithValue("@name", category.Name);
            updateCommand.Parameters.AddWithValue("@description",
                string.IsNullOrEmpty(category.Description) ? DBNull.Value : category.Description);
            updateCommand.Parameters.AddWithValue("@color", category.Color);
            updateCommand.Parameters.AddWithValue("@icon", category.Icon);

            await updateCommand.ExecuteNonQueryAsync();
            return (true, LocalizationService.GetString("CategoryUpdated"));
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Elimina una categoría
    /// </summary>
    public async Task<(bool success, string message)> DeleteCategoryAsync(int categoryId)
    {
        if (AuthService.CurrentUser == null)
            return (false, LocalizationService.GetString("MustBeLoggedIn"));

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Verificar que la categoría pertenece al usuario
            var checkQuery = @"SELECT COUNT(*) FROM user_categories 
                              WHERE id = @id AND user_id = @userId";
            using var checkCommand = new SqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@id", categoryId);
            checkCommand.Parameters.AddWithValue("@userId", AuthService.CurrentUser.Id);

            var isOwner = (int)await checkCommand.ExecuteScalarAsync() > 0;
            if (!isOwner)
                return (false, LocalizationService.GetString("NoPermissionDelete"));

            // Eliminar la categoría (las novelas se eliminarán en cascada)
            var deleteQuery = "DELETE FROM user_categories WHERE id = @id";
            using var deleteCommand = new SqlCommand(deleteQuery, connection);
            deleteCommand.Parameters.AddWithValue("@id", categoryId);

            await deleteCommand.ExecuteNonQueryAsync();
            return (true, LocalizationService.GetString("CategoryDeleted"));
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Agrega una novela a una categoría
    /// </summary>
    public async Task<(bool success, string message)> AddNovelToCategoryAsync(int categoryId, int novelId)
    {
        if (AuthService.CurrentUser == null)
            return (false, LocalizationService.GetString("MustBeLoggedIn"));

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Verificar que la categoría pertenece al usuario
            var checkQuery = @"SELECT COUNT(*) FROM user_categories 
                              WHERE id = @categoryId AND user_id = @userId";
            using var checkCommand = new SqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@categoryId", categoryId);
            checkCommand.Parameters.AddWithValue("@userId", AuthService.CurrentUser.Id);

            var isOwner = (int)await checkCommand.ExecuteScalarAsync() > 0;
            if (!isOwner)
                return (false, LocalizationService.GetString("NoPermissionAdd"));

            // Verificar si la novela ya está en la categoría
            var existsQuery = @"SELECT COUNT(*) FROM category_novels 
                               WHERE category_id = @categoryId AND novel_id = @novelId";
            using var existsCommand = new SqlCommand(existsQuery, connection);
            existsCommand.Parameters.AddWithValue("@categoryId", categoryId);
            existsCommand.Parameters.AddWithValue("@novelId", novelId);

            var exists = (int)await existsCommand.ExecuteScalarAsync() > 0;
            if (exists)
                return (false, LocalizationService.GetString("NovelAlreadyInCategory"));

            // Agregar la novela a la categoría
            var insertQuery = @"INSERT INTO category_novels (category_id, novel_id) 
                               VALUES (@categoryId, @novelId)";
            using var insertCommand = new SqlCommand(insertQuery, connection);
            insertCommand.Parameters.AddWithValue("@categoryId", categoryId);
            insertCommand.Parameters.AddWithValue("@novelId", novelId);

            await insertCommand.ExecuteNonQueryAsync();
            return (true, LocalizationService.GetString("NovelAddedToCategory"));
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Elimina una novela de una categoría
    /// </summary>
    public async Task<(bool success, string message)> RemoveNovelFromCategoryAsync(int categoryId, int novelId)
    {
        if (AuthService.CurrentUser == null)
            return (false, LocalizationService.GetString("MustBeLoggedIn"));

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            // Verificar que la categoría pertenece al usuario
            var checkQuery = @"SELECT COUNT(*) FROM user_categories 
                              WHERE id = @categoryId AND user_id = @userId";
            using var checkCommand = new SqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@categoryId", categoryId);
            checkCommand.Parameters.AddWithValue("@userId", AuthService.CurrentUser.Id);

            var isOwner = (int)await checkCommand.ExecuteScalarAsync() > 0;
            if (!isOwner)
                return (false, LocalizationService.GetString("NoPermissionModify"));

            // Eliminar la novela de la categoría
            var deleteQuery = @"DELETE FROM category_novels 
                               WHERE category_id = @categoryId AND novel_id = @novelId";
            using var deleteCommand = new SqlCommand(deleteQuery, connection);
            deleteCommand.Parameters.AddWithValue("@categoryId", categoryId);
            deleteCommand.Parameters.AddWithValue("@novelId", novelId);

            var rowsAffected = await deleteCommand.ExecuteNonQueryAsync();
            if (rowsAffected == 0)
                return (false, LocalizationService.GetString("NovelNotInCategory"));

            return (true, LocalizationService.GetString("NovelRemovedFromCategory"));
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene las novelas de una categoría específica
    /// </summary>
    public async Task<List<Novel>> GetCategoryNovelsAsync(int categoryId)
    {
        var novels = new List<Novel>();

        if (AuthService.CurrentUser == null) return novels;

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT n.*, STRING_AGG(g.name, ', ') as genres, cn.added_at
                         FROM novels n
                         INNER JOIN category_novels cn ON n.id = cn.novel_id
                         INNER JOIN user_categories c ON cn.category_id = c.id
                         LEFT JOIN novel_genres ng ON n.id = ng.novel_id
                         LEFT JOIN genres g ON ng.genre_id = g.id
                         WHERE c.id = @categoryId AND c.user_id = @userId
                         GROUP BY n.id, n.title, n.author, n.cover_image, n.synopsis, 
                                  n.status, n.rating, n.chapter_count, n.created_at, n.updated_at,
                                  cn.added_at
                         ORDER BY cn.added_at DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@categoryId", categoryId);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser.Id);

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
            System.Diagnostics.Debug.WriteLine($"Error obteniendo novelas de categoría: {ex.Message}");
        }

        return novels;
    }

    /// <summary>
    /// Obtiene las categorías a las que pertenece una novela
    /// </summary>
    public async Task<List<UserCategory>> GetNovelCategoriesAsync(int novelId)
    {
        var categories = new List<UserCategory>();

        if (AuthService.CurrentUser == null) return categories;

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT c.*
                         FROM user_categories c
                         INNER JOIN category_novels cn ON c.id = cn.category_id
                         WHERE cn.novel_id = @novelId AND c.user_id = @userId
                         ORDER BY c.name";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@novelId", novelId);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser.Id);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new UserCategory
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ?
                                 "" : reader.GetString(reader.GetOrdinal("description")),
                    Color = reader.IsDBNull(reader.GetOrdinal("color")) ?
                            "#2196F3" : reader.GetString(reader.GetOrdinal("color")),
                    Icon = reader.IsDBNull(reader.GetOrdinal("icon")) ?
                           "📁" : reader.GetString(reader.GetOrdinal("icon")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo categorías de novela: {ex.Message}");
        }

        return categories;
    }
}