
namespace CampusOne.Domain.Entities
{
    public class Book
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Author { get; set; } = string.Empty;

        public string Cover { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Uploaded PDF path or external reading URL
        public string? FileUrl { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}
