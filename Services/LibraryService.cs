using NovelBook.Models;
using Microsoft.Data.SqlClient;

namespace NovelBook.Services;

public class LibraryService
{
    private readonly DatabaseService _database;
    private readonly AuthService _authService;

    public LibraryService(DatabaseService database, AuthService authService)
    {
        _database = database;
        _authService = authService;
    }

    public async Task<List<UserLibraryItem>> GetUserLibraryAsync()
    {
        var library = new List<UserLibraryItem>();

        if (AuthService.CurrentUser == null) return library;

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"SELECT ul.*, n.* 
                         FROM user_library ul
                         INNER JOIN novels n ON ul.novel_id = n.id
                         WHERE ul.user_id = @userId
                         ORDER BY ul.added_at DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser.Id);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var item = new UserLibraryItem
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                    NovelId = reader.GetInt32(reader.GetOrdinal("novel_id")),
                    AddedAt = reader.GetDateTime(reader.GetOrdinal("added_at")),
                    LastReadChapter = reader.GetInt32(reader.GetOrdinal("last_read_chapter")),
                    IsFavorite = reader.GetBoolean(reader.GetOrdinal("is_favorite")),
                    ReadingStatus = reader.GetString(reader.GetOrdinal("reading_status")),
                    Novel = new Novel
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("novel_id")),
                        Title = reader.GetString(reader.GetOrdinal("title")),
                        Author = reader.IsDBNull(reader.GetOrdinal("author")) ? "" : reader.GetString(reader.GetOrdinal("author")),
                        CoverImage = reader.IsDBNull(reader.GetOrdinal("cover_image")) ? "" : reader.GetString(reader.GetOrdinal("cover_image")),
                        Status = reader.GetString(reader.GetOrdinal("status")),
                        Rating = reader.GetDecimal(reader.GetOrdinal("rating")),
                        ChapterCount = reader.GetInt32(reader.GetOrdinal("chapter_count"))
                    }
                };

                library.Add(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al obtener biblioteca: {ex.Message}");
        }

        return library;
    }

    public async Task<bool> AddToLibraryAsync(int novelId)
    {
        if (AuthService.CurrentUser == null) return false;

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"IF NOT EXISTS (SELECT 1 FROM user_library WHERE user_id = @userId AND novel_id = @novelId)
                         INSERT INTO user_library (user_id, novel_id) VALUES (@userId, @novelId)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser.Id);
            command.Parameters.AddWithValue("@novelId", novelId);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al agregar a biblioteca: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RemoveFromLibraryAsync(int novelId)
    {
        if (AuthService.CurrentUser == null) return false;

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = "DELETE FROM user_library WHERE user_id = @userId AND novel_id = @novelId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser.Id);
            command.Parameters.AddWithValue("@novelId", novelId);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al eliminar de biblioteca: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> IsInLibraryAsync(int novelId)
    {
        if (AuthService.CurrentUser == null) return false;

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM user_library WHERE user_id = @userId AND novel_id = @novelId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser.Id);
            command.Parameters.AddWithValue("@novelId", novelId);

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ToggleFavoriteAsync(int novelId)
    {
        if (AuthService.CurrentUser == null) return false;

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"UPDATE user_library 
                     SET is_favorite = ~is_favorite 
                     WHERE user_id = @userId AND novel_id = @novelId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser.Id);
            command.Parameters.AddWithValue("@novelId", novelId);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error toggling favorite: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateReadingStatusAsync(int novelId, string newStatus)
    {
        if (AuthService.CurrentUser == null) return false;

        try
        {
            using var connection = _database.GetConnection();
            await connection.OpenAsync();

            var query = @"UPDATE user_library 
                     SET reading_status = @status 
                     WHERE user_id = @userId AND novel_id = @novelId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", AuthService.CurrentUser.Id);
            command.Parameters.AddWithValue("@novelId", novelId);
            command.Parameters.AddWithValue("@status", newStatus);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating status: {ex.Message}");
            return false;
        }
    }
}