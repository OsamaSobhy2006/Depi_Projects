
namespace CampusOne.Domain.Entities
{
    public class BorrowRequest
    {
        public int Id { get; set; }

        public int BookId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public DateTime BorrowDate { get; set; } = DateTime.UtcNow;

        public DateTime DueDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        public string Status { get; set; } = "Borrowed";

        public decimal FineAmount { get; set; } = 0.00m;

        public bool IsFinePaid { get; set; } = false;

        public DateTime? PaidAt { get; set; }

        public string? StripeCheckoutSessionId { get; set; }

        public Book? Book { get; set; }
    }
}
