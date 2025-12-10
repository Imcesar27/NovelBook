namespace NovelBook.Models;

/// <summary>
/// Modelo para etiquetas de novelas creadas por usuarios
/// </summary>
public class NovelTag
{
    public int Id { get; set; }
    public int NovelId { get; set; }
    public int UserId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Propiedades calculadas/adicionales
    public int VoteCount { get; set; }
    public bool UserHasVoted { get; set; }
    public string CreatorName { get; set; } = string.Empty;

    /// <summary>
    /// Verifica si el creador puede eliminar la etiqueta (dentro de 24 horas)
    /// </summary>
    public bool CanBeDeletedByCreator(int currentUserId)
    {
        if (UserId != currentUserId) return false;
        var hoursSinceCreation = (DateTime.Now - CreatedAt).TotalHours;
        return hoursSinceCreation <= 24;
    }

    /// <summary>
    /// Obtiene el tiempo restante para eliminar
    /// </summary>
    public string GetTimeRemainingToDelete()
    {
        var hoursRemaining = 24 - (DateTime.Now - CreatedAt).TotalHours;
        if (hoursRemaining <= 0) return "Expirado";
        if (hoursRemaining < 1) return $"{(int)(hoursRemaining * 60)} min";
        return $"{(int)hoursRemaining}h";
    }
}