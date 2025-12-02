namespace NovelBook.Models;

/// <summary>
/// Modelo para representar un patrón de lectura identificado
/// Ejemplos: "Los usuarios leen más los fines de semana", "Prefieren novelas de 100-150 capítulos"
/// </summary>
public class ReadingPattern
{
    /// <summary>
    /// ID único del patrón
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Tipo de patrón: time_preference, content_preference, engagement_pattern, 
    /// abandonment_pattern, completion_pattern
    /// </summary>
    public string PatternType { get; set; }

    /// <summary>
    /// Nombre descriptivo del patrón
    /// </summary>
    public string PatternName { get; set; }

    /// <summary>
    /// Descripción detallada del patrón encontrado
    /// </summary>
    public string PatternValue { get; set; }

    /// <summary>
    /// Frecuencia de ocurrencia del patrón (número de veces identificado)
    /// </summary>
    public int Frequency { get; set; }

    /// <summary>
    /// Nivel de confianza del patrón (0.00 a 1.00)
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// Fecha en que se identificó el patrón
    /// </summary>
    public DateTime IdentifiedAt { get; set; }

    /// <summary>
    /// Datos adicionales en formato JSON
    /// </summary>
    public string Metadata { get; set; }

    /// <summary>
    /// Constructor por defecto
    /// </summary>
    public ReadingPattern()
    {
        Frequency = 0;
        Confidence = 0.5m;
        IdentifiedAt = DateTime.Now;
    }

    /// <summary>
    /// Obtiene el ícono según el tipo de patrón
    /// </summary>
    public string GetIcon()
    {
        return PatternType.ToLower() switch
        {
            "time_preference" => "🕐",        // Preferencia de horario
            "content_preference" => "📖",     // Preferencia de contenido
            "engagement_pattern" => "💪",     // Patrón de engagement
            "abandonment_pattern" => "🚪",    // Patrón de abandono
            "completion_pattern" => "🏆",     // Patrón de finalización
            _ => "🔍"                         // General
        };
    }

    /// <summary>
    /// Obtiene el color temático según el tipo de patrón
    /// </summary>
    public string GetThemeColor()
    {
        return PatternType.ToLower() switch
        {
            "time_preference" => "#2196F3",      // Azul
            "content_preference" => "#9C27B0",   // Púrpura
            "engagement_pattern" => "#4CAF50",   // Verde
            "abandonment_pattern" => "#F44336",  // Rojo
            "completion_pattern" => "#FF9800",   // Naranja
            _ => "#607D8B"                       // Gris
        };
    }

    /// <summary>
    /// Obtiene el texto descriptivo del tipo de patrón
    /// </summary>
    public string GetPatternTypeText()
    {
        return PatternType.ToLower() switch
        {
            "time_preference" => "Preferencia de Horario",
            "content_preference" => "Preferencia de Contenido",
            "engagement_pattern" => "Patrón de Engagement",
            "abandonment_pattern" => "Patrón de Abandono",
            "completion_pattern" => "Patrón de Finalización",
            _ => "Patrón General"
        };
    }

    /// <summary>
    /// Obtiene el porcentaje de confianza formateado
    /// </summary>
    public string GetConfidenceText()
    {
        return $"{Confidence * 100:F0}%";
    }

    /// <summary>
    /// Obtiene el color según el nivel de confianza
    /// </summary>
    public string GetConfidenceColor()
    {
        return Confidence switch
        {
            >= 0.8m => "#4CAF50",  // Verde - Alta confianza
            >= 0.5m => "#FF9800",  // Naranja - Media confianza
            _ => "#F44336"         // Rojo - Baja confianza
        };
    }

    /// <summary>
    /// Indica si el patrón es confiable (confianza >= 70%)
    /// </summary>
    public bool IsReliable => Confidence >= 0.7m;
}

/// <summary>
/// Tipos de patrones disponibles (para referencia)
/// </summary>
public static class PatternTypes
{
    public const string TimePreference = "time_preference";
    public const string ContentPreference = "content_preference";
    public const string EngagementPattern = "engagement_pattern";
    public const string AbandonmentPattern = "abandonment_pattern";
    public const string CompletionPattern = "completion_pattern";
}