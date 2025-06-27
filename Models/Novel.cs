namespace NovelBook.Models;

public class Novel
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string CoverImage { get; set; }
    public string Synopsis { get; set; }
    public string Status { get; set; }
    public decimal Rating { get; set; }
    public int ChapterCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<Genre> Genres { get; set; } = new List<Genre>();
}