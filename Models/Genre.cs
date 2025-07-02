namespace NovelBook.Models;

public class Genre
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}

/// <summary>
/// Modelo extendido para géneros populares con estadísticas
/// </summary>
public class PopularGenre : Genre
{
    /// <summary>
    /// Posición en el ranking de popularidad
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Número de novelas en este género
    /// </summary>
    public int NovelCount { get; set; }

    /// <summary>
    /// Calificación promedio de las novelas del género
    /// </summary>
    public decimal AverageRating { get; set; }

    /// <summary>
    /// Total de capítulos leídos de todas las novelas del género
    /// </summary>
    public int TotalChaptersRead { get; set; }

    /// <summary>
    /// Número de usuarios únicos que tienen novelas de este género
    /// </summary>
    public int ActiveUsers { get; set; }

    /// <summary>
    /// Total de reseñas en novelas del género
    /// </summary>
    public int TotalReviews { get; set; }

    /// <summary>
    /// Puntaje de popularidad calculado
    /// </summary>
    public double PopularityScore { get; set; }

    /// <summary>
    /// Obtiene un icono representativo basado en el nombre del género
    /// </summary>
    public string Icon => Name.ToLower() switch
    {
        var n when n.Contains("romance") => "💕",
        var n when n.Contains("fantasia") || n.Contains("fantasy") => "🔮",
        var n when n.Contains("accion") || n.Contains("action") => "⚔️",
        var n when n.Contains("terror") || n.Contains("horror") => "👻",
        var n when n.Contains("misterio") || n.Contains("mystery") => "🔍",
        var n when n.Contains("comedia") || n.Contains("comedy") => "😄",
        var n when n.Contains("drama") => "🎭",
        var n when n.Contains("aventura") || n.Contains("adventure") => "🗺️",
        var n when n.Contains("ciencia") || n.Contains("sci-fi") => "🚀",
        var n when n.Contains("historia") || n.Contains("historical") => "📜",
        var n when n.Contains("deporte") || n.Contains("sports") => "⚽",
        var n when n.Contains("escolar") || n.Contains("school") => "🎓",
        var n when n.Contains("magia") || n.Contains("magic") => "✨",
        _ => "📚"
    };

    /// <summary>
    /// Obtiene el color temático del género
    /// </summary>
    public string ThemeColor => Name.ToLower() switch
    {
        var n when n.Contains("romance") => "#E91E63",
        var n when n.Contains("fantasia") || n.Contains("fantasy") => "#9C27B0",
        var n when n.Contains("accion") || n.Contains("action") => "#F44336",
        var n when n.Contains("terror") || n.Contains("horror") => "#424242",
        var n when n.Contains("misterio") || n.Contains("mystery") => "#3F51B5",
        var n when n.Contains("comedia") || n.Contains("comedy") => "#FFC107",
        var n when n.Contains("drama") => "#795548",
        var n when n.Contains("aventura") || n.Contains("adventure") => "#4CAF50",
        var n when n.Contains("ciencia") || n.Contains("sci-fi") => "#00BCD4",
        _ => "#607D8B"
    };

    /// <summary>
    /// Obtiene una descripción del nivel de popularidad
    /// </summary>
    public string PopularityLevel
    {
        get
        {
            if (Rank == 1) return "🔥 Más Popular";
            if (Rank <= 3) return "⭐ Muy Popular";
            if (Rank <= 5) return "✨ Popular";
            if (Rank <= 10) return "📈 En Tendencia";
            return "📚 Género";
        }
    }
}

/// <summary>
/// Estadísticas detalladas de un género
/// </summary>
public class GenreStats
{
    public int GenreId { get; set; }
    public string GenreName { get; set; }
    public string Description { get; set; }

    // Estadísticas de novelas
    public int TotalNovels { get; set; }
    public int CompletedNovels { get; set; }
    public int OngoingNovels { get; set; }
    public int TotalChapters { get; set; }

    // Estadísticas de ratings
    public decimal AverageRating { get; set; }
    public decimal MinRating { get; set; }
    public decimal MaxRating { get; set; }

    // Estadísticas de usuarios
    public int UniqueReaders { get; set; }
    public int TotalReviews { get; set; }

    /// <summary>
    /// Porcentaje de novelas completadas
    /// </summary>
    public double CompletionRate =>
        TotalNovels > 0 ? (double)CompletedNovels / TotalNovels * 100 : 0;

    /// <summary>
    /// Promedio de capítulos por novela
    /// </summary>
    public double AverageChaptersPerNovel =>
        TotalNovels > 0 ? (double)TotalChapters / TotalNovels : 0;

    /// <summary>
    /// Promedio de lectores por novela
    /// </summary>
    public double AverageReadersPerNovel =>
        TotalNovels > 0 ? (double)UniqueReaders / TotalNovels : 0;
}