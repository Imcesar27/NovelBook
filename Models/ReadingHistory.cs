namespace NovelBook.Models;

/// <summary>
/// Representa un elemento del historial de lectura
/// Corresponde a la tabla 'reading_history' en la base de datos
/// </summary>
public class ReadingHistoryItem
{
    /// <summary>
    /// ID único del registro en el historial
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID del usuario
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// ID de la novela
    /// </summary>
    public int NovelId { get; set; }

    /// <summary>
    /// ID del capítulo leído
    /// </summary>
    public int ChapterId { get; set; }

    /// <summary>
    /// Progreso de lectura en el capítulo (0-100)
    /// </summary>
    public decimal ReadingProgress { get; set; }

    /// <summary>
    /// Tiempo de lectura en segundos
    /// </summary>
    public int ReadingTime { get; set; }

    /// <summary>
    /// Fecha y hora de lectura
    /// </summary>
    public DateTime ReadAt { get; set; }

    /// <summary>
    /// Indica si el capítulo fue completado
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Última posición de lectura (para retomar)
    /// </summary>
    public int LastPosition { get; set; }

    // Propiedades adicionales para mostrar en UI (no están en BD)
    public string NovelTitle { get; set; }
    public string NovelAuthor { get; set; }
    public string NovelCover { get; set; }
    public int ChapterNumber { get; set; }
    public string ChapterTitle { get; set; }

    /// <summary>
    /// Obtiene el tiempo de lectura formateado
    /// </summary>
    public string FormattedReadingTime
    {
        get
        {
            if (ReadingTime < 60)
                return $"{ReadingTime} seg";
            else if (ReadingTime < 3600)
                return $"{ReadingTime / 60} min";
            else
                return $"{ReadingTime / 3600}h {(ReadingTime % 3600) / 60}m";
        }
    }

    /// <summary>
    /// Obtiene la fecha formateada según qué tan reciente es
    /// </summary>
    public string FormattedDate
    {
        get
        {
            var now = DateTime.Now;
            var diff = now - ReadAt;

            if (diff.TotalMinutes < 1)
                return "Hace un momento";
            else if (diff.TotalMinutes < 60)
                return $"Hace {(int)diff.TotalMinutes} minutos";
            else if (diff.TotalHours < 24)
                return $"Hace {(int)diff.TotalHours} horas";
            else if (diff.TotalDays < 7)
                return $"Hace {(int)diff.TotalDays} días";
            else
                return ReadAt.ToString("dd/MM/yyyy");
        }
    }

    /// <summary>
    /// Texto descriptivo del capítulo
    /// </summary>
    public string ChapterDescription => $"Capítulo {ChapterNumber}: {ChapterTitle}";
}

/// <summary>
/// Representa el historial agrupado por novela
/// </summary>
public class NovelHistoryGroup
{
    public int NovelId { get; set; }
    public string NovelTitle { get; set; }
    public string NovelAuthor { get; set; }
    public string NovelCover { get; set; }
    public DateTime LastRead { get; set; }
    public int ChaptersRead { get; set; }
    public int TotalReadingTime { get; set; }

    /// <summary>
    /// Obtiene el tiempo total formateado
    /// </summary>
    public string FormattedTotalTime
    {
        get
        {
            if (TotalReadingTime < 3600)
                return $"{TotalReadingTime / 60} min";
            else
                return $"{TotalReadingTime / 3600}h {(TotalReadingTime % 3600) / 60}m";
        }
    }

    /// <summary>
    /// Obtiene cuándo fue la última lectura
    /// </summary>
    public string LastReadFormatted
    {
        get
        {
            var diff = DateTime.Now - LastRead;
            if (diff.TotalDays < 1)
                return "Hoy";
            else if (diff.TotalDays < 2)
                return "Ayer";
            else if (diff.TotalDays < 7)
                return $"Hace {(int)diff.TotalDays} días";
            else
                return LastRead.ToString("dd/MM/yyyy");
        }
    }
}

/// <summary>
/// Estadísticas de lectura del usuario
/// </summary>
public class ReadingStats
{
    /// <summary>
    /// Total de capítulos leídos
    /// </summary>
    public int TotalChaptersRead { get; set; }

    /// <summary>
    /// Total de novelas diferentes leídas
    /// </summary>
    public int TotalNovelsRead { get; set; }

    /// <summary>
    /// Tiempo total de lectura en segundos
    /// </summary>
    public int TotalReadingTime { get; set; }

    /// <summary>
    /// Días totales con actividad de lectura
    /// </summary>
    public int ReadingDays { get; set; }

    /// <summary>
    /// Racha actual de días consecutivos leyendo
    /// </summary>
    public int CurrentStreak { get; set; }

    /// <summary>
    /// Tiempo de lectura formateado
    /// </summary>
    public string FormattedTotalTime
    {
        get
        {
            if (TotalReadingTime < 3600)
                return $"{TotalReadingTime / 60} min";
            else if (TotalReadingTime < 86400)
                return $"{TotalReadingTime / 3600}h";
            else
            {
                var days = TotalReadingTime / 86400;
                var hours = (TotalReadingTime % 86400) / 3600;
                return $"{days}d {hours}h";
            }
        }
    }

    /// <summary>
    /// Promedio de capítulos por día
    /// </summary>
    public double AverageChaptersPerDay =>
        ReadingDays > 0 ? (double)TotalChaptersRead / ReadingDays : 0;

    /// <summary>
    /// Promedio de tiempo de lectura por día
    /// </summary>
    public string AverageTimePerDay
    {
        get
        {
            if (ReadingDays == 0) return "0 min";

            var avgSeconds = TotalReadingTime / ReadingDays;
            if (avgSeconds < 3600)
                return $"{avgSeconds / 60} min/día";
            else
                return $"{avgSeconds / 3600}h/día";
        }
    }
}

/// <summary>
/// Representa el progreso de lectura de un capítulo
/// Corresponde a la tabla 'reading_progress'
/// </summary>
public class ReadingProgress
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ChapterId { get; set; }
    public decimal Progress { get; set; }
    public int LastPosition { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime LastReadAt { get; set; }
}