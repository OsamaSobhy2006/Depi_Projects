namespace LibraryManagementSystem.Models;

public class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public bool IsBorrowed { get; set; }

    public DateTime? DueDate { get; set; }
}
