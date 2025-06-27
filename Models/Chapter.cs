namespace NovelBook.Models;

public class Chapter
{
    public int Id { get; set; }
    public int NovelId { get; set; }
    public int ChapterNumber { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
}