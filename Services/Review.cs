namespace NovelBook.Models;

/// <summary>
/// Modelo que representa una reseña de novela
/// Corresponde a la tabla 'reviews' en la base de datos
/// </summary>
public class Review
{
    /// <summary>
    /// ID único de la reseña
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID del usuario que escribió la reseña
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// ID de la novela que se está reseñando
    /// </summary>
    public int NovelId { get; set; }

    /// <summary>
    /// Calificación del 1 al 5
    /// 1 = Muy mala, 5 = Excelente
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Comentario opcional del usuario sobre la novela
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Fecha y hora en que se creó la reseña
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Fecha y hora de la última actualización
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Nombre del usuario (no está en la BD, se llena al hacer JOIN)
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// Obtiene el texto descriptivo para la calificación
    /// </summary>
    public string RatingText => Rating switch
    {
        1 => "Muy mala",
        2 => "Mala",
        3 => "Regular",
        4 => "Buena",
        5 => "Excelente",
        _ => "Sin calificar"
    };

    /// <summary>
    /// Obtiene las estrellas en formato visual
    /// </summary>
    public string GetStars()
    {
        return new string('★', Rating) + new string('☆', 5 - Rating);
    }
}

/// <summary>
/// Clase para las estadísticas de reseñas de una novela
/// </summary>
public class ReviewStats
{
    /// <summary>
    /// Total de reseñas
    /// </summary>
    public int TotalReviews { get; set; }

    /// <summary>
    /// Calificación promedio
    /// </summary>
    public decimal AverageRating { get; set; }

    /// <summary>
    /// Distribución de calificaciones (cuántas de cada estrella)
    /// Key = rating (1-5), Value = cantidad
    /// </summary>
    public Dictionary<int, int> RatingDistribution { get; set; }

    /// <summary>
    /// Constructor que inicializa el diccionario
    /// </summary>
    public ReviewStats()
    {
        RatingDistribution = new Dictionary<int, int>
        {
            { 1, 0 },
            { 2, 0 },
            { 3, 0 },
            { 4, 0 },
            { 5, 0 }
        };
    }

    /// <summary>
    /// Calcula el porcentaje de reseñas para una calificación específica
    /// </summary>
    /// <param name="rating">Calificación (1-5)</param>
    /// <returns>Porcentaje de reseñas con esa calificación</returns>
    public double GetPercentage(int rating)
    {
        if (TotalReviews == 0) return 0;
        return (double)RatingDistribution[rating] / TotalReviews * 100;
    }
}