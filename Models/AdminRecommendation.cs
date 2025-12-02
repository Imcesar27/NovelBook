namespace NovelBook.Models;

/// <summary>
/// Modelo para representar una recomendación generada para el administrador
/// Ejemplos: "Agregar más novelas de Fantasía", "El autor X tiene alto engagement"
/// </summary>
public class AdminRecommendation
{
    /// <summary>
    /// ID único de la recomendación
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Tipo de recomendación: content, author, genre, timing, quality
    /// </summary>
    public string RecommendationType { get; set; }

    /// <summary>
    /// Título corto de la recomendación
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Descripción detallada con el análisis y justificación
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Prioridad: 1=Baja, 2=Media, 3=Alta
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Nivel de confianza del análisis (0.00 a 1.00)
    /// </summary>
    public decimal ConfidenceScore { get; set; }

    /// <summary>
    /// Si el administrador ya leyó esta recomendación
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Si la recomendación ya fue implementada
    /// </summary>
    public bool IsImplemented { get; set; }

    /// <summary>
    /// Fecha de creación de la recomendación
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Datos adicionales en formato JSON
    /// </summary>
    public string Metadata { get; set; }

    /// <summary>
    /// Constructor por defecto
    /// </summary>
    public AdminRecommendation()
    {
        Priority = 1;
        ConfidenceScore = 0.5m;
        IsRead = false;
        IsImplemented = false;
        CreatedAt = DateTime.Now;
    }

    /// <summary>
    /// Obtiene el texto de prioridad
    /// </summary>
    public string GetPriorityText()
    {
        return Priority switch
        {
            1 => "Baja",
            2 => "Media",
            3 => "Alta",
            _ => "Desconocida"
        };
    }

    /// <summary>
    /// Obtiene el color según la prioridad
    /// </summary>
    public string GetPriorityColor()
    {
        return Priority switch
        {
            1 => "#4CAF50",  // Verde - Baja
            2 => "#FF9800",  // Naranja - Media
            3 => "#F44336",  // Rojo - Alta
            _ => "#607D8B"   // Gris
        };
    }

    /// <summary>
    /// Obtiene el ícono según el tipo de recomendación
    /// </summary>
    public string GetIcon()
    {
        return RecommendationType.ToLower() switch
        {
            "content" => "📚",      // Recomendación de contenido
            "author" => "✍️",       // Recomendación de autor
            "genre" => "🏷️",        // Recomendación de género
            "timing" => "⏰",       // Recomendación de horario
            "quality" => "⭐",      // Recomendación de calidad
            _ => "💡"               // General
        };
    }

    /// <summary>
    /// Obtiene el porcentaje de confianza formateado
    /// </summary>
    public string GetConfidenceText()
    {
        return $"{ConfidenceScore * 100:F0}%";
    }

    /// <summary>
    /// Obtiene el color según el nivel de confianza
    /// </summary>
    public string GetConfidenceColor()
    {
        return ConfidenceScore switch
        {
            >= 0.8m => "#4CAF50",  // Verde - Alta confianza
            >= 0.5m => "#FF9800",  // Naranja - Media confianza
            _ => "#F44336"         // Rojo - Baja confianza
        };
    }
}

/// <summary>
/// Tipos de recomendaciones disponibles (para referencia)
/// </summary>
public static class RecommendationTypes
{
    public const string Content = "content";
    public const string Author = "author";
    public const string Genre = "genre";
    public const string Timing = "timing";
    public const string Quality = "quality";
}

/// <summary>
/// Niveles de prioridad (para referencia)
/// </summary>
public static class PriorityLevels
{
    public const int Low = 1;
    public const int Medium = 2;
    public const int High = 3;
}