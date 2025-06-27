namespace NovelBook.Models;

public class UserLibraryItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int NovelId { get; set; }
    public Novel Novel { get; set; }
    public DateTime AddedAt { get; set; }
    public int LastReadChapter { get; set; }
    public bool IsFavorite { get; set; }
    public string ReadingStatus { get; set; }
}