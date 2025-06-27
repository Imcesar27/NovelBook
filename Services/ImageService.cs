using Microsoft.Maui.Graphics;

namespace NovelBook.Services;

public class ImageService
{
    private readonly DatabaseService _database;

    public ImageService(DatabaseService database)
    {
        _database = database;
    }

    /// <summary>
    /// Obtiene la imagen de portada como ImageSource
    /// </summary>
    public async Task<ImageSource> GetCoverImageAsync(string coverImageUrl)
    {
        try
        {
            // Si es una URL de base de datos
            if (!string.IsNullOrEmpty(coverImageUrl) && coverImageUrl.StartsWith("db://covers/"))
            {
                var novelIdStr = coverImageUrl.Replace("db://covers/", "");
                if (int.TryParse(novelIdStr, out int novelId))
                {
                    using var connection = _database.GetConnection();
                    await connection.OpenAsync();

                    var query = "SELECT image_data, image_type FROM novel_covers WHERE novel_id = @novelId";
                    using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@novelId", novelId);

                    using var reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        var imageData = (byte[])reader["image_data"];
                        return ImageSource.FromStream(() => new MemoryStream(imageData));
                    }
                }
            }
            // Si es una URL normal
            else if (!string.IsNullOrEmpty(coverImageUrl))
            {
                return ImageSource.FromUri(new Uri(coverImageUrl));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando imagen: {ex.Message}");
        }

        // Imagen por defecto
        return "novel_placeholder.jpg";
    }
}