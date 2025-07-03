using MauiColor = Microsoft.Maui.Graphics.Color;

namespace NovelBook.Models;

/// <summary>
/// Modelo para representar una categoría personalizada del usuario (carpeta)
/// </summary>
public class UserCategory
{
    /// <summary>
    /// ID único de la categoría
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID del usuario que creó la categoría
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Nombre de la categoría (ej: "Para leer en vacaciones")
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Descripción opcional de la categoría
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Color en formato hexadecimal (ej: #FF5733)
    /// </summary>
    public string Color { get; set; }

    /// <summary>
    /// Emoji o nombre del ícono para mostrar
    /// </summary>
    public string Icon { get; set; }

    /// <summary>
    /// Fecha de creación de la categoría
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Fecha de última actualización
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Número de novelas en esta categoría
    /// </summary>
    public int NovelCount { get; set; }

    /// <summary>
    /// Lista de novelas en esta categoría (se carga cuando es necesario)
    /// </summary>
    public List<Novel> Novels { get; set; }

    /// <summary>
    /// Constructor por defecto
    /// </summary>
    public UserCategory()
    {
        Novels = new List<Novel>();
        Icon = "📁"; // Ícono por defecto
        Color = "#2196F3"; // Color azul por defecto
    }

    /// <summary>
    /// Obtiene el color como objeto Color de MAUI
    /// </summary>
    public MauiColor GetMauiColor()
    {
        try
        {
            
            return MauiColor.FromArgb(this.Color);
        }
        catch
        {
            // Si el color no es válido, devolver azul por defecto
            return MauiColor.FromArgb("#2196F3");
        }
    }

    /// <summary>
    /// Valida si el nombre de la categoría es válido
    /// </summary>
    public bool IsValidName()
    {
        return !string.IsNullOrWhiteSpace(Name) &&
               Name.Length >= 3 &&
               Name.Length <= 100;
    }
}

/// <summary>
/// Modelo para representar la relación entre categoría y novela
/// </summary>
public class CategoryNovel
{
    /// <summary>
    /// ID de la categoría
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// ID de la novela
    /// </summary>
    public int NovelId { get; set; }

    /// <summary>
    /// Fecha cuando se agregó la novela a la categoría
    /// </summary>
    public DateTime AddedAt { get; set; }
}