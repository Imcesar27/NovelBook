namespace NovelBook.Models;

/// <summary>
/// Modelo para representar una métrica de analytics calculada
/// Ejemplos: tiempo promedio de lectura, tasa de abandono, usuarios activos
/// </summary>
public class AnalyticsMetric
{
    /// <summary>
    /// ID único de la métrica
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Tipo de métrica: engagement, popularity, retention, abandonment, reading_speed
    /// </summary>
    public string MetricType { get; set; }

    /// <summary>
    /// Nombre descriptivo de la métrica (ej: "Tiempo Promedio de Lectura")
    /// </summary>
    public string MetricName { get; set; }

    /// <summary>
    /// Valor numérico calculado de la métrica
    /// </summary>
    public decimal MetricValue { get; set; }

    /// <summary>
    /// Fecha y hora en que se calculó la métrica
    /// </summary>
    public DateTime CalculatedAt { get; set; }

    /// <summary>
    /// Datos adicionales en formato JSON (unidad, período, filtros aplicados, etc.)
    /// </summary>
    public string Metadata { get; set; }

    /// <summary>
    /// Constructor por defecto
    /// </summary>
    public AnalyticsMetric()
    {
        CalculatedAt = DateTime.Now;
    }

    /// <summary>
    /// Obtiene el valor formateado según el tipo de métrica
    /// </summary>
    public string GetFormattedValue()
    {
        return MetricType.ToLower() switch
        {
            "engagement" => $"{MetricValue:F1} min",           // Minutos
            "popularity" => $"{MetricValue:F0}",               // Número entero
            "retention" => $"{MetricValue:F1}%",               // Porcentaje
            "abandonment" => $"{MetricValue:F1}%",             // Porcentaje
            "reading_speed" => $"{MetricValue:F0} palabras/min", // Palabras por minuto
            _ => $"{MetricValue:F2}"                           // Valor por defecto
        };
    }

    /// <summary>
    /// Obtiene el ícono representativo según el tipo de métrica
    /// </summary>
    public string GetIcon()
    {
        return MetricType.ToLower() switch
        {
            "engagement" => "⏱️",
            "popularity" => "🔥",
            "retention" => "📈",
            "abandonment" => "📉",
            "reading_speed" => "⚡",
            _ => "📊"
        };
    }

    /// <summary>
    /// Obtiene el color temático según el tipo de métrica
    /// </summary>
    public string GetThemeColor()
    {
        return MetricType.ToLower() switch
        {
            "engagement" => "#2196F3",    // Azul
            "popularity" => "#FF9800",    // Naranja
            "retention" => "#4CAF50",     // Verde
            "abandonment" => "#F44336",   // Rojo
            "reading_speed" => "#9C27B0", // Púrpura
            _ => "#607D8B"                // Gris
        };
    }
}

/// <summary>
/// Tipos de métricas disponibles (para referencia)
/// </summary>
public static class MetricTypes
{
    public const string Engagement = "engagement";
    public const string Popularity = "popularity";
    public const string Retention = "retention";
    public const string Abandonment = "abandonment";
    public const string ReadingSpeed = "reading_speed";
}